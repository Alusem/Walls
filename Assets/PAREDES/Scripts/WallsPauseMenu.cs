using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botão de pausa no canto superior direito, painel Retomar/Menu e contagem 3-2-1 ao retomar.
/// </summary>
public class WallsPauseMenu : MonoBehaviour
{
    public static bool BlockGameplayInput { get; private set; }

    GameManager _gm;
    Button _pauseButton;
    GameObject _pauseOverlayRoot;
    GameObject _countdownRoot;
    TextMeshProUGUI _countdownLabel;
    Image _overlayDim;
    Image _resumeBtnGraphic;
    Image _menuBtnGraphic;
    TMP_Text _resumeLabel;
    TMP_Text _menuLabel;
    TMP_Text _pauseTitleLabel;
    Coroutine _resumeRoutine;
    bool _paused;
    Image _pauseBarLeft;
    Image _pauseBarRight;

    static Sprite _pauseIconSprite;

    static Sprite PauseIconSprite()
    {
        if (_pauseIconSprite != null)
            return _pauseIconSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _pauseIconSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return _pauseIconSprite;
    }

    void Awake()
    {
        _gm = GetComponent<GameManager>();
    }

    void Start()
    {
        BuildUiIfNeeded();
        ThemeManager.ThemeChanged += OnTheme;
        OnTheme(ThemeManager.Palette);
    }

    void OnDestroy()
    {
        ThemeManager.ThemeChanged -= OnTheme;
    }

    void Update()
    {
        if (_pauseButton == null)
            return;
        bool gameOver = IsGameOverState();
        bool showCorner = _gm != null && _gm.isStarted && !gameOver && _resumeRoutine == null && !_paused;
        _pauseButton.gameObject.SetActive(showCorner);
    }

    bool IsGameOverState()
    {
        if (_gm == null)
            return true;
        if (_gm.GameOverPanel != null && _gm.GameOverPanel.activeSelf)
            return true;
        if (_gm.GameOverEffectPanel != null && _gm.GameOverEffectPanel.activeSelf)
            return true;
        return false;
    }

    void BuildUiIfNeeded()
    {
        if (_gm == null)
            return;
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null || canvasGo.GetComponent<Canvas>() == null)
            return;
        if (canvasGo.transform.Find("PauseHudRoot") != null)
            return;

        var hud = new GameObject("PauseHudRoot");
        hud.transform.SetParent(canvasGo.transform, false);
        hud.transform.SetAsLastSibling();
        var hudRt = hud.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero;
        hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = Vector2.zero;
        hudRt.offsetMax = Vector2.zero;

        TMP_FontAsset font = WallsUiFont.Load();
        if (font == null && _gm.GameOverPanel != null)
        {
            var restartTf = _gm.GameOverPanel.transform.Find("RestartButton");
            if (restartTf != null)
            {
                var existing = restartTf.GetComponentInChildren<TMP_Text>(true);
                if (existing != null)
                    font = existing.font;
            }
        }

        _pauseButton = CreateCornerPauseButton(hud.transform);
        _pauseButton.onClick.AddListener(OnPauseClicked);

        _pauseOverlayRoot = CreatePauseOverlay(hud.transform, font);
        _pauseOverlayRoot.SetActive(false);

