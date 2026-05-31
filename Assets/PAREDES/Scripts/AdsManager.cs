using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

/// <summary>
/// Intersticial de sessão: após N game overs e intervalo mínimo entre exibições.
/// Intersticial de morte: a cada N mortes e intervalo mínimo entre exibições.
/// Banner: só na cena Main. App ID em GoogleMobileAdsSettings.asset.
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    const string GoogleTestAndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
    const string GoogleTestIosInterstitial = "ca-app-pub-3940256099942544/4411468910";
    const string GoogleTestAndroidBanner = "ca-app-pub-3940256099942544/6300978111";
    const string GoogleTestIosBanner = "ca-app-pub-3940256099942544/2934735716";
    const string GoogleTestAndroidRewarded = "ca-app-pub-3940256099942544/5224354917";
    const string GoogleTestIosRewarded = "ca-app-pub-3940256099942544/1712485313";

    [Tooltip("Ligue só para desenvolvimento com anúncios de teste Google (ignora IDs de produção abaixo).")]
    [SerializeField] bool useGoogleTestAdIds = false;

    [Tooltip("Intersticial ao reiniciar / ir ao menu (ca-app-pub-.../...).")]
    [SerializeField] string androidInterstitialId = "ca-app-pub-8613089600560888/6471427279";

    [SerializeField] string iosInterstitialId = "";

    [Tooltip("Intersticial extra quando o jogador morre (antes do painel de game over).")]
    [SerializeField] string androidDeathInterstitialId = "ca-app-pub-8613089600560888/63217788423";

    [SerializeField] string iosDeathInterstitialId = "";

    [Tooltip("Banner na parte inferior durante o jogo. Vazio em produção usa bloco de teste só para o banner.")]
    [SerializeField] string androidBannerId = "";

    [SerializeField] string iosBannerId = "";

    [Tooltip("Recompensado para continuar após morte. A duração do vídeo é definida pelo anunciante/AdMob (anúncios de teste costumam ser curtos). Vazio usa o bloco de teste Google.")]
    [SerializeField] string androidReviveRewardedId = "";

    [SerializeField] string iosReviveRewardedId = "";

    [Tooltip("Intersticial de reiniciar/menu só após N game overs acumulados.")]
    [SerializeField] int showInterstitialEveryNGameOvers = 3;

    [Tooltip("Segundos mínimos entre dois intersticiais de sessão (reiniciar ou voltar ao menu).")]
    [SerializeField] float minSecondsBetweenSessionInterstitials = 45f;

    [Tooltip("Intersticial de morte só a cada N mortes (acumulado).")]
    [SerializeField] int deathInterstitialEveryNDeaths = 3;

    [Tooltip("Segundos mínimos entre dois intersticiais de morte.")]
    [SerializeField] float minSecondsBetweenDeathInterstitials = 75f;

    InterstitialAd _sessionInterstitial;
    InterstitialAd _deathInterstitial;
    RewardedAd _reviveRewarded;
    BannerView _bannerView;
    bool _initialized;
    int _gameOverCount;
    int _deathsAccumulatedForDeathAd;
    float _lastSessionInterstitialUnscaledTime = -1e9f;
    float _lastDeathInterstitialUnscaledTime = -1e9f;
    bool _sessionLoading;
    bool _deathLoading;
    bool _reviveRewardedLoading;
    bool _reviveRewardEarned;
    bool _reviveRewardCallbackDelivered;
    Action<bool> _onReviveRewardClosed;
    Action _afterSessionInterstitial;
    Action _afterDeathInterstitial;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (Instance != this)
            return;
        MobileAds.Initialize((InitializationStatus _) =>
        {
            _initialized = true;
            RequestSessionInterstitial();
            RequestDeathInterstitial();
            RequestReviveRewarded();
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        });
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        _sessionInterstitial?.Destroy();
        _sessionInterstitial = null;
        _deathInterstitial?.Destroy();
        _deathInterstitial = null;
        _reviveRewarded?.Destroy();
        _reviveRewarded = null;
        DestroyBanner();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_initialized)
            return;
        if (scene.name == "Main")
            CreateOrLoadBanner();
        else
            DestroyBanner();
    }

    string SessionInterstitialUnitId() => ResolveInterstitialUnitId(androidInterstitialId, iosInterstitialId);

    string DeathInterstitialUnitId() => ResolveInterstitialUnitId(androidDeathInterstitialId, iosDeathInterstitialId);

    string ResolveInterstitialUnitId(string androidId, string iosId)
    {
        if (useGoogleTestAdIds)
        {
#if UNITY_IOS
            return GoogleTestIosInterstitial;
#else
            return GoogleTestAndroidInterstitial;
#endif
        }

#if UNITY_IOS
        var id = iosId != null ? iosId.Trim() : "";
#else
        var id = androidId != null ? androidId.Trim() : "";
#endif
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[AdsManager] ID de intersticial vazio — a usar anúncio de teste Google.");
#if UNITY_IOS
            return GoogleTestIosInterstitial;
#else
            return GoogleTestAndroidInterstitial;
#endif
        }
        return id;
    }

    string BannerUnitId()
    {
        if (useGoogleTestAdIds)
        {
#if UNITY_IOS
            return GoogleTestIosBanner;
#else
            return GoogleTestAndroidBanner;
#endif
        }

#if UNITY_IOS
        var id = iosBannerId != null ? iosBannerId.Trim() : "";
#else
        var id = androidBannerId != null ? androidBannerId.Trim() : "";
#endif
        if (string.IsNullOrEmpty(id))
        {
#if UNITY_IOS
            return GoogleTestIosBanner;
#else
            return GoogleTestAndroidBanner;
#endif
        }
        return id;
    }

    string ReviveRewardedUnitId()
    {
        if (useGoogleTestAdIds)
        {
#if UNITY_IOS
            return GoogleTestIosRewarded;
#else
            return GoogleTestAndroidRewarded;
#endif
        }

#if UNITY_IOS
        var id = iosReviveRewardedId != null ? iosReviveRewardedId.Trim() : "";
#else
        var id = androidReviveRewardedId != null ? androidReviveRewardedId.Trim() : "";
#endif
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[AdsManager] ID de recompensado (continuar) vazio — a usar anúncio de teste Google.");
#if UNITY_IOS
            return GoogleTestIosRewarded;
#else
            return GoogleTestAndroidRewarded;
#endif
        }
        return id;
    }

    void RequestReviveRewarded()
    {
        if (!_initialized || _reviveRewardedLoading)
            return;
        if (_reviveRewarded != null)
            return;
        _reviveRewardedLoading = true;
        RewardedAd.Load(ReviveRewardedUnitId(), new AdRequest(), (ad, err) =>
        {
            _reviveRewardedLoading = false;
            if (err != null || ad == null)
                return;
            _reviveRewarded = ad;
            WireReviveRewardedCallbacks(ad);
        });
    }

    void WireReviveRewardedCallbacks(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () => { StartCoroutine(ReviveRewardedCloseCleanup(ad)); };
        ad.OnAdFullScreenContentFailed += _ =>
        {
            RestoreGameplayPortraitOrientation();
            DeliverReviveRewardResult();
            ad.Destroy();
            if (_reviveRewarded == ad)
                _reviveRewarded = null;
            RequestReviveRewarded();
        };
    }

    IEnumerator ReviveRewardedCloseCleanup(RewardedAd ad)
    {
        yield return null;
        RestoreGameplayPortraitOrientation();
        DeliverReviveRewardResult();
        if (_reviveRewarded == ad)
        {
            ad.Destroy();
            _reviveRewarded = null;
        }
        RequestReviveRewarded();
    }

    void DeliverReviveRewardResult()
    {
        if (_reviveRewardCallbackDelivered)
            return;
        _reviveRewardCallbackDelivered = true;
        var cb = _onReviveRewardClosed;
        _onReviveRewardClosed = null;
        cb?.Invoke(_reviveRewardEarned);
    }

    /// <summary>
    /// Mostra o vídeo recompensado de continuar. O callback recebe true se o jogador viu até ao fim e ganhou a recompensa.
    /// </summary>
    public void TryShowReviveRewarded(Action<bool> onComplete)
    {
        if (!_initialized || _reviveRewarded == null || !_reviveRewarded.CanShowAd())
        {
            onComplete?.Invoke(false);
            RequestReviveRewarded();
            return;
        }

        _onReviveRewardClosed = onComplete;
        _reviveRewardCallbackDelivered = false;
        _reviveRewardEarned = false;
        _reviveRewarded.Show(_ => { _reviveRewardEarned = true; });
    }

    void RequestSessionInterstitial()
    {
        if (!_initialized || _sessionLoading)
            return;
        if (_sessionInterstitial != null)
            return;
        _sessionLoading = true;
        InterstitialAd.Load(SessionInterstitialUnitId(), new AdRequest(), (ad, err) =>
        {
            _sessionLoading = false;
            if (err != null || ad == null)
                return;
            _sessionInterstitial = ad;
            WireSessionInterstitialCallbacks(ad);
        });
    }

    void WireSessionInterstitialCallbacks(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            RestoreGameplayPortraitOrientation();
            _afterSessionInterstitial?.Invoke();
            _afterSessionInterstitial = null;
            ad.Destroy();
            if (_sessionInterstitial == ad)
                _sessionInterstitial = null;
            RequestSessionInterstitial();
        };
        ad.OnAdFullScreenContentFailed += _ =>
        {
            RestoreGameplayPortraitOrientation();
            _afterSessionInterstitial?.Invoke();
            _afterSessionInterstitial = null;
            ad.Destroy();
            if (_sessionInterstitial == ad)
                _sessionInterstitial = null;
            RequestSessionInterstitial();
        };
    }

    void RequestDeathInterstitial()
    {
        if (!_initialized || _deathLoading)
            return;
        if (_deathInterstitial != null)
            return;
        _deathLoading = true;
        InterstitialAd.Load(DeathInterstitialUnitId(), new AdRequest(), (ad, err) =>
        {
            _deathLoading = false;
            if (err != null || ad == null)
                return;
            _deathInterstitial = ad;
            WireDeathInterstitialCallbacks(ad);
        });
    }

    void WireDeathInterstitialCallbacks(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            RestoreGameplayPortraitOrientation();
            _afterDeathInterstitial?.Invoke();
            _afterDeathInterstitial = null;
            ad.Destroy();
            if (_deathInterstitial == ad)
                _deathInterstitial = null;
            RequestDeathInterstitial();
        };
        ad.OnAdFullScreenContentFailed += _ =>
        {
            RestoreGameplayPortraitOrientation();
            _afterDeathInterstitial?.Invoke();
            _afterDeathInterstitial = null;
            ad.Destroy();
            if (_deathInterstitial == ad)
                _deathInterstitial = null;
            RequestDeathInterstitial();
        };
    }

    void CreateOrLoadBanner()
    {
        DestroyBanner();
        try
        {
            var size = AdSize.Banner;
            _bannerView = new BannerView(BannerUnitId(), size, AdPosition.Bottom);
            _bannerView.LoadAd(new AdRequest());
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AdsManager] Banner: " + e.Message);
        }
    }

    void DestroyBanner()
    {
        if (_bannerView == null)
            return;
        _bannerView.Destroy();
        _bannerView = null;
    }

    /// <summary>
    /// Wall Rush é retrato. O SDK pode alterar a orientação ao fechar vídeo/intersticial — repõe retrato.
    /// </summary>
    public static void RestoreGameplayPortraitOrientation()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    public void RegisterGameOver()
    {
        _gameOverCount++;
        _deathsAccumulatedForDeathAd++;
    }

    public void TryShowInterstitialThen(Action onFinished)
    {
        var sessionCooldownOk = Time.unscaledTime - _lastSessionInterstitialUnscaledTime >=
                                minSecondsBetweenSessionInterstitials;
        var needGameOvers = Mathf.Max(1, showInterstitialEveryNGameOvers);
        if (!_initialized || _sessionInterstitial == null || !_sessionInterstitial.CanShowAd() ||
            _gameOverCount < needGameOvers || !sessionCooldownOk)
        {
            onFinished?.Invoke();
            return;
        }

        _gameOverCount = 0;
        _lastSessionInterstitialUnscaledTime = Time.unscaledTime;
        _afterSessionInterstitial = onFinished;
        _sessionInterstitial.Show();
    }

    public void TryShowDeathInterstitialThen(Action onFinished)
    {
        var deathCooldownOk = Time.unscaledTime - _lastDeathInterstitialUnscaledTime >=
                              minSecondsBetweenDeathInterstitials;
        var needDeaths = Mathf.Max(1, deathInterstitialEveryNDeaths);
        var deathCountOk = _deathsAccumulatedForDeathAd >= needDeaths;
        if (!_initialized || _deathInterstitial == null || !_deathInterstitial.CanShowAd() ||
            !deathCooldownOk || !deathCountOk)
        {
            onFinished?.Invoke();
            return;
        }

        _deathsAccumulatedForDeathAd = 0;
        _lastDeathInterstitialUnscaledTime = Time.unscaledTime;
        _afterDeathInterstitial = onFinished;
        _deathInterstitial.Show();
    }

    public IEnumerator RunDeathInterstitialIfAny()
    {
        var done = false;
        TryShowDeathInterstitialThen(() => done = true);
        while (!done)
            yield return null;
    }
}
