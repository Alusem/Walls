using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menu principal + modal de definições, tema dinâmico e animações.
/// </summary>
public class WallsUIManager : MonoBehaviour
{
    [SerializeField] AudioClip _uiClickClip;
    [SerializeField] WallsLeaderboardRemoteConfig _remoteLeaderboardConfig;

    TMP_FontAsset _font;
    Image _menuBackdrop;
    RectTransform _titleRect;
    TextMeshProUGUI _titleTmp;
    float _titleBaseY = 400f;
    CanvasGroup _mainGroup;

    GameObject _settingsOverlay;
    CanvasGroup _settingsOverlayGroup;
    Image _dimImage;
    CanvasGroup _settingsCardGroup;
    RectTransform _settingsCardRt;
    Image _settingsCardBg;
    Slider _musicSlider;
    Slider _sfxSlider;
    Image _musicFill;
    Image _sfxFill;
    Image _musicTrack;
    Image _sfxTrack;
    Toggle _shakeToggle;
    TextMeshProUGUI _shakeLabel;
    Image _shakeCheckGraphic;
    TMP_InputField _nicknameField;
    TextMeshProUGUI _nicknameInputText;
    TextMeshProUGUI _nicknamePlaceholder;
    Image _nicknameFieldBg;
    bool _settingsAnimating;

    GameObject _rankingOverlay;
    CanvasGroup _rankingOverlayGroup;
    RectTransform _rankingCardRt;
    CanvasGroup _rankingCardGroup;
    Image _rankingCardBg;
    TextMeshProUGUI _rankingBodyTmp;
    TextMeshProUGUI _rankingTitleTmp;
    Image _rankingDimImage;
    bool _rankingAnimating;

    static Sprite _whiteSprite;

    void Awake()
    {
        _font = WallsUiFont.Load();
        EnsureEventSystem();
        ThemeManager.ThemeChanged += OnGlobalTheme;
        var bg = WallsGamePrefs.LastRunBackground.TryGet(out var last)
            ? last
            : WallsGameColors.RandomBackgroundColor();
        if (Camera.main != null)
            Camera.main.backgroundColor = bg;
        ThemeManager.ApplyBaseColor(bg);
        var remote = WallsLeaderboardRemoteConfig.ResolveForRuntime(_remoteLeaderboardConfig);
        WallsOnlineLeaderboard.SetRemoteConfig(remote);
        WallsLocalLeaderboard.ClearStoredListOnceForGlobalRankingUi();
        BuildUi();
        OnGlobalTheme(ThemeManager.Palette);
        WallsAudioDirector.Instance?.SetMenuMusicAttenuation(true);
    }

    void OnDestroy()
    {
        ThemeManager.ThemeChanged -= OnGlobalTheme;
    }

