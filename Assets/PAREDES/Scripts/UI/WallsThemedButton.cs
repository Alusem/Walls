using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class WallsThemedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] Image _background;
    [SerializeField] TextMeshProUGUI _label;
    RectTransform _rect;

    Color _glass;
    Color _glassHover;
    Color _text;

    bool _hover;
    bool _pressed;
    Coroutine _scaleRoutine;

    public event Action Clicked;

    void Awake()
    {
        _rect = transform as RectTransform;
        if (_background == null)
            _background = GetComponent<Image>();
        _background.raycastTarget = true;
    }

    void OnEnable()
    {
        ThemeManager.ThemeChanged += OnTheme;
        OnTheme(ThemeManager.Palette);
    }

    void OnDisable()
    {
        ThemeManager.ThemeChanged -= OnTheme;
        if (_scaleRoutine != null)
        {
            StopCoroutine(_scaleRoutine);
            _scaleRoutine = null;
        }
        if (_rect != null)
            _rect.localScale = Vector3.one;
    }

    public void BindLabel(TextMeshProUGUI label)
    {
        _label = label;
    }

    void OnTheme(ThemePalette p)
    {
        _glass = p.GlassButton;
        _glassHover = p.GlassButtonHover;
        _text = p.TextPrimary;
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (_background == null)
            return;
        if (_pressed)
            _background.color = Color.Lerp(_glass, _glassHover, 0.5f);
        else if (_hover)
            _background.color = _glassHover;
        else
            _background.color = _glass;
        if (_label != null)
            _label.color = _text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover = true;
        RefreshVisual();
        RunScale(Vector3.one * 1.06f, 0.12f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
        _pressed = false;
        RefreshVisual();
        RunScale(Vector3.one, 0.14f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        RefreshVisual();
        RunScale(Vector3.one * 0.94f, 0.06f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        RefreshVisual();
        RunScale(_hover ? Vector3.one * 1.06f : Vector3.one, 0.1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        Clicked?.Invoke();
    }

    void RunScale(Vector3 target, float duration)
    {
        if (_rect == null || !isActiveAndEnabled)
            return;
        if (_scaleRoutine != null)
            StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScaleTo(target, duration));
    }

    System.Collections.IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 from = _rect.localScale;
        float u = 0f;
        while (u < 1f)
        {
            u += duration <= 0f ? 1f : Time.unscaledDeltaTime / duration;
            float e = WallsTween.EaseOutBack(Mathf.Clamp01(u));
            _rect.localScale = Vector3.LerpUnclamped(from, target, Mathf.Min(e, 1f));
            yield return null;
        }
        _rect.localScale = target;
        _scaleRoutine = null;
    }
}
