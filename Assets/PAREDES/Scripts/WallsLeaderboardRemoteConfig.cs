using UnityEngine;

/// <summary>
/// Cria: Assets → Create → Wall Rush → Leaderboard Remoto (Supabase).
/// No Supabase: SQL Editor → executa wall_rush_scores_supabase.sql (na mesma pasta deste script).
/// Project Settings → URL do projeto e anon key (Settings → API).
/// </summary>
[CreateAssetMenu(fileName = "WallsLeaderboardRemoteConfig", menuName = "Wall Rush/Leaderboard Remoto (Supabase)")]
public sealed class WallsLeaderboardRemoteConfig : ScriptableObject
{
    /// <summary>Usado se o asset Resources estiver vazio ou com script em falta (YAML manual).</summary>
    public static class EmbeddedDefaults
    {
        public const string SupabaseUrl = "https://hvstyxdoilqovqszgcjh.supabase.co";
        public const string SupabasePublishableKey = "sb_publishable_RDwx1SivbjdO3BYLyaxXog_yYCTFygm";
        public const string TableName = "wall_rush_scores";
        public const string PlayerColumn = "display_name";
    }

    /// <summary>Inspector → Resources → senão valores embutidos (para o ranking funcionar sem asset válido).</summary>
    public static WallsLeaderboardRemoteConfig ResolveForRuntime(WallsLeaderboardRemoteConfig fromInspector)
    {
        if (fromInspector != null && fromInspector.IsValid())
            return fromInspector;
        var fromResources = Resources.Load<WallsLeaderboardRemoteConfig>("WallsLeaderboardRemoteConfig");
        if (fromResources != null && fromResources.IsValid())
            return fromResources;

        var inst = CreateInstance<WallsLeaderboardRemoteConfig>();
        inst.supabaseUrl = EmbeddedDefaults.SupabaseUrl;
        inst.supabaseAnonKey = EmbeddedDefaults.SupabasePublishableKey;
        inst.tableName = EmbeddedDefaults.TableName;
        inst.playerNameColumn = EmbeddedDefaults.PlayerColumn;
        return inst;
    }

    [Tooltip("Ex.: https://abcdefgh.supabase.co (sem barra no fim)")]
    public string supabaseUrl = "";

    [Tooltip("Chave publishable (sb_publishable_…) ou anon JWT (eyJ…). Nunca uses sb_secret / service_role no cliente.")]
    public string supabaseAnonKey = "";

    [Tooltip("Nome da tabela criada no SQL (predefinido: wall_rush_scores).")]
    public string tableName = "wall_rush_scores";

    [Tooltip("Nome da coluna do jogador na tabela: player_name ou display_name (tem de coincidir com o SQL).")]
    public string playerNameColumn = "display_name";

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(supabaseUrl)
               && !string.IsNullOrWhiteSpace(supabaseAnonKey)
               && !string.IsNullOrWhiteSpace(tableName)
               && IsSafeIdentifier(tableName.Trim())
               && IsSafeIdentifier(PlayerNameColumnTrimmed());
    }

    static bool IsSafeIdentifier(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        foreach (var c in s)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_'))
                return false;
        }

        return true;
    }

    public string PlayerNameColumnTrimmed()
    {
        if (string.IsNullOrWhiteSpace(playerNameColumn))
            return "display_name";
        return playerNameColumn.Trim();
    }

    public string NormalizedBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(supabaseUrl))
            return "";
        return supabaseUrl.Trim().TrimEnd('/');
    }
}
