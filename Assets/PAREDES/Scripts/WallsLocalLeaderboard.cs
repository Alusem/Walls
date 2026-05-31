using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Top pontuações neste dispositivo (PlayerPrefs). Para ranking online seria preciso backend ou Play Games / Firebase.
/// </summary>
public static class WallsLocalLeaderboard
{
    const string PrefsKey = "Walls_LeaderboardV1";
    const string LegacyBestKey = "BestScore";
    const string ClearedForGlobalUiKey = "WallsLocalLeaderboardClearedV1";
    public const int MaxEntries = 10;

    /// <summary>Apaga a lista local antiga uma vez (o menu passou a usar só ranking online).</summary>
    public static void ClearStoredListOnceForGlobalRankingUi()
    {
        if (PlayerPrefs.GetInt(ClearedForGlobalUiKey, 0) != 0)
            return;
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.SetInt(ClearedForGlobalUiKey, 1);
        PlayerPrefs.Save();
    }

    public static void SubmitScore(int score)
    {
        if (score <= 0)
            return;
        var list = LoadListInternal();
        list.Add(score);
        list.Sort((a, b) => b.CompareTo(a));
        while (list.Count > MaxEntries)
            list.RemoveAt(list.Count - 1);
        SaveList(list);
    }

    static List<int> LoadListInternal()
    {
        var list = new List<int>();
        var raw = PlayerPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), out var v) && v > 0)
                    list.Add(v);
            }
        }

        if (list.Count == 0)
        {
            var best = PlayerPrefs.GetInt(LegacyBestKey, 0);
            if (best > 0)
            {
                list.Add(best);
                SaveList(list);
            }
        }
        else
        {
            list.Sort((a, b) => b.CompareTo(a));
            if (list.Count > MaxEntries)
                list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        }

        return list;
    }

    static void SaveList(List<int> list)
    {
        if (list == null || list.Count == 0)
        {
            PlayerPrefs.SetString(PrefsKey, "");
            PlayerPrefs.Save();
            return;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < list.Count && i < MaxEntries; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(list[i]);
        }

        PlayerPrefs.SetString(PrefsKey, sb.ToString());
        PlayerPrefs.Save();
    }

    public static IReadOnlyList<int> GetTopScores()
    {
        return LoadListInternal();
    }
}
