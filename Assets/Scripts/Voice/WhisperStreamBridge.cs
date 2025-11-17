using UnityEngine;
using Whisper;
using Whisper.Utils;

public class WhisperStreamBridge : MonoBehaviour
{
    [Header("References")]
    public WhisperManager whisper;
    public MicrophoneRecord mic;
    public VoiceCommandRouter router;

    private WhisperStream _stream;
    private bool _started;

    private async void OnEnable()
    {
        if (!whisper) { Debug.LogError("[Whisper] Missing WhisperManager reference"); return; }
        if (!mic) { Debug.LogError("[Whisper] Missing MicrophoneRecord reference"); return; }

        // Wait for model to load
        if (!whisper.IsLoaded)
        {
            Debug.Log("[Whisper] Waiting for model to load...");
            await whisper.InitModel();
        }

        if (!whisper.IsLoaded)
        {
            Debug.LogError("[Whisper] Model failed to load!");
            return;
        }

        // Start mic
        var dev = PickMic("Oculus Virtual Audio");         // or "Steam Streaming" if using Steam Link
        Debug.Log("[Mic] Using device: " + dev);

        mic.StartRecord();
        mic.OnChunkReady += OnChunk;

        int min, max;
        Microphone.GetDeviceCaps(dev, out min, out max);
        Debug.Log($"[Mic] Caps for '{dev}': min={min}, max={max} (0 means 'unspecified')");

        // Create the whisper stream based on mic
        _stream = await whisper.CreateStream(mic.frequency, 1);
        if (_stream == null)
        {
            Debug.LogError("[Whisper] Could not create Whisper stream");
            return;
        }

        // Hook events
        _stream.OnSegmentFinished += OnSegment;
        _stream.OnResultUpdated += OnPartial;
        _stream.OnStreamFinished += OnFinal;

        _started = true;
        Debug.Log("[Whisper] Streaming started!");
    }

    private void OnDisable()
    {
        if (!_started) return;

        mic.OnChunkReady -= OnChunk;
        mic.StopRecord();

        if (_stream != null)
        {
            _stream.OnSegmentFinished -= OnSegment;
            _stream.OnResultUpdated -= OnPartial;
            _stream.OnStreamFinished -= OnFinal;
            _stream.StopStream();
            _stream = null;
        }

        _started = false;
        Debug.Log("[Whisper] Streaming stopped!");
    }

    private void OnChunk(AudioChunk chunk)
    {
        if (_stream == null) return;
        _stream.AddToStream(chunk);
    }

    private void OnPartial(string partial)
    {
        // live update for debugging
        Debug.Log($"Partial: {partial}");
    }

    private void OnSegment(WhisperResult segment)
    {
        if (segment == null || string.IsNullOrEmpty(segment.Result)) return;
        var text = segment.Result.Trim().ToLowerInvariant();
        Debug.Log($"[Whisper] Segment: {text}");
        router?.OnWhisperResult(text);
    }

    private void OnFinal(string finalText)
    {
        if (string.IsNullOrEmpty(finalText)) return;
        Debug.Log($"[Whisper] Final: {finalText}");
        router?.OnWhisperResult(finalText.ToLowerInvariant());
    }

    string PickMic(string preferredContains)
    {
        foreach (var d in Microphone.devices)
            if (d.IndexOf(preferredContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return d;
        return Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
    }

}