        _countdownRoot = CreateCountdownOverlay(hud.transform, font);
        _countdownRoot.SetActive(false);
    }

    Button CreateCornerPauseButton(Transform parent)
    {
        var go = new GameObject("PauseButton");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(112f, 112f);
        rt.anchoredPosition = new Vector2(-28f, -28f);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.22f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var sprite = PauseIconSprite();
        const float barW = 10f;
        const float barH = 40f;
        const float gap = 12f;
        var half = (barW + gap) * 0.5f;

        _pauseBarLeft = CreatePauseBar(go.transform, sprite, new Vector2(-half, 0f), barW, barH);
        _pauseBarRight = CreatePauseBar(go.transform, sprite, new Vector2(half, 0f), barW, barH);

        return btn;
    }

    static Image CreatePauseBar(Transform parent, Sprite sprite, Vector2 anchoredPos, float w, float h)
    {
        var bar = new GameObject("PauseBar");
        bar.transform.SetParent(parent, false);
        var brt = bar.AddComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(w, h);
        brt.anchoredPosition = anchoredPos;
        var barImg = bar.AddComponent<Image>();
        barImg.sprite = sprite;
        barImg.type = Image.Type.Simple;
        barImg.raycastTarget = false;
        barImg.color = new Color(0.96f, 0.97f, 1f, 0.95f);
        return barImg;
    }

    GameObject CreatePauseOverlay(Transform parent, TMP_FontAsset font)
    {
        var root = new GameObject("PauseOverlay");
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        _overlayDim = root.AddComponent<Image>();
        _overlayDim.raycastTarget = true;
        _overlayDim.color = new Color(0.02f, 0.02f, 0.06f, 0.78f);

        var card = new GameObject("PauseCard");
        card.transform.SetParent(root.transform, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(720f, 520f);
        cardRt.anchoredPosition = Vector2.zero;

        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(1f, 1f, 1f, 0.2f);
        cardImg.raycastTarget = false;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(card.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 100f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        _pauseTitleLabel = titleGo.AddComponent<TextMeshProUGUI>();
        _pauseTitleLabel.text = "Pausa";
        _pauseTitleLabel.fontSize = 56;
        _pauseTitleLabel.alignment = TextAlignmentOptions.Center;
        _pauseTitleLabel.color = Color.white;
        if (font != null)
            _pauseTitleLabel.font = font;

        _resumeBtnGraphic = CreateMenuButton(card.transform, "ResumeButton", "Retomar", new Vector2(0f, 40f), font, OnResumeClicked);
        _resumeLabel = _resumeBtnGraphic.GetComponentInChildren<TMP_Text>(true);

        _menuBtnGraphic = CreateMenuButton(card.transform, "MenuButtonPause", "Menu", new Vector2(0f, -140f), font, OnMenuFromPauseClicked);
        _menuLabel = _menuBtnGraphic.GetComponentInChildren<TMP_Text>(true);

        return root;
    }

    static Image CreateMenuButton(Transform parent, string name, string label, Vector2 yPos, TMP_FontAsset font, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(620f, 120f);
        rt.anchoredPosition = yPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        if (font != null)
            tmp.font = font;

        return img;
    }

    GameObject CreateCountdownOverlay(Transform parent, TMP_FontAsset font)
    {
        var root = new GameObject("CountdownOverlay");
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dim = root.AddComponent<Image>();
        dim.raycastTarget = true;
        dim.color = new Color(0.02f, 0.02f, 0.06f, 0.55f);

        var textGo = new GameObject("Count");
        textGo.transform.SetParent(root.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(800f, 400f);
        trt.anchoredPosition = Vector2.zero;
        _countdownLabel = textGo.AddComponent<TextMeshProUGUI>();
        _countdownLabel.text = "";
        _countdownLabel.fontSize = 220;
        _countdownLabel.alignment = TextAlignmentOptions.Center;
        _countdownLabel.color = Color.white;
        if (font != null)
            _countdownLabel.font = font;

        return root;
    }

    void OnTheme(ThemePalette p)
    {
        if (_overlayDim != null)
        {
            var c = p.OverlayDim;
            c.a = Mathf.Max(c.a, 0.72f);
            _overlayDim.color = c;
        }
        if (_pauseButton != null)
        {
            var img = _pauseButton.GetComponent<Image>();
            if (img != null)
            {
                var c = p.GlassButton;
                c.a = Mathf.Max(c.a, 0.28f);
                img.color = c;
            }
            var barColor = p.TextPrimary;
            if (_pauseBarLeft != null)
                _pauseBarLeft.color = barColor;
            if (_pauseBarRight != null)
                _pauseBarRight.color = barColor;
        }
        var card = _pauseOverlayRoot != null ? _pauseOverlayRoot.transform.Find("PauseCard") : null;
        if (card != null)
        {
            var cardImg = card.GetComponent<Image>();
            if (cardImg != null)
            {
                var c = p.ModalCard;
                c.a = Mathf.Max(c.a, 0.55f);
                cardImg.color = c;
            }
        }
        if (_pauseTitleLabel != null)
            _pauseTitleLabel.color = p.TextPrimary;
        ApplyButtonTheme(_resumeBtnGraphic, _resumeLabel, p);
        ApplyButtonTheme(_menuBtnGraphic, _menuLabel, p);
        if (_countdownRoot != null)
        {
            var dim = _countdownRoot.GetComponent<Image>();
            if (dim != null)
            {
                var c = p.OverlayDim;
                c.a = Mathf.Clamp(c.a * 0.85f, 0.45f, 0.75f);
                dim.color = c;
            }
        }
        if (_countdownLabel != null)
            _countdownLabel.color = p.TextPrimary;
    }

    static void ApplyButtonTheme(Image img, TMP_Text label, ThemePalette p)
    {
        if (img == null)
            return;
        var c = p.ModalCard;
        c.a = Mathf.Max(c.a, 0.55f);
        img.color = c;
        if (label != null)
            label.color = p.TextPrimary;
    }

    void OnPauseClicked()
    {
        if (_paused || _resumeRoutine != null || IsGameOverState())
            return;
        if (_gm == null || !_gm.isStarted)
            return;

        _paused = true;
        Time.timeScale = 0f;
        BlockGameplayInput = true;
        if (_pauseOverlayRoot != null)
            _pauseOverlayRoot.SetActive(true);
    }

    void OnResumeClicked()
    {
        if (!_paused || _resumeRoutine != null)
            return;
        if (_pauseOverlayRoot != null)
            _pauseOverlayRoot.SetActive(false);
        _resumeRoutine = StartCoroutine(ResumeCountdownRoutine());
    }

    void OnMenuFromPauseClicked()
    {
        if (!_paused && _resumeRoutine == null)
            return;
        if (_resumeRoutine != null)
        {
            StopCoroutine(_resumeRoutine);
            _resumeRoutine = null;
        }
        _paused = false;
        if (_pauseOverlayRoot != null)
            _pauseOverlayRoot.SetActive(false);
        if (_countdownRoot != null)
            _countdownRoot.SetActive(false);
        Time.timeScale = 1f;
        BlockGameplayInput = false;
        if (_gm != null)
            _gm.GoToMenu();
    }

    IEnumerator ResumeCountdownRoutine()
    {
        _paused = false;
        BlockGameplayInput = true;
        Time.timeScale = 0f;
        if (_countdownRoot != null)
            _countdownRoot.SetActive(true);

        for (var n = 3; n >= 1; n--)
        {
            if (_countdownLabel != null)
                _countdownLabel.text = n.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        if (_countdownLabel != null)
            _countdownLabel.text = "";
        if (_countdownRoot != null)
            _countdownRoot.SetActive(false);

        Time.timeScale = 1f;
        BlockGameplayInput = false;
        _resumeRoutine = null;
    }
}
