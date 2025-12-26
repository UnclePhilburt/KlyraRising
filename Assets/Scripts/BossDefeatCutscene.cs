using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossDefeatCutscene : MonoBehaviour
{
    [Header("Boss to Watch")]
    [SerializeField] private Enemy _boss;

    [Header("Transition")]
    [SerializeField] private string _victorySceneName = "VictoryScene";
    [SerializeField] private float _fadeOutDuration = 2f;
    [SerializeField] private float _delayBeforeFade = 1f;

    private bool _bossWasAssigned = false;
    private bool _triggered = false;
    private Canvas _fadeCanvas;
    private Image _fadePanel;

    void Start()
    {
        if (_boss == null)
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e.gameObject.name.ToLower().Contains("boss"))
                {
                    _boss = e;
                    break;
                }
            }
        }

        if (_boss != null)
        {
            _bossWasAssigned = true;
        }
    }

    void Update()
    {
        if (_triggered) return;

        if (_boss != null && !_bossWasAssigned)
        {
            _bossWasAssigned = true;
        }

        bool bossDead = (_bossWasAssigned && _boss == null) ||
                        (_boss != null && !_boss.IsAlive);

        if (bossDead)
        {
            _triggered = true;
            StartCoroutine(TransitionToVictory());
        }
    }

    IEnumerator TransitionToVictory()
    {
        // Disable player
        var player = FindFirstObjectByType<ThirdPersonController>();
        if (player != null) player.enabled = false;

        // Wait a moment
        yield return new WaitForSeconds(_delayBeforeFade);

        // Create fade panel
        CreateFadePanel();

        // Fade to black
        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _fadePanel.color = new Color(0, 0, 0, elapsed / _fadeOutDuration);
            yield return null;
        }

        // Load victory scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(_victorySceneName);
    }

    void CreateFadePanel()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        _fadeCanvas = canvasObj.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 999;

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(_fadeCanvas.transform, false);
        _fadePanel = panelObj.AddComponent<Image>();
        _fadePanel.color = new Color(0, 0, 0, 0);

        RectTransform rect = _fadePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
