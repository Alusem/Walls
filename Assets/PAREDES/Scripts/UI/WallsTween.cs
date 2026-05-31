using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Curvas e tweens leves sem DOTween.
/// </summary>
public static class WallsTween
{
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseInOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    public static IEnumerator TweenCanvasGroup(CanvasGroup cg, float from, float to, float duration, Func<float, float> ease)
    {
        if (cg == null)
            yield break;
        float u = 0f;
        while (u < 1f)
        {
            u += duration <= 0f ? 1f : Time.unscaledDeltaTime / duration;
            float e = ease(Mathf.Clamp01(u));
            cg.alpha = Mathf.Lerp(from, to, e);
            yield return null;
        }
        cg.alpha = to;
    }

    public static IEnumerator TweenScale(RectTransform rt, Vector3 from, Vector3 to, float duration, Func<float, float> ease)
    {
        if (rt == null)
            yield break;
        float u = 0f;
        while (u < 1f)
        {
            u += duration <= 0f ? 1f : Time.unscaledDeltaTime / duration;
            float e = ease(Mathf.Clamp01(u));
            rt.localScale = Vector3.LerpUnclamped(from, to, e);
            yield return null;
        }
        rt.localScale = to;
    }

    public static IEnumerator TweenAnchoredY(RectTransform rt, float fromY, float toY, float duration, Func<float, float> ease)
    {
        if (rt == null)
            yield break;
        var p = rt.anchoredPosition;
        float u = 0f;
        while (u < 1f)
        {
            u += duration <= 0f ? 1f : Time.unscaledDeltaTime / duration;
            float e = ease(Mathf.Clamp01(u));
            p.y = Mathf.LerpUnclamped(fromY, toY, e);
            rt.anchoredPosition = p;
            yield return null;
        }
        p.y = toY;
        rt.anchoredPosition = p;
    }

    public static IEnumerator TweenColor(Graphic g, Color from, Color to, float duration, Func<float, float> ease)
    {
        if (g == null)
            yield break;
        float u = 0f;
        while (u < 1f)
        {
            u += duration <= 0f ? 1f : Time.unscaledDeltaTime / duration;
            float e = ease(Mathf.Clamp01(u));
            g.color = Color.LerpUnclamped(from, to, e);
            yield return null;
        }
        g.color = to;
    }
}
