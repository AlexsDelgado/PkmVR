using System;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

//Connects: Quest microphone  →  Whisper streaming  →  VoiceCommandRouter.
public class WhisperStreamBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WhisperManager whisper;
    [SerializeField] private MicrophoneRecord mic;
    [SerializeField] private VoiceCommandRouter router;

    [Header("Microphone")]
    [Tooltip("Substring to search in Microphone.devices. Leave empty to use OS default.")]
    [SerializeField] private string preferredDeviceSubstring;

    [Header("Debug")]
    [SerializeField] private bool logDevices = true;
    [SerializeField] private bool logPartial = true;
    [SerializeField] private bool logFinal = true;

    private WhisperStream _stream;
    private bool _started;

    // ---------- LIFECYCLE ----------

    private async void OnEnable()
    {
        if (!ValidateRefs())
            return;

        // Ensure model is ready
        await EnsureModelLoaded();

        if (!whisper.IsLoaded)
        {
            Debug.LogError("[WhisperBridge] Model not loaded, aborting.");
            return;
        }

        // Select Quest microphone and tell MicrophoneRecord to use it
        string device = PickMicDevice();
        if (string.IsNullOrEmpty(device))
        {
            Debug.LogError("[WhisperBridge] No microphone device found.");
            return;
        }

        mic.SelectedMicDevice = device;

        if (logDevices)
        {
            int min, max;
            Microphone.GetDeviceCaps(device, out min, out max);
            Debug.Log($"[WhisperBridge] Using mic: '{device}' (min={min}, max={max})");
        }

        // Create streaming session bound to this mic
        _stream = await whisper.CreateStream(mic);
        if (_stream == null)
        {
            Debug.LogError("[WhisperBridge] Failed to create WhisperStream.");
            return;
        }

        // Hook events
        HookEvents(true);

        // Start audio + stream
        _stream.StartStream();
        mic.StartRecord();

        _started = true;
        Debug.Log("[WhisperBridge] Streaming started.");
    }

    private void OnDisable()
    {
        if (!_started)
            return;

        // Stop mic first so we stop producing data
        if (mic != null)
            mic.StopRecord();

        // Stop / dispose stream
        if (_stream != null)
        {
            HookEvents(false);
            _stream.StopStream();
            _stream = null;
        }

        _started = false;
        Debug.Log("[WhisperBridge] Streaming stopped.");
    }

    // ---------- EVENT WIRING ----------

    private void HookEvents(bool subscribe)
    {
        if (whisper == null)
            return;

        if (subscribe)
        {
            // FINAL segments
            whisper.OnNewSegment += HandleNewSegment;

            // Optional: partial text for live feedback
            whisper.OnPartialTranscription += HandlePartial;
        }
        else
        {
            whisper.OnNewSegment -= HandleNewSegment;
            whisper.OnPartialTranscription -= HandlePartial;
        }
    }

    private bool IsOnlySpecialTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (!t.StartsWith("[") || !t.EndsWith("]"))
                return false; // we found a real word
        }
        return true; // all tokens are [LIKE_THIS]
    }

    private void HandlePartial(string text)
    {
        if (IsOnlySpecialTokens(text))
            return;

        if (logPartial)
            Debug.Log("[WhisperBridge] Partial: " + text);
    }

    private void HandleNewSegment(WhisperSegment seg)
    {
        if (seg == null) return;

        var text = seg.Text;
        if (IsOnlySpecialTokens(text))
            return;

        text = text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (logFinal)
            Debug.Log("[WhisperBridge] FINAL: " + text);

        router?.OnWhisperResult(text);
    }

    // ---------- UTILITIES ----------

    private bool ValidateRefs()
    {
        bool ok = true;

        if (whisper == null)
        {
            Debug.LogError("[WhisperBridge] Missing WhisperManager reference.");
            ok = false;
        }
        if (mic == null)
        {
            Debug.LogError("[WhisperBridge] Missing MicrophoneRecord reference.");
            ok = false;
        }
        if (!ok)
            enabled = false;

        return ok;
    }

    private async Task EnsureModelLoaded()
    {
        if (whisper.IsLoaded)
            return;

        Debug.Log("[WhisperBridge] Waiting for Whisper model to load...");
        await whisper.InitModel();

        if (whisper.IsLoaded)
            Debug.Log("[WhisperBridge] Model loaded.");
    }

    // Try to pick Quest mic by substring; fall back to first device.
    private string PickMicDevice()
    {
        var devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
            return null;

        if (logDevices)
        {
            Debug.Log("[WhisperBridge] Available mics:");
            foreach (var d in devices)
                Debug.Log("  - " + d);
        }

        if (!string.IsNullOrEmpty(preferredDeviceSubstring))
        {
            foreach (var d in devices)
            {
                if (d.IndexOf(preferredDeviceSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                    return d;
            }
        }

        // Fallback: first device
        return devices[0];
    }
}