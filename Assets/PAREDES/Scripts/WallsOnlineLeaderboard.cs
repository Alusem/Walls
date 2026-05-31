using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Top 10 mundial via Supabase REST (anon key). Requer asset WallsLeaderboardRemoteConfig preenchido.
/// </summary>
public static class WallsOnlineLeaderboard
{
    public const string DisplayNamePrefsKey = "WallsLeaderboardDisplayName";
    public const string BestSubmittedToServerKey = "WallsOnlineBestSubmitted";
    public const string CachedTopPrefsKey = "WallsOnlineCachedTopV1";
    public const int DisplayNameMaxLength = 24;
    public const int TopLimit = 10;
    /// <summary>Linhas pedidas ao servidor antes de deduplicar por apelido.</summary>
    public const int FetchPoolLimit = 300;

    static WallsLeaderboardRemoteConfig _config;

    public static void SetRemoteConfig(WallsLeaderboardRemoteConfig cfg) => _config = cfg;

    public static bool IsRemoteConfigured() => _config != null && _config.IsValid();

    /// <summary>Nome usado no ranking (Definições → Apelido). Vazio → "Jogador".</summary>
    public static string GetDisplayNameForRankings()
    {
        return SanitizeName(PlayerPrefs.GetString(DisplayNamePrefsKey, ""));
    }

    public static void SaveDisplayNameFromSettings(string raw)
    {
        if (raw == null)
            raw = "";
        raw = raw.Trim();
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (c == '"' || c == '\\' || c == '\n' || c == '\r')
                continue;
            sb.Append(c);
            if (sb.Length >= DisplayNameMaxLength)
                break;
        }

