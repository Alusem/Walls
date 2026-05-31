using System;
using UnityEngine;

/// <summary>
/// Hub global de tema: o jogo define a cor base (ex. fundo da câmara), a UI reage.
/// </summary>
public static class ThemeManager
{
    public static ThemePalette Palette { get; private set; } = new ThemePalette(new Color(0.45f, 0.45f, 0.48f));

    public static event Action<ThemePalette> ThemeChanged;

    public static void ApplyBaseColor(Color worldBackgroundColor)
    {
        Palette = new ThemePalette(worldBackgroundColor);
        ThemeChanged?.Invoke(Palette);
    }
}
