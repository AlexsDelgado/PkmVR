using System;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class VoiceCommandRouter : MonoBehaviour
{
    [TextArea] public string lastHeard;
    bool in_combat = false;
    CombatManager combatManager;
    string[] movesName = new string[4];
    private void Start()
    {
        combatManager = CombatManager.instance;
        combatManager.combatStart += SwapCombatState;
        combatManager.changePKM += SetMoveNames;
    }
    void SwapCombatState()
    {
        in_combat = !in_combat;
    }
    void SetMoveNames()
    {
        for (int i = 0; i < movesName.Length; i++)
        {
            if (combatManager.my_avtive_pokemon.moves[i] == null)
            {
                movesName[i] = "Pegelagarto";
            }
            movesName[i] = combatManager.my_avtive_pokemon.moves[i].name;
        }
    }
    string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        s = s.ToLowerInvariant();

        // Remove accents
        var formD = s.Normalize(NormalizationForm.FormD);
        var filtered = formD
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        s = new string(filtered);

        // Remove brackets and extra stuff
        s = s.Replace("[", "").Replace("]", "");
        return s.Trim();
    }

    int EditDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }
        return dp[a.Length, b.Length];
    }

    bool ContainsApprox(string text, string target, int maxDistance = 1)
    {
        text = Normalize(text);
        target = Normalize(target);

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var w in words)
        {
            if (EditDistance(w, target) <= maxDistance)
                return true;
        }
        return false;
    }

    public void OnWhisperResult(string raw)
    {
        var text = Normalize(raw);
        lastHeard = text;
        Debug.Log($"[Voice] {text}");

        // Minimal Spanish keywords – test cases
        if (ContainsApprox(text, "atacar") || ContainsApprox(text, "ataque"))
        {
            // AbilityResolver.Instance.Queue("Attack");
        }
        else if (ContainsApprox(text, "curar") || ContainsApprox(text, "cura") || ContainsApprox(text, "sanar"))
        {
            // AbilityResolver.Instance.Queue("Heal");
        }
        else if (ContainsApprox(text, "defender") || ContainsApprox(text, "defensa"))
        {
            // AbilityResolver.Instance.Queue("Defend");
        }
        else if (ContainsApprox(text, "rayo") || ContainsApprox(text, "trueno"))
        {
            // AbilityResolver.Instance.Queue("Thunderbolt");
        }

        //Use this for the combat
        if (!in_combat) return;
        if (ContainsApprox(text, movesName[0]))
        {
            combatManager.CommandMove(0);
        }
        else if (ContainsApprox(text, movesName[1]))
        {
            combatManager.CommandMove(1);
        }
        else if (ContainsApprox(text, movesName[2]))
        {
            combatManager.CommandMove(2);
        }
        else if (ContainsApprox(text, movesName[4]))
        {
            combatManager.CommandMove(3);
        }
    }
}