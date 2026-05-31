using UnityEngine;

public static class WallsAudioPrefs
{
    public const string MusicLinearKey = "WallsAudio_MusicLinear";
    public const string SfxLinearKey = "WallsAudio_SfxLinear";
    public const string FromMenuKey = "Walls_FromMenu";

    public static float MusicLinear
    {
        get => PlayerPrefs.GetFloat(MusicLinearKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MusicLinearKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public static float SfxLinear
    {
        get => PlayerPrefs.GetFloat(SfxLinearKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxLinearKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
