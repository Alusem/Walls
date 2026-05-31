using TMPro;
using UnityEngine;

/// <summary>
/// Liberation Sans SDF cobre melhor PT (ç, ã, õ) que atlas Noto parcial do projeto.
/// </summary>
public static class WallsUiFont
{
    public static TMP_FontAsset Load()
    {
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (f != null)
            return f;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSans-Black SDF");
    }
}
