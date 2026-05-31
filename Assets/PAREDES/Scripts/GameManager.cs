using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [HideInInspector]
    public int score = 0;


    public TextMeshProUGUI CurrentScoreText;
    public TextMeshProUGUI BestScoreText;
    public TextMeshProUGUI BestText;
    public GameObject TouchToStartText;

    public GameObject GameOverPanel;
    public GameObject GameOverEffectPanel;

    [HideInInspector]
    public bool isStarted = false;

    GameObject _continueReviveButtonRoot;
    Button _continueReviveButton;

    /// <summary>True após o jogador tocar em Continuar nesta morte — impede vários vídeos na mesma tela.</summary>
    bool _continueOfferSpentThisDeath;
    /// <summary>True após um revive com anúncio recompensado nesta partida — até reiniciar a cena não volta a oferecer Continuar.</summary>
    bool _rewardedContinueConsumedThisRun;
    /// <summary>True após aplicar o revive com sucesso — evita duplo callback do SDK.</summary>
    bool _reviveFromAdAppliedThisDeath;
    bool _continueRewardAdCoroutineRunning;

    static int PlayCount;



    void Awake()
    {
        Application.targetFrameRate = 60;

        Time.timeScale = 1.0f;
        BestScoreText.text = PlayerPrefs.GetInt("BestScore", 0).ToString();

        if (PlayerPrefs.GetInt(WallsAudioPrefs.FromMenuKey, 0) == 1)
        {
            isStarted = true;
            if (TouchToStartText != null)
                TouchToStartText.SetActive(false);
            PlayerPrefs.DeleteKey(WallsAudioPrefs.FromMenuKey);
            PlayerPrefs.Save();
        }

        SetupScoreHudBehindPlayer();

        var hud = GetComponent<WallsGameplayHud>();
        if (hud == null)
            hud = gameObject.AddComponent<WallsGameplayHud>();
        hud.Setup();

        if (GetComponent<WallsPauseMenu>() == null)
            gameObject.AddComponent<WallsPauseMenu>();

        EnsureMenuReturnButton();
        EnsureContinueReviveButton();
        CleanupContinueReviveIconChildren();
        EnsureRestartLabelRecomeçar();
        FixGameOverPanelButtonLayout();
        if (GameOverPanel != null)
            ApplyGameOverMenuButtonTheme(ThemeManager.Palette);
    }

    /// <summary>Mesmo aspeto dos botões do menu principal (WallsUIManager): LiberationSans, regular, sem negrito.</summary>
    static void ApplyMenuLikeGameOverButtonLabel(TMP_Text tmp)
    {
        if (tmp == null)
            return;
        var font = WallsUiFont.Load();
        if (font != null)
            tmp.font = font;
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
    }

    /// <summary>
    /// Placar no mundo (Screen Space - Camera, sorting baixo) para a bola desenhar por cima.
    /// </summary>
    void SetupScoreHudBehindPlayer()
    {
        if (CurrentScoreText == null)
            return;
        var rootCanvas = CurrentScoreText.GetComponentInParent<Canvas>();
        if (rootCanvas == null || rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return;
        if (CurrentScoreText.transform.parent != null &&
            CurrentScoreText.transform.parent.name == "ScoreHudLayer")
            return;
        var cam = Camera.main;
        if (cam == null)
            return;

        var layerGo = new GameObject("ScoreHudLayer");
        layerGo.transform.SetParent(null);
        layerGo.transform.SetSiblingIndex(rootCanvas.transform.GetSiblingIndex());

        var layerCanvas = layerGo.AddComponent<Canvas>();
        layerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        layerCanvas.worldCamera = cam;
        layerCanvas.planeDistance = 8f;
        layerCanvas.sortingOrder = 50;

        var scaler = layerGo.AddComponent<CanvasScaler>();
        var srcScaler = rootCanvas.GetComponent<CanvasScaler>();
        if (srcScaler != null)
        {
            scaler.uiScaleMode = srcScaler.uiScaleMode;
            scaler.referencePixelsPerUnit = srcScaler.referencePixelsPerUnit;
            scaler.scaleFactor = srcScaler.scaleFactor;
            scaler.referenceResolution = srcScaler.referenceResolution;
            scaler.screenMatchMode = srcScaler.screenMatchMode;
            scaler.matchWidthOrHeight = srcScaler.matchWidthOrHeight;
        }
        layerGo.AddComponent<GraphicRaycaster>();

        CurrentScoreText.transform.SetParent(layerGo.transform, false);
        if (BestScoreText != null)
            BestScoreText.transform.SetParent(layerGo.transform, false);
        if (BestText != null)
            BestText.transform.SetParent(layerGo.transform, false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && isStarted == false)
        {
            isStarted = true;
            TouchToStartText.SetActive(false);
        }
    }


    public void addScore()
    {
        score++;
        CurrentScoreText.text = score.ToString();

        if (score > PlayerPrefs.GetInt("BestScore", 0))
        {
            PlayerPrefs.SetInt("BestScore", score);
            BestScoreText.text = PlayerPrefs.GetInt("BestScore", 0).ToString();
        }
    }


    public void Gameover()
    {
        _continueOfferSpentThisDeath = false;
        _reviveFromAdAppliedThisDeath = false;
        ResetContinueReviveForNewDeath();
        GetComponent<TriangleManager>()?.PrepareReviveTriangleState();
        AdsManager.Instance?.RegisterGameOver();
        WallsAudioDirector.Instance?.ApplyMusicDuck();
        StartCoroutine(GameoverCoroutine());
    }


    IEnumerator GameoverCoroutine()
    {
        var p = ThemeManager.Palette;
        if (CurrentScoreText != null)
            CurrentScoreText.color = p.TextPrimary;
        if (BestScoreText != null)
            BestScoreText.color = p.TextPrimary;
        if (BestText != null)
            BestText.color = p.TextSecondary;

        GameOverEffectPanel.SetActive(true);
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(0.5f);
        if (AdsManager.Instance != null)
            yield return AdsManager.Instance.RunDeathInterstitialIfAny();
        GameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        CleanupContinueReviveIconChildren();
        if (_continueReviveButtonRoot != null)
        {
            _continueReviveButtonRoot.SetActive(true);
            if (_rewardedContinueConsumedThisRun)
                ApplyContinueReviveRunConsumedLock();
            else if (_continueReviveButton != null)
                _continueReviveButton.interactable = !_continueOfferSpentThisDeath;
        }
        FixGameOverPanelButtonLayout();
        ApplyGameOverMenuButtonTheme(ThemeManager.Palette);
        yield break;
    }

    void EnsureMenuReturnButton()
    {
        if (GameOverPanel == null)
            return;
        const string btnName = "MenuButton";
        if (GameOverPanel.transform.Find(btnName) != null)
            return;

        TMP_FontAsset font = WallsUiFont.Load();
        if (font == null)
        {
            var restartTf = GameOverPanel.transform.Find("RestartButton");
            if (restartTf != null)
            {
                var existing = restartTf.GetComponentInChildren<TMP_Text>(true);
                if (existing != null)
                    font = existing.font;
            }
        }

        var go = new GameObject(btnName);
        go.transform.SetParent(GameOverPanel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(680f, 120f);
        rt.anchoredPosition = new Vector2(0f, -520f);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(GoToMenu);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Voltar ao menu";
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ThemeManager.Palette.TextPrimary;
        if (font != null)
            tmp.font = font;
        ApplyMenuLikeGameOverButtonLabel(tmp);
    }

    void ApplyGameOverMenuButtonTheme(ThemePalette p)
    {
        if (GameOverPanel == null)
            return;
        ApplyGameOverCardButtonTheme(GameOverPanel.transform.Find("MenuButton"), p);
        ApplyGameOverCardButtonTheme(GameOverPanel.transform.Find("ContinueReviveButton"), p);
        ApplyGameOverCardButtonTheme(GameOverPanel.transform.Find("RestartButton"), p);
    }

    static void ApplyGameOverCardButtonTheme(Transform btnTf, ThemePalette p)
    {
        if (btnTf == null)
            return;
        var img = btnTf.GetComponent<Image>();
        if (img != null)
        {
            var c = p.ModalCard;
            c.a = Mathf.Max(c.a, 0.55f);
            img.color = c;
        }
        var label = btnTf.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = p.TextPrimary;
            ApplyMenuLikeGameOverButtonLabel(label);
        }
    }

    void EnsureRestartLabelRecomeçar()
    {
        if (GameOverPanel == null)
            return;
        var restartTf = GameOverPanel.transform.Find("RestartButton");
        if (restartTf == null)
            return;
        var tmp = restartTf.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = "Recomeçar";
            tmp.fontSize = 48f;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = 48f;
            tmp.enableAutoSizing = false;
            ApplyMenuLikeGameOverButtonLabel(tmp);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
    }

    /// <summary>
    /// O RestartButton na cena vinha com altura enorme e cobria o "Continuar". Alinha os três botões.
    /// </summary>
    void FixGameOverPanelButtonLayout()
    {
        if (GameOverPanel == null)
            return;
        const float btnH = 120f;
        const float btnW = 680f;
        const float gap = 40f;
        const float stackBottomY = -700f;

        var restartTf = GameOverPanel.transform.Find("RestartButton");
        if (restartTf != null)
        {
            var rt = restartTf.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(btnW, btnH);
                rt.anchoredPosition = new Vector2(0f, stackBottomY);
            }
        }

        var continueTf = GameOverPanel.transform.Find("ContinueReviveButton");
        if (continueTf != null)
        {
            var rt = continueTf.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(btnW, btnH);
                var step = btnH * 0.5f + gap + btnH * 0.5f;
                rt.anchoredPosition = new Vector2(0f, stackBottomY + step);
            }
        }

        var menuTf = GameOverPanel.transform.Find("MenuButton");
        if (menuTf != null)
        {
            var rt = menuTf.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(btnW, btnH);
                var step = btnH * 0.5f + gap + btnH * 0.5f;
                rt.anchoredPosition = new Vector2(0f, stackBottomY + step * 2f);
            }
        }
    }

    void EnsureContinueReviveButton()
    {
        if (GameOverPanel == null)
            return;
        const string btnName = "ContinueReviveButton";
        if (GameOverPanel.transform.Find(btnName) != null)
        {
            _continueReviveButtonRoot = GameOverPanel.transform.Find(btnName).gameObject;
            _continueReviveButton = _continueReviveButtonRoot.GetComponent<Button>();
            EnsureContinueReviveCanvasGroup(_continueReviveButtonRoot);
            ApplyContinueButtonTextStyle(_continueReviveButtonRoot.transform);
            return;
        }

        TMP_FontAsset font = WallsUiFont.Load();
        if (font == null)
        {
            var restartTf = GameOverPanel.transform.Find("RestartButton");
            if (restartTf != null)
            {
                var existing = restartTf.GetComponentInChildren<TMP_Text>(true);
                if (existing != null)
                    font = existing.font;
            }
        }

        var go = new GameObject(btnName);
        go.transform.SetParent(GameOverPanel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(680f, 120f);
        rt.anchoredPosition = new Vector2(0f, -560f);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        _continueReviveButton = go.AddComponent<Button>();
        _continueReviveButton.targetGraphic = img;
        _continueReviveButton.onClick.AddListener(OnContinueWithVideoClicked);
        EnsureContinueReviveCanvasGroup(go);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Continuar (Anúncio)";
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ThemeManager.Palette.TextPrimary;
        if (font != null)
            tmp.font = font;
        ApplyMenuLikeGameOverButtonLabel(tmp);

        _continueReviveButtonRoot = go;
        go.SetActive(false);
    }

    public void OnContinueWithVideoClicked()
    {
        if (_rewardedContinueConsumedThisRun || _continueOfferSpentThisDeath || _continueRewardAdCoroutineRunning)
            return;
        _continueOfferSpentThisDeath = true;
        _continueRewardAdCoroutineRunning = true;
        LockContinueReviveAfterFirstTap();
        StartCoroutine(ContinueWithRewardedRoutine());
    }

    IEnumerator ContinueWithRewardedRoutine()
    {
        var done = false;
        var earned = false;
        try
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.TryShowReviveRewarded(ok => { earned = ok; done = true; });
            else
                done = true;
            while (!done)
                yield return null;
            if (earned)
                ReviveAfterRewardedAd();
        }
        finally
        {
            _continueRewardAdCoroutineRunning = false;
        }
    }

    void ReviveAfterRewardedAd()
    {
        if (_reviveFromAdAppliedThisDeath)
            return;
        _reviveFromAdAppliedThisDeath = true;
        _rewardedContinueConsumedThisRun = true;
        WallsAudioDirector.Instance?.ClearMusicDuck();
        Time.timeScale = 1f;
        AdsManager.RestoreGameplayPortraitOrientation();
        GameOverPanel.SetActive(false);
        GameOverEffectPanel.SetActive(false);
        isStarted = false;
        if (TouchToStartText != null)
            TouchToStartText.SetActive(true);

        var playerGo = GameObject.Find("Player");
        if (playerGo != null)
            playerGo.GetComponent<Player>()?.PrepareReviveFromReward();
        GetComponent<TriangleManager>()?.ApplyReviveTriangleSpawnBudget();

        if (_continueReviveButtonRoot != null)
            _continueReviveButtonRoot.SetActive(false);
    }

    public void GoToMenu()
    {
        void LoadMenu()
        {
            WallsOnlineLeaderboard.TrySubmitScoreIfPersonalBest(score);
            WallsAudioDirector.Instance?.ClearMusicDuck();
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        if (AdsManager.Instance != null)
            AdsManager.Instance.TryShowInterstitialThen(LoadMenu);
        else
            LoadMenu();
    }

    public void Restart()
    {
        void Reload()
        {
            WallsOnlineLeaderboard.TrySubmitScoreIfPersonalBest(score);
            WallsAudioDirector.Instance?.ClearMusicDuck();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (AdsManager.Instance != null)
            AdsManager.Instance.TryShowInterstitialThen(Reload);
        else
            Reload();
    }

    void EnsureContinueReviveCanvasGroup(GameObject rootGo)
    {
        if (rootGo == null)
            return;
        var cg = rootGo.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = rootGo.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    void LockContinueReviveAfterFirstTap()
    {
        ApplyContinueReviveInputLock(greyOut: false);
    }

    /// <summary>Oferta de vídeo já usada nesta partida — botão visível mas inativo até reiniciar a cena.</summary>
    void ApplyContinueReviveRunConsumedLock()
    {
        ApplyContinueReviveInputLock(greyOut: true);
    }

    void ApplyContinueReviveInputLock(bool greyOut)
    {
        if (_continueReviveButtonRoot == null)
            return;
        var cg = _continueReviveButtonRoot.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = _continueReviveButtonRoot.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.alpha = greyOut ? 0.42f : 1f;
        if (_continueReviveButton != null)
        {
            _continueReviveButton.interactable = false;
            _continueReviveButton.enabled = false;
        }
    }

    void ResetContinueReviveForNewDeath()
    {
        if (_continueReviveButtonRoot == null)
            return;
        if (_rewardedContinueConsumedThisRun)
        {
            ApplyContinueReviveRunConsumedLock();
            return;
        }
        var cg = _continueReviveButtonRoot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
        }
        if (_continueReviveButton != null)
        {
            _continueReviveButton.enabled = true;
            _continueReviveButton.interactable = true;
        }
    }

    void CleanupContinueReviveIconChildren()
    {
        if (GameOverPanel == null)
            return;
        var root = GameOverPanel.transform.Find("ContinueReviveButton");
        if (root == null)
            return;
        foreach (var name in new[] { "PlayIcon", "VideoGlyph", "PlayIconImg", "VideoIconImg" })
        {
            var ch = root.Find(name);
            if (ch != null)
                Destroy(ch.gameObject);
        }
        ApplyContinueButtonTextStyle(root);
    }

    static void ApplyContinueButtonTextStyle(Transform continueRoot)
    {
        var textTf = continueRoot.Find("Text");
        if (textTf == null)
            return;
        var trt = textTf.GetComponent<RectTransform>();
        if (trt != null)
        {
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }
        var tmp = textTf.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            return;
        tmp.text = "Continuar (Anúncio)";
        tmp.fontSize = 48f;
        tmp.enableAutoSizing = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.color = ThemeManager.Palette.TextPrimary;
        ApplyMenuLikeGameOverButtonLabel(tmp);
    }
}