        raw = sb.ToString();
        PlayerPrefs.SetString(DisplayNamePrefsKey, raw);
        PlayerPrefs.Save();
    }

    static string SanitizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Jogador";
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw.Trim())
        {
            if (c == '"' || c == '\\' || c == '\n' || c == '\r')
                continue;
            sb.Append(c);
            if (sb.Length >= DisplayNameMaxLength)
                break;
        }

        return sb.Length > 0 ? sb.ToString() : "Jogador";
    }

    /// <summary>
    /// Envia só se esta partida for o teu recorde pessoal (igual a BestScore) e melhor do que o último envio ao servidor.
    /// Assim só a tua melhor pontuação entra no ranking online (por aparelho).
    /// </summary>
    public static void TrySubmitScoreIfPersonalBest(int runScore)
    {
        if (!IsRemoteConfigured() || runScore <= 0)
            return;
        var allTimeBest = PlayerPrefs.GetInt("BestScore", 0);
        if (runScore != allTimeBest)
            return;
        var lastSubmitted = PlayerPrefs.GetInt(BestSubmittedToServerKey, 0);
        if (runScore <= lastSubmitted)
            return;
        WallsNetHost.Run(SubmitScoreRoutine(runScore));
    }

    static IEnumerator SubmitScoreRoutine(int score)
    {
        var cfg = _config;
        var url = $"{cfg.NormalizedBaseUrl()}/rest/v1/{cfg.tableName.Trim()}";
        var nameCol = cfg.PlayerNameColumnTrimmed();
        var body = "{\"score\":" + score + ",\"" + nameCol + "\":\""
                   + JsonEscape(GetDisplayNameForRankings()) + "\"}";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            var bodyRaw = Encoding.UTF8.GetBytes(body);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 15;
            req.SetRequestHeader("Content-Type", "application/json");
            ApplySupabaseApiHeaders(req, cfg.supabaseAnonKey.Trim());
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning("[Wall Rush] Leaderboard submit falhou: " + BuildFailureSummary(req));
                yield break;
            }
        }

        PlayerPrefs.SetInt(BestSubmittedToServerKey, score);
        PlayerPrefs.Save();
    }

    public static IEnumerator FetchTopCoroutine(Action<WallRushLeaderboardFetchResult> onComplete)
    {
        var result = new WallRushLeaderboardFetchResult();
        if (!IsRemoteConfigured())
        {
            result.failureReason = LeaderboardFetchFailureReason.NotConfigured;
            result.detailMessage = "Ranking online não configurado.";
            onComplete?.Invoke(result);
            yield break;
        }

        var cfg = _config;
        var nameCol = cfg.PlayerNameColumnTrimmed();
        var url = $"{cfg.NormalizedBaseUrl()}/rest/v1/{cfg.tableName.Trim()}" +
                  "?select=score," + nameCol + "&order=score.desc&limit=" + FetchPoolLimit;

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            ApplySupabaseApiHeaders(req, cfg.supabaseAnonKey.Trim());

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            var ok = req.result == UnityWebRequest.Result.Success;
#else
            var ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (!ok)
            {
                FillFailureFromRequest(result, req);
                TryAttachCachedRanking(result);
                Debug.LogWarning("[Wall Rush] Leaderboard fetch falhou: " + BuildFailureSummary(req));
                onComplete?.Invoke(result);
                yield break;
            }

            var text = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text) || text == "[]")
            {
                result.success = true;
                result.lines = new List<WallRushLeaderboardLine>();
                SaveCachedTop(result.lines);
                onComplete?.Invoke(result);
                yield break;
            }

            try
            {
                var wrapped = "{\"rows\":" + text + "}";
                var parsed = JsonUtility.FromJson<ScoreRowsWrapper>(wrapped);
                if (parsed?.rows == null)
                    throw new FormatException("rows ausente no payload do ranking.");

                foreach (var row in parsed.rows)
                {
                    if (row == null || row.score <= 0)
                        continue;
                    var rawName = !string.IsNullOrWhiteSpace(row.player_name)
                        ? row.player_name
                        : row.display_name;
                    var name = string.IsNullOrWhiteSpace(rawName) ? "Jogador" : rawName.Trim();
                    if (name.Length > DisplayNameMaxLength)
                        name = name.Substring(0, DisplayNameMaxLength);
                    result.lines.Add(new WallRushLeaderboardLine { displayName = name, score = row.score });
                }
            }
            catch (Exception e)
            {
                result.failureReason = LeaderboardFetchFailureReason.InvalidResponse;
                result.detailMessage = "A resposta do ranking veio num formato inesperado.";
                result.debugMessage = e.Message;
                TryAttachCachedRanking(result);
                Debug.LogWarning("[Wall Rush] Leaderboard parse: " + e.Message);
                onComplete?.Invoke(result);
                yield break;
            }

            result.lines = DedupeBestPerDisplayName(result.lines);
            result.success = true;
            SaveCachedTop(result.lines);
            onComplete?.Invoke(result);
        }
    }

    /// <summary>Uma linha por apelido: fica só a melhor pontuação de cada um.</summary>
    public static List<WallRushLeaderboardLine> DedupeBestPerDisplayName(List<WallRushLeaderboardLine> raw)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (raw != null)
        {
            foreach (var line in raw)
            {
                var key = string.IsNullOrWhiteSpace(line.displayName) ? "Jogador" : line.displayName.Trim();
                if (!map.TryGetValue(key, out var best) || line.score > best)
                    map[key] = line.score;
            }
        }

        var result = new List<WallRushLeaderboardLine>(map.Count);
        foreach (var kv in map)
            result.Add(new WallRushLeaderboardLine { displayName = kv.Key, score = kv.Value });
        result.Sort((a, b) => b.score.CompareTo(a.score));
        if (result.Count > TopLimit)
            result.RemoveRange(TopLimit, result.Count - TopLimit);
        return result;
    }

    static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    static void ApplySupabaseApiHeaders(UnityWebRequest req, string apiKey)
    {
        if (req == null || string.IsNullOrWhiteSpace(apiKey))
            return;
        apiKey = apiKey.Trim();
        req.SetRequestHeader("apikey", apiKey);
        if (LooksLikeJwt(apiKey))
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);
    }

    static bool LooksLikeJwt(string apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey) && apiKey.TrimStart().StartsWith("eyJ", StringComparison.Ordinal);
    }

    static void FillFailureFromRequest(WallRushLeaderboardFetchResult result, UnityWebRequest req)
    {
        result.httpStatusCode = req != null ? req.responseCode : 0;
        result.failureReason = ClassifyFailureReason(req);
        result.detailMessage = BuildFailureUserMessage(result.failureReason);
        result.debugMessage = BuildFailureSummary(req);
    }

    static LeaderboardFetchFailureReason ClassifyFailureReason(UnityWebRequest req)
    {
        if (req == null)
            return LeaderboardFetchFailureReason.Unknown;

        var code = req.responseCode;
        var err = (req.error ?? "").ToLowerInvariant();
        var body = SafeToLower(req.downloadHandler != null ? req.downloadHandler.text : "");

        if (code == 401)
            return LeaderboardFetchFailureReason.Unauthorized;
        if (code == 403)
            return LeaderboardFetchFailureReason.Forbidden;
        if (code == 404)
            return LeaderboardFetchFailureReason.NotFound;
        if (code == 408 || err.Contains("timed out") || err.Contains("timeout"))
            return LeaderboardFetchFailureReason.Timeout;
        if (code == 429)
            return LeaderboardFetchFailureReason.RateLimited;
        if (body.Contains("paused") || body.Contains("paus") || body.Contains("inactive"))
            return LeaderboardFetchFailureReason.ServicePaused;
        if (code >= 500)
            return LeaderboardFetchFailureReason.ServerUnavailable;
        if (code == 0)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return LeaderboardFetchFailureReason.NetworkUnavailable;
            return LeaderboardFetchFailureReason.NetworkUnavailable;
        }
        return LeaderboardFetchFailureReason.Unknown;
    }

    static string BuildFailureUserMessage(LeaderboardFetchFailureReason reason)
    {
        switch (reason)
        {
            case LeaderboardFetchFailureReason.NotConfigured:
                return "Ranking online não configurado.";
            case LeaderboardFetchFailureReason.NetworkUnavailable:
                return "Sem ligação com a internet ou com o servidor do ranking.";
            case LeaderboardFetchFailureReason.Timeout:
                return "O ranking demorou demasiado tempo a responder.";
            case LeaderboardFetchFailureReason.Unauthorized:
                return "A chave pública do Supabase foi recusada.";
            case LeaderboardFetchFailureReason.Forbidden:
                return "O Supabase bloqueou a leitura do ranking.";
            case LeaderboardFetchFailureReason.NotFound:
                return "A tabela do ranking não foi encontrada no Supabase.";
            case LeaderboardFetchFailureReason.RateLimited:
                return "O Supabase limitou temporariamente os pedidos do ranking.";
            case LeaderboardFetchFailureReason.ServicePaused:
                return "O projeto Supabase parece estar pausado ou inativo.";
            case LeaderboardFetchFailureReason.ServerUnavailable:
                return "O servidor do ranking está indisponível neste momento.";
            case LeaderboardFetchFailureReason.InvalidResponse:
                return "O ranking respondeu com dados inválidos.";
            default:
                return "Não foi possível atualizar o ranking agora.";
        }
    }

    static string BuildFailureSummary(UnityWebRequest req)
    {
        if (req == null)
            return "pedido nulo";

        var body = req.downloadHandler != null ? req.downloadHandler.text : "";
        if (!string.IsNullOrEmpty(body) && body.Length > 220)
            body = body.Substring(0, 220) + "...";

        return "HTTP " + req.responseCode
               + " | error=" + (req.error ?? "none")
               + (string.IsNullOrWhiteSpace(body) ? "" : " | body=" + body);
    }

    static string SafeToLower(string value)
    {
        return string.IsNullOrEmpty(value) ? "" : value.ToLowerInvariant();
    }

    static void SaveCachedTop(List<WallRushLeaderboardLine> lines)
    {
        var cache = new LeaderboardCacheWrapper
        {
            fetchedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            rows = lines != null ? lines.ToArray() : Array.Empty<WallRushLeaderboardLine>()
        };

        PlayerPrefs.SetString(CachedTopPrefsKey, JsonUtility.ToJson(cache));
        PlayerPrefs.Save();
    }

    static void TryAttachCachedRanking(WallRushLeaderboardFetchResult result)
    {
        if (result == null)
            return;

        if (TryLoadCachedTop(out var cachedLines, out var fetchedAtUnixSeconds))
        {
            result.usedCache = true;
            result.cacheFetchedAtUnixSeconds = fetchedAtUnixSeconds;
            result.lines = cachedLines;
        }
    }

    static bool TryLoadCachedTop(out List<WallRushLeaderboardLine> lines, out long fetchedAtUnixSeconds)
    {
        lines = new List<WallRushLeaderboardLine>();
        fetchedAtUnixSeconds = 0;

        var raw = PlayerPrefs.GetString(CachedTopPrefsKey, "");
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            var cache = JsonUtility.FromJson<LeaderboardCacheWrapper>(raw);
            if (cache == null || cache.rows == null)
                return false;

            fetchedAtUnixSeconds = cache.fetchedAtUnixSeconds;
            lines = new List<WallRushLeaderboardLine>(cache.rows);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Wall Rush] Leaderboard cache inválido: " + e.Message);
            return false;
        }
    }

    [Serializable]
    class ScoreRowsWrapper
    {
        public ScoreSelectRow[] rows;
    }

    [Serializable]
    class ScoreSelectRow
    {
        public int score;
        public string display_name;
        public string player_name;
    }

    [Serializable]
    class LeaderboardCacheWrapper
    {
        public long fetchedAtUnixSeconds;
        public WallRushLeaderboardLine[] rows;
    }
}

public enum LeaderboardFetchFailureReason
{
    None,
    NotConfigured,
    NetworkUnavailable,
    Timeout,
    Unauthorized,
    Forbidden,
    NotFound,
    RateLimited,
    ServicePaused,
    ServerUnavailable,
    InvalidResponse,
    Unknown
}

[Serializable]
public sealed class WallRushLeaderboardFetchResult
{
    public bool success;
    public bool usedCache;
    public long httpStatusCode;
    public long cacheFetchedAtUnixSeconds;
    public LeaderboardFetchFailureReason failureReason;
    public string detailMessage;
    public string debugMessage;
    public List<WallRushLeaderboardLine> lines = new List<WallRushLeaderboardLine>();
}

[Serializable]
public struct WallRushLeaderboardLine
{
    public string displayName;
    public int score;
}
