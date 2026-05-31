using UnityEngine;

public static class WallsGamePrefs
{
    const string ScreenShakeKey = "Walls_ScreenShake";

    /// <summary>Última cor de fundo da partida (menu reutiliza ao voltar).</summary>
    public static class LastRunBackground
    {
        const string R = "Walls_LastBg_R";
        const string G = "Walls_LastBg_G";
        const string B = "Walls_LastBg_B";
        const string Has = "Walls_LastBg_Has";

        public static bool TryGet(out Color c)
        {
            c = default;
            if (PlayerPrefs.GetInt(Has, 0) == 0)
                return false;
            c = new Color(PlayerPrefs.GetFloat(R), PlayerPrefs.GetFloat(G), PlayerPrefs.GetFloat(B), 1f);
            return true;
        }

        public static void Save(Color background)
        {
            PlayerPrefs.SetFloat(R, background.r);
            PlayerPrefs.SetFloat(G, background.g);
            PlayerPrefs.SetFloat(B, background.b);
            PlayerPrefs.SetInt(Has, 1);
            PlayerPrefs.Save();
        }
    }

    public static bool ScreenShakeEnabled
    {
        get => PlayerPrefs.GetInt(ScreenShakeKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ScreenShakeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
