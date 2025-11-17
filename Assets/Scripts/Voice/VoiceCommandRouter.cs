using UnityEngine;

public class VoiceCommandRouter : MonoBehaviour
{
    [TextArea] public string lastHeard;

    public void OnWhisperResult(string text)
    {
        lastHeard = text;
        Debug.Log($"[Voice] {text}");

        // Minimal Spanish keywords – expand as you like
        if (text.Contains("atacar") || text.Contains("ataque"))
        {
            // AbilityResolver.Instance.Queue("Attack");
        }
        else if (text.Contains("curar") || text.Contains("cura") || text.Contains("sanar"))
        {
            // AbilityResolver.Instance.Queue("Heal");
        }
        else if (text.Contains("defender") || text.Contains("defensa"))
        {
            // AbilityResolver.Instance.Queue("Defend");
        }
        else if (text.Contains("rayo") || text.Contains("trueno"))
        {
            // AbilityResolver.Instance.Queue("Thunderbolt");
        }
        // add more…
    }
}