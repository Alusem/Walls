using UnityEngine;

/// <summary>
/// Paleta derivada da cor de fundo do jogo (bola / câmara).
/// </summary>
public readonly struct ThemePalette
{
    public readonly Color Base;
    public readonly Color Darker;
    public readonly Color Lighter;
    public readonly Color PanelTint;
    public readonly Color GlassButton;
    public readonly Color GlassButtonHover;
    public readonly Color TextPrimary;
    public readonly Color TextSecondary;
    public readonly Color SliderTrack;
    public readonly Color SliderFill;
    public readonly Color OverlayDim;
    public readonly Color ModalCard;

    public ThemePalette(Color baseColor)
    {
        Base = baseColor;
        Darker = ThemeColorUtility.GetDarker(baseColor, 0.42f);
        Lighter = ThemeColorUtility.GetLighter(baseColor, 0.38f);
        PanelTint = Color.Lerp(Darker, Color.black, 0.35f);
        PanelTint.a = 0.92f;
        GlassButton = new Color(Lighter.r, Lighter.g, Lighter.b, 0.22f);
        GlassButtonHover = new Color(Lighter.r, Lighter.g, Lighter.b, 0.38f);
        TextPrimary = ThemeColorUtility.ReadableTextOn(baseColor);
        TextSecondary = new Color(TextPrimary.r, TextPrimary.g, TextPrimary.b, 0.82f);
        SliderTrack = new Color(TextPrimary.r, TextPrimary.g, TextPrimary.b, 0.18f);
        SliderFill = Color.Lerp(Lighter, Color.white, 0.15f);
        SliderFill.a = 0.95f;
        OverlayDim = new Color(0.02f, 0.02f, 0.06f, 0.72f);
        ModalCard = Color.Lerp(Lighter, Color.white, 0.25f);
        ModalCard.a = 0.18f;
    }
}

public static class ThemeColorUtility
{
    public static Color GetDarker(Color c, float amount = 0.35f)
    {
        amount = Mathf.Clamp01(amount);
        return Color.Lerp(c, Color.black, amount);
    }

    public static Color GetLighter(Color c, float amount = 0.35f)
    {
        amount = Mathf.Clamp01(amount);
        return Color.Lerp(c, Color.white, amount);
    }

    /// <summary>Cor do texto da UI (menu/definições) — sempre clara, sem alternar com o matiz do fundo.</summary>
    public static Color ReadableTextOn(Color background)
    {
        return new Color(0.96f, 0.97f, 1f, 1f);
    }
}

/// <summary>
/// Cores de fundo do jogo (HSV igual ao <see cref="Player"/>).
/// </summary>
public static class WallsGameColors
{
    public const float BackgroundSaturation = 0.6f;
    public const float BackgroundValue = 0.8f;

    /// <summary>Matiz inicial em passos de 0.1, como no arranque do jogador.</summary>
    public static float RandomHueStep()
    {
        return Random.Range(0, 10) / 10f;
    }

    public static Color ColorFromHue(float hue01)
    {
        return Color.HSVToRGB(Mathf.Repeat(hue01, 1f), BackgroundSaturation, BackgroundValue);
    }

    public static Color RandomBackgroundColor()
    {
        return ColorFromHue(RandomHueStep());
    }
}
