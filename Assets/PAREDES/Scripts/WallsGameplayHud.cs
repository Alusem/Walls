using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mantém textos e painéis do HUD alinhados com <see cref="ThemeManager"/> quando o fundo muda.
/// </summary>
public class WallsGameplayHud : MonoBehaviour
{
    GameManager _gm;
    Image _gameOverPanelImage;

    void Awake()
    {
        _gm = GetComponent<GameManager>();
    }

    public void Setup()
    {
        ThemeManager.ThemeChanged += OnTheme;
        if (_gm != null && _gm.GameOverPanel != null)
        {
            _gameOverPanelImage = _gm.GameOverPanel.GetComponent<Image>();
            if (_gameOverPanelImage == null)
                _gameOverPanelImage = _gm.GameOverPanel.GetComponentInChildren<Image>(true);
        }
        OnTheme(ThemeManager.Palette);
    }

    void OnDestroy()
    {
        ThemeManager.ThemeChanged -= OnTheme;
    }

    void OnTheme(ThemePalette p)
    {
        if (_gm == null)
            return;
        if (_gm.CurrentScoreText != null)
        {
            var c = p.TextPrimary;
            c.a = 0.38f;
            _gm.CurrentScoreText.color = c;
        }
        if (_gm.BestScoreText != null)
        {
            var c = p.TextPrimary;
            c.a = 0.45f;
            _gm.BestScoreText.color = c;
        }
        if (_gm.BestText != null)
        {
            var c = p.TextSecondary;
            c.a = 0.5f;
            _gm.BestText.color = c;
        }
        if (_gm.TouchToStartText != null)
        {
            var touch = _gm.TouchToStartText.GetComponentInChildren<TextMeshProUGUI>(true);
            if (touch != null)
            {
                var c = p.TextSecondary;
                c.a = 0.62f;
                touch.color = c;
            }
        }
        if (_gameOverPanelImage != null)
            _gameOverPanelImage.color = p.ModalCard;
    }
}