    void Update()
    {
        if (_titleRect != null)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 1.75f) * 12f;
            _titleRect.anchoredPosition = new Vector2(0f, _titleBaseY + bob);
        }
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1125, 2436);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var mainPanel = new GameObject("MainMenuPanel");
        mainPanel.transform.SetParent(canvasGo.transform, false);
        _mainGroup = mainPanel.AddComponent<CanvasGroup>();
        var mainRt = mainPanel.AddComponent<RectTransform>();
        Stretch(mainRt);

        _menuBackdrop = mainPanel.AddComponent<Image>();
        _menuBackdrop.sprite = WhiteSprite();
        _menuBackdrop.type = Image.Type.Simple;
        _menuBackdrop.raycastTarget = true;

        _titleRect = CreateTmp(mainPanel.transform, "WALL RUSH", 88, new Vector2(980, 260), _titleBaseY);
        _titleTmp = _titleRect.GetComponent<TextMeshProUGUI>();
        _titleTmp.fontStyle = FontStyles.Bold;
        _titleTmp.outlineWidth = 0.15f;
        _titleTmp.outlineColor = new Color(0, 0, 0, 0.35f);

        AddShadow(_titleRect.gameObject, new Vector2(0, -6), 0.4f);

        CreateMenuButton(mainPanel.transform, "Começar", new Vector2(0, 180), () => { PlayUiClick(); Play(); });
        CreateMenuButton(mainPanel.transform, "Definições", new Vector2(0, 40), () => { PlayUiClick(); OpenSettings(); });
        CreateMenuButton(mainPanel.transform, "Ranking", new Vector2(0, -100), () => { PlayUiClick(); OpenRanking(); });
        CreateMenuButton(mainPanel.transform, "Sair", new Vector2(0, -240), () => { PlayUiClick(); Quit(); });

        BuildSettingsModal(canvasGo.transform);
        BuildRankingModal(canvasGo.transform);
    }

    void BuildSettingsModal(Transform canvas)
    {
        _settingsOverlay = new GameObject("SettingsOverlay");
        _settingsOverlay.transform.SetParent(canvas, false);
        _settingsOverlayGroup = _settingsOverlay.AddComponent<CanvasGroup>();
        _settingsOverlayGroup.alpha = 0f;
        _settingsOverlayGroup.interactable = false;
        _settingsOverlayGroup.blocksRaycasts = false;
        var overlayRt = _settingsOverlay.AddComponent<RectTransform>();
        Stretch(overlayRt);

        var dimGo = new GameObject("BackgroundOverlay");
        dimGo.transform.SetParent(_settingsOverlay.transform, false);
        var dimRt = dimGo.AddComponent<RectTransform>();
        Stretch(dimRt);
        _dimImage = dimGo.AddComponent<Image>();
        _dimImage.sprite = WhiteSprite();
        _dimImage.color = ThemeManager.Palette.OverlayDim;
        var dimBtn = dimGo.AddComponent<Button>();
        dimBtn.targetGraphic = _dimImage;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => { PlayUiClick(); CloseSettings(); });

        var card = new GameObject("SettingsPanel");
        card.transform.SetParent(_settingsOverlay.transform, false);
        _settingsCardRt = card.AddComponent<RectTransform>();
        _settingsCardRt.sizeDelta = new Vector2(680, 940);
        _settingsCardRt.anchorMin = _settingsCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        _settingsCardRt.anchoredPosition = Vector2.zero;

        _settingsCardGroup = card.AddComponent<CanvasGroup>();
        _settingsCardGroup.alpha = 0f;

        _settingsCardBg = card.AddComponent<Image>();
        _settingsCardBg.sprite = WhiteSprite();
        _settingsCardBg.type = Image.Type.Simple;
        AddShadow(card, new Vector2(0, -14), 0.55f);

        var title = CreateTmp(card.transform, "Definições", 56, new Vector2(600, 100), 360f);
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        CreateThemedNicknameField(card.transform, new Vector2(0, 235));

        _musicSlider = CreateThemedSlider(card.transform, "Música", new Vector2(0, 100), WallsAudioPrefs.MusicLinear, v =>
        {
            WallsAudioPrefs.MusicLinear = v;
            WallsAudioDirector.Instance?.RefreshVolumes();
        }, out _musicTrack, out _musicFill);

        _sfxSlider = CreateThemedSlider(card.transform, "Efeitos", new Vector2(0, -80), WallsAudioPrefs.SfxLinear, v =>
        {
            WallsAudioPrefs.SfxLinear = v;
            WallsAudioDirector.Instance?.RefreshVolumes();
        }, out _sfxTrack, out _sfxFill);

        BuildShakeToggle(card.transform, new Vector2(0, -260));

        var closeBtnRt = CreateMenuButton(card.transform, "Fechar", new Vector2(0, -380), () => { PlayUiClick(); CloseSettings(); });
        closeBtnRt.sizeDelta = new Vector2(400, 96);

        _settingsOverlay.SetActive(false);
        _settingsCardRt.localScale = Vector3.one * 0.88f;
    }

    void BuildRankingModal(Transform canvas)
    {
        _rankingOverlay = new GameObject("RankingOverlay");
        _rankingOverlay.transform.SetParent(canvas, false);
        _rankingOverlayGroup = _rankingOverlay.AddComponent<CanvasGroup>();
        _rankingOverlayGroup.alpha = 0f;
        _rankingOverlayGroup.interactable = false;
        _rankingOverlayGroup.blocksRaycasts = false;
        var overlayRt = _rankingOverlay.AddComponent<RectTransform>();
        Stretch(overlayRt);

        var dimGo = new GameObject("RankingDim");
        dimGo.transform.SetParent(_rankingOverlay.transform, false);
        var dimRt = dimGo.AddComponent<RectTransform>();
        Stretch(dimRt);
        _rankingDimImage = dimGo.AddComponent<Image>();
        _rankingDimImage.sprite = WhiteSprite();
        _rankingDimImage.color = ThemeManager.Palette.OverlayDim;
        var dimBtn = dimGo.AddComponent<Button>();
        dimBtn.targetGraphic = _rankingDimImage;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => { PlayUiClick(); CloseRanking(); });

        var card = new GameObject("RankingPanel");
        card.transform.SetParent(_rankingOverlay.transform, false);
        _rankingCardRt = card.AddComponent<RectTransform>();
        _rankingCardRt.sizeDelta = new Vector2(720, 980);
        _rankingCardRt.anchorMin = _rankingCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        _rankingCardRt.anchoredPosition = Vector2.zero;

        _rankingCardGroup = card.AddComponent<CanvasGroup>();
        _rankingCardGroup.alpha = 0f;

        _rankingCardBg = card.AddComponent<Image>();
        _rankingCardBg.sprite = WhiteSprite();
        _rankingCardBg.type = Image.Type.Simple;
        AddShadow(card, new Vector2(0, -14), 0.55f);

        var titleRt = CreateTmp(card.transform, "Melhores pontuações", 52, new Vector2(640, 90), 410f);
        _rankingTitleTmp = titleRt.GetComponent<TextMeshProUGUI>();
        _rankingTitleTmp.fontStyle = FontStyles.Bold;

        var bodyGo = new GameObject("RankingBody");
        bodyGo.transform.SetParent(card.transform, false);
        var bodyRt = bodyGo.AddComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRt.pivot = new Vector2(0.5f, 0.5f);
        bodyRt.sizeDelta = new Vector2(640, 560);
        bodyRt.anchoredPosition = new Vector2(0f, 20f);
        _rankingBodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
        _rankingBodyTmp.fontSize = 40;
        _rankingBodyTmp.alignment = TextAlignmentOptions.TopGeoAligned;
        _rankingBodyTmp.enableWordWrapping = true;
        if (_font != null)
            _rankingBodyTmp.font = _font;

        var closeBtnRt = CreateMenuButton(card.transform, "Fechar", new Vector2(0, -360), () => { PlayUiClick(); CloseRanking(); });
        closeBtnRt.sizeDelta = new Vector2(400, 96);

        _rankingOverlay.SetActive(false);
        _rankingCardRt.localScale = Vector3.one * 0.88f;
    }

    void ApplyRankingToUi(WallRushLeaderboardFetchResult result)
    {
        if (_rankingBodyTmp == null)
            return;

        if (!WallsOnlineLeaderboard.IsRemoteConfigured())
        {
            _rankingBodyTmp.text = "Ranking online não configurado.\n\nNo Unity, cria o asset Leaderboard Remoto (Supabase) com URL e chave.";
            if (_rankingTitleTmp != null)
                _rankingTitleTmp.text = "Melhores pontuações";
            return;
        }

        var globalLines = result != null ? result.lines : null;
        var hasLines = globalLines != null && globalLines.Count > 0;

        if (result != null && result.success && hasLines)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < globalLines.Count; i++)
            {
                var line = globalLines[i];
                sb.AppendLine((i + 1).ToString() + "º  " + line.displayName + "  " + line.score.ToString("N0") + " pts");
            }

            _rankingBodyTmp.text = sb.ToString().TrimEnd();
            if (_rankingTitleTmp != null)
                _rankingTitleTmp.text = "Melhores pontuações";
            return;
        }

        if (result != null && result.success && !hasLines)
        {
            _rankingBodyTmp.text = "Ainda não há pontuações.\n\nDefine o teu apelido em Definições, joga e bate o teu recorde para entrares no top "
                                   + WallsOnlineLeaderboard.TopLimit + ".";
            if (_rankingTitleTmp != null)
                _rankingTitleTmp.text = "Melhores pontuações";
            return;
        }

        if (result != null && result.usedCache && hasLines)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < globalLines.Count; i++)
            {
                var line = globalLines[i];
                sb.AppendLine((i + 1).ToString() + "º  " + line.displayName + "  " + line.score.ToString("N0") + " pts");
            }

            sb.AppendLine();
            sb.AppendLine("Mostrando a última cópia guardada do ranking.");
            sb.Append(DescribeRankingFailure(result));

            var cacheAge = FormatCacheAge(result.cacheFetchedAtUnixSeconds);
            if (!string.IsNullOrEmpty(cacheAge))
                sb.Append("\n\nÚltima sincronização: " + cacheAge + ".");

            _rankingBodyTmp.text = sb.ToString().TrimEnd();
            if (_rankingTitleTmp != null)
                _rankingTitleTmp.text = "Melhores pontuações (cache)";
            return;
        }

        _rankingBodyTmp.text = DescribeRankingFailure(result);
        if (_rankingTitleTmp != null)
            _rankingTitleTmp.text = "Melhores pontuações";
    }

    static string DescribeRankingFailure(WallRushLeaderboardFetchResult result)
    {
        if (result == null)
            return "Não foi possível carregar o ranking agora.";

        switch (result.failureReason)
        {
            case LeaderboardFetchFailureReason.NotConfigured:
                return "Ranking online não configurado.\n\nConfirma a URL, a chave pública e o asset do Supabase no Unity.";
            case LeaderboardFetchFailureReason.NetworkUnavailable:
                return "Não foi possível ligar ao ranking.\n\nConfirma a internet do aparelho e tenta novamente.";
            case LeaderboardFetchFailureReason.Timeout:
                return "O ranking demorou demasiado tempo a responder.\n\nTenta novamente dentro de alguns segundos.";
            case LeaderboardFetchFailureReason.Unauthorized:
                return "A chave pública do Supabase foi recusada.\n\nRevê a publishable key / anon key no asset de configuração.";
            case LeaderboardFetchFailureReason.Forbidden:
                return "O Supabase bloqueou a leitura do ranking.\n\nConfirma as policies RLS da tabela `wall_rush_scores`.";
            case LeaderboardFetchFailureReason.NotFound:
                return "A tabela do ranking não foi encontrada.\n\nConfirma o nome da tabela e da coluna configurados no Supabase.";
            case LeaderboardFetchFailureReason.RateLimited:
                return "O Supabase limitou temporariamente os pedidos.\n\nEspera um pouco e tenta novamente.";
            case LeaderboardFetchFailureReason.ServicePaused:
                return "O projeto do ranking parece estar pausado.\n\nSe estiveres no plano grátis do Supabase, abre o dashboard e reativa o projeto.";
            case LeaderboardFetchFailureReason.ServerUnavailable:
                return "O servidor do ranking está indisponível.\n\nO projeto Supabase pode estar em pausa ou com instabilidade.";
            case LeaderboardFetchFailureReason.InvalidResponse:
                return "O ranking respondeu com dados inválidos.\n\nConfirma a estrutura da tabela e as colunas esperadas pelo jogo.";
            default:
                return "Não foi possível carregar o ranking agora.\n\nTenta novamente em breve.";
        }
    }

    static string FormatCacheAge(long fetchedAtUnixSeconds)
    {
        if (fetchedAtUnixSeconds <= 0)
            return "";

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var delta = Mathf.Max(0f, now - fetchedAtUnixSeconds);
        if (delta < 60f)
            return "há menos de 1 minuto";
        if (delta < 3600f)
            return "há cerca de " + Mathf.RoundToInt(delta / 60f) + " min";
        if (delta < 86400f)
            return "há cerca de " + Mathf.RoundToInt(delta / 3600f) + " h";
        return "há cerca de " + Mathf.RoundToInt(delta / 86400f) + " dias";
    }

    IEnumerator RankingFetchAfterOpen()
    {
        yield return null;
        WallRushLeaderboardFetchResult result = null;
        yield return StartCoroutine(WallsOnlineLeaderboard.FetchTopCoroutine(r => result = r));
        ApplyRankingToUi(result);
    }

    void OpenRanking()
    {
        if (_rankingAnimating || _rankingOverlay == null)
            return;
        if (_rankingBodyTmp != null)
            _rankingBodyTmp.text = "A carregar ranking…";
        _rankingOverlay.SetActive(true);
        StartCoroutine(OpenRankingRoutine());
        StartCoroutine(RankingFetchAfterOpen());
    }

    IEnumerator OpenRankingRoutine()
    {
        _rankingAnimating = true;
        if (_mainGroup != null)
        {
            _mainGroup.alpha = 0f;
            _mainGroup.interactable = false;
            _mainGroup.blocksRaycasts = false;
        }

        _rankingOverlayGroup.alpha = 0f;
        _rankingCardGroup.alpha = 0f;
        _rankingCardRt.localScale = Vector3.one * 0.82f;
        _rankingOverlayGroup.interactable = true;
        _rankingOverlayGroup.blocksRaycasts = true;

        float t = 0f;
        const float dur = 0.32f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float e = WallsTween.EaseOutBack(Mathf.Clamp01(t));
            _rankingOverlayGroup.alpha = Mathf.Lerp(0f, 1f, WallsTween.EaseInOutQuad(Mathf.Clamp01(t)));
            _rankingCardGroup.alpha = _rankingOverlayGroup.alpha;
            _rankingCardRt.localScale = Vector3.LerpUnclamped(Vector3.one * 0.82f, Vector3.one, e);
            yield return null;
        }

        _rankingOverlayGroup.alpha = 1f;
        _rankingCardGroup.alpha = 1f;
        _rankingCardRt.localScale = Vector3.one;
        _rankingAnimating = false;
    }

    void CloseRanking()
    {
        if (_rankingAnimating || _rankingOverlay == null || !_rankingOverlay.activeSelf)
            return;
        StartCoroutine(CloseRankingRoutine());
    }

    IEnumerator CloseRankingRoutine()
    {
        _rankingAnimating = true;
        float t = 0f;
        const float dur = 0.22f;
        float a0 = _rankingOverlayGroup.alpha;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float u = WallsTween.EaseInOutQuad(Mathf.Clamp01(t));
            _rankingOverlayGroup.alpha = Mathf.Lerp(a0, 0f, u);
            _rankingCardGroup.alpha = _rankingOverlayGroup.alpha;
            _rankingCardRt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, u);
            yield return null;
        }

        _rankingOverlay.SetActive(false);
        _rankingOverlayGroup.interactable = false;
        _rankingOverlayGroup.blocksRaycasts = false;
        if (_mainGroup != null)
        {
            _mainGroup.alpha = 1f;
            _mainGroup.interactable = true;
            _mainGroup.blocksRaycasts = true;
        }

        _rankingAnimating = false;
    }

    void BuildShakeToggle(Transform parent, Vector2 pos)
    {
        var row = new GameObject("ShakeRow");
        row.transform.SetParent(parent, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(620, 72);
        rowRt.anchoredPosition = pos;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var tGo = new GameObject("Label");
        tGo.transform.SetParent(row.transform, false);
        tGo.AddComponent<RectTransform>();
        var leLabel = tGo.AddComponent<LayoutElement>();
        leLabel.flexibleWidth = 1f;
        leLabel.minWidth = 80f;
        leLabel.preferredHeight = 56f;
        _shakeLabel = tGo.AddComponent<TextMeshProUGUI>();
        _shakeLabel.text = "Tremer ecrã";
        _shakeLabel.fontSize = 38;
        _shakeLabel.enableAutoSizing = false;
        _shakeLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _shakeLabel.overflowMode = TextOverflowModes.Ellipsis;
        if (_font != null)
            _shakeLabel.font = _font;

        var toggleRoot = new GameObject("Toggle");
        toggleRoot.transform.SetParent(row.transform, false);
        toggleRoot.AddComponent<RectTransform>();
        var leToggle = toggleRoot.AddComponent<LayoutElement>();
        leToggle.preferredWidth = 56f;
        leToggle.preferredHeight = 56f;
        leToggle.flexibleWidth = 0f;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(toggleRoot.transform, false);
        Stretch(bgGo.AddComponent<RectTransform>());
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = WhiteSprite();
        bgImg.color = new Color(1, 1, 1, 0.2f);

        var checkGo = new GameObject("Checkmark");
        checkGo.transform.SetParent(toggleRoot.transform, false);
        var cRt = checkGo.AddComponent<RectTransform>();
        Stretch(cRt);
        cRt.offsetMin = new Vector2(8, 8);
        cRt.offsetMax = new Vector2(-8, -8);
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.sprite = WhiteSprite();
        checkImg.color = ThemeManager.Palette.SliderFill;
        _shakeCheckGraphic = checkImg;

        _shakeToggle = toggleRoot.AddComponent<Toggle>();
        _shakeToggle.targetGraphic = bgImg;
        _shakeToggle.graphic = checkImg;
        _shakeToggle.isOn = WallsGamePrefs.ScreenShakeEnabled;
        _shakeToggle.onValueChanged.AddListener(on => WallsGamePrefs.ScreenShakeEnabled = on);
    }

    RectTransform CreateMenuButton(Transform parent, string label, Vector2 pos, System.Action onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(520, 104);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.type = Image.Type.Simple;

        var themed = go.AddComponent<WallsThemedButton>();
        var lGo = new GameObject("Label");
        lGo.transform.SetParent(go.transform, false);
        Stretch(lGo.AddComponent<RectTransform>());
        var tmp = lGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        if (_font != null)
            tmp.font = _font;
        themed.BindLabel(tmp);
        themed.Clicked += () => onClick?.Invoke();

        AddShadow(go, new Vector2(0, -5), 0.35f);
        return rt;
    }

    RectTransform CreateTmp(Transform parent, string text, float size, Vector2 sizeDelta, float y)
    {
        var go = new GameObject("Text_" + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        if (_font != null)
            tmp.font = _font;
        return rt;
    }

    Slider CreateThemedSlider(Transform parent, string label, Vector2 pos, float initial, UnityEngine.Events.UnityAction<float> onChanged, out Image trackImg, out Image fillImg)
    {
        CreateTmp(parent, label, 36, new Vector2(560, 56), pos.y + 72f);

        var go = new GameObject("Slider_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(560, 40);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;

        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        Stretch(bg.AddComponent<RectTransform>());
        trackImg = bg.AddComponent<Image>();
        trackImg.sprite = WhiteSprite();
        trackImg.color = ThemeManager.Palette.SliderTrack;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        Stretch(faRt);
        faRt.offsetMin = new Vector2(6, 5);
        faRt.offsetMax = new Vector2(-6, -5);

        var fillBar = new GameObject("Fill");
        fillBar.transform.SetParent(fillArea.transform, false);
        var fRt = fillBar.AddComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.sizeDelta = Vector2.zero;
        fillImg = fillBar.AddComponent<Image>();
        fillImg.sprite = WhiteSprite();
        fillImg.color = ThemeManager.Palette.SliderFill;

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fRt;
        slider.targetGraphic = fillImg;
        slider.value = initial;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    void CreateThemedNicknameField(Transform parent, Vector2 pos)
    {
        CreateTmp(parent, "Apelido", 36, new Vector2(560, 56), pos.y + 48f);

        var root = new GameObject("Input_Apelido");
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(520, 64);
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = pos;

        _nicknameFieldBg = root.AddComponent<Image>();
        _nicknameFieldBg.sprite = WhiteSprite();
        _nicknameFieldBg.color = ThemeManager.Palette.SliderTrack;

        _nicknameField = root.AddComponent<TMP_InputField>();
        _nicknameField.lineType = TMP_InputField.LineType.SingleLine;
        _nicknameField.characterLimit = WallsOnlineLeaderboard.DisplayNameMaxLength;
        _nicknameField.caretWidth = 2;

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(root.transform, false);
        var taRt = textArea.AddComponent<RectTransform>();
        Stretch(taRt);
        taRt.offsetMin = new Vector2(14, 8);
        taRt.offsetMax = new Vector2(-14, -8);
        textArea.AddComponent<RectMask2D>();

        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(textArea.transform, false);
        var phRt = phGo.AddComponent<RectTransform>();
        Stretch(phRt);
        _nicknamePlaceholder = phGo.AddComponent<TextMeshProUGUI>();
        _nicknamePlaceholder.text = "Nome no ranking online";
        _nicknamePlaceholder.fontSize = 30;
        _nicknamePlaceholder.alignment = TextAlignmentOptions.Left;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(textArea.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        Stretch(txtRt);
        _nicknameInputText = txtGo.AddComponent<TextMeshProUGUI>();
        _nicknameInputText.fontSize = 30;
        _nicknameInputText.alignment = TextAlignmentOptions.Left;

        _nicknameField.textViewport = taRt;
        _nicknameField.textComponent = _nicknameInputText;
        _nicknameField.placeholder = _nicknamePlaceholder;

        if (_font != null)
        {
            _nicknamePlaceholder.font = _font;
            _nicknameInputText.font = _font;
            _nicknameField.fontAsset = _font;
        }

        _nicknameField.onEndEdit.AddListener(_ => SaveNicknameFromField());

        ApplyNicknameFieldTheme(ThemeManager.Palette);
    }

    void SaveNicknameFromField()
    {
        if (_nicknameField == null)
            return;
        WallsOnlineLeaderboard.SaveDisplayNameFromSettings(_nicknameField.text);
    }

    void ApplyNicknameFieldTheme(ThemePalette p)
    {
        if (_nicknameFieldBg != null)
            _nicknameFieldBg.color = p.SliderTrack;
        if (_nicknameInputText != null)
            _nicknameInputText.color = p.TextPrimary;
        if (_nicknameField != null)
        {
            _nicknameField.customCaretColor = true;
            _nicknameField.caretColor = p.TextPrimary;
            _nicknameField.selectionColor = new Color(p.SliderFill.r, p.SliderFill.g, p.SliderFill.b, 0.35f);
        }

        if (_nicknamePlaceholder != null)
            _nicknamePlaceholder.color = new Color(p.TextSecondary.r, p.TextSecondary.g, p.TextSecondary.b, 0.45f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void AddShadow(GameObject go, Vector2 distance, float alpha)
    {
        var sh = go.AddComponent<Shadow>();
        sh.effectDistance = distance;
        sh.effectColor = new Color(0, 0, 0, alpha);
    }

    void OnGlobalTheme(ThemePalette p)
    {
        if (_menuBackdrop != null)
            _menuBackdrop.color = p.Base;
        if (_titleTmp != null)
        {
            _titleTmp.color = p.TextPrimary;
            _titleTmp.outlineColor = new Color(p.TextPrimary.r, p.TextPrimary.g, p.TextPrimary.b, 0.2f);
        }
        if (_dimImage != null)
            _dimImage.color = p.OverlayDim;
        if (_settingsCardBg != null)
            _settingsCardBg.color = p.ModalCard;
        if (_musicTrack != null)
            _musicTrack.color = p.SliderTrack;
        if (_sfxTrack != null)
            _sfxTrack.color = p.SliderTrack;
        if (_musicFill != null)
            _musicFill.color = p.SliderFill;
        if (_sfxFill != null)
            _sfxFill.color = p.SliderFill;
        if (_shakeLabel != null)
            _shakeLabel.color = p.TextSecondary;
        if (_shakeCheckGraphic != null)
            _shakeCheckGraphic.color = p.SliderFill;
        ApplyNicknameFieldTheme(p);
        if (_rankingCardBg != null)
            _rankingCardBg.color = p.ModalCard;
        if (_rankingBodyTmp != null)
            _rankingBodyTmp.color = p.TextPrimary;
        if (_rankingTitleTmp != null)
            _rankingTitleTmp.color = p.TextPrimary;

        if (_rankingDimImage != null)
            _rankingDimImage.color = p.OverlayDim;
    }

    void PlayUiClick()
    {
        if (_uiClickClip == null || Camera.main == null)
            return;
        float vol = 0.55f;
        if (WallsAudioDirector.Instance != null)
            vol *= Mathf.Max(0.05f, WallsAudioDirector.Instance.GetSfxLinear());
        AudioSource.PlayClipAtPoint(_uiClickClip, Camera.main.transform.position, vol);
    }

    void OpenSettings()
    {
        if (_settingsAnimating || _settingsOverlay == null)
            return;
        _musicSlider?.SetValueWithoutNotify(WallsAudioPrefs.MusicLinear);
        _sfxSlider?.SetValueWithoutNotify(WallsAudioPrefs.SfxLinear);
        if (_shakeToggle != null)
            _shakeToggle.SetIsOnWithoutNotify(WallsGamePrefs.ScreenShakeEnabled);
        if (_nicknameField != null)
            _nicknameField.SetTextWithoutNotify(PlayerPrefs.GetString(WallsOnlineLeaderboard.DisplayNamePrefsKey, ""));
        _settingsOverlay.SetActive(true);
        StartCoroutine(OpenSettingsRoutine());
    }

    IEnumerator OpenSettingsRoutine()
    {
        _settingsAnimating = true;
        if (_mainGroup != null)
        {
            _mainGroup.alpha = 0f;
            _mainGroup.interactable = false;
            _mainGroup.blocksRaycasts = false;
        }
        _settingsOverlayGroup.alpha = 0f;
        _settingsCardGroup.alpha = 0f;
        _settingsCardRt.localScale = Vector3.one * 0.82f;
        _settingsOverlayGroup.interactable = true;
        _settingsOverlayGroup.blocksRaycasts = true;

        float t = 0f;
        const float dur = 0.32f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float e = WallsTween.EaseOutBack(Mathf.Clamp01(t));
            _settingsOverlayGroup.alpha = Mathf.Lerp(0f, 1f, WallsTween.EaseInOutQuad(Mathf.Clamp01(t)));
            _settingsCardGroup.alpha = _settingsOverlayGroup.alpha;
            _settingsCardRt.localScale = Vector3.LerpUnclamped(Vector3.one * 0.82f, Vector3.one, e);
            yield return null;
        }
        _settingsOverlayGroup.alpha = 1f;
        _settingsCardGroup.alpha = 1f;
        _settingsCardRt.localScale = Vector3.one;
        _settingsAnimating = false;
    }

    void CloseSettings()
    {
        if (_settingsAnimating || _settingsOverlay == null || !_settingsOverlay.activeSelf)
            return;
        SaveNicknameFromField();
        StartCoroutine(CloseSettingsRoutine());
    }

    IEnumerator CloseSettingsRoutine()
    {
        _settingsAnimating = true;
        float t = 0f;
        const float dur = 0.22f;
        float a0 = _settingsOverlayGroup.alpha;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float u = WallsTween.EaseInOutQuad(Mathf.Clamp01(t));
            _settingsOverlayGroup.alpha = Mathf.Lerp(a0, 0f, u);
            _settingsCardGroup.alpha = _settingsOverlayGroup.alpha;
            _settingsCardRt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, u);
            yield return null;
        }
        _settingsOverlay.SetActive(false);
        _settingsOverlayGroup.interactable = false;
        _settingsOverlayGroup.blocksRaycasts = false;
        if (_mainGroup != null)
        {
            _mainGroup.alpha = 1f;
            _mainGroup.interactable = true;
            _mainGroup.blocksRaycasts = true;
        }
        _settingsAnimating = false;
    }

    void Play()
    {
        WallsAudioDirector.Instance?.ClearMenuMusicAttenuation();
        PlayerPrefs.SetInt(WallsAudioPrefs.FromMenuKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Main");
    }

    void Quit()
    {
        Application.Quit();
    }
}
