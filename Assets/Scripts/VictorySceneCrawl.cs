using UnityEngine;
using TMPro;
using System.Collections;

public class VictorySceneCrawl : MonoBehaviour
{
    [Header("Text Crawl (assign in scene)")]
    [SerializeField] private RectTransform _crawlContainer;
    [SerializeField] private TextMeshProUGUI _crawlText;

    [Header("Timing")]
    [SerializeField] private float _startDelay = 2f;
    [SerializeField] private float _crawlDuration = 120f;
    [SerializeField] private float _totalScrollDistance = 5000f;

    [Header("Audio")]
    [SerializeField] private AudioClip _musicClip;
    [Range(0f, 1f)]
    [SerializeField] private float _musicVolume = 0.5f;
    [SerializeField] private AudioSource _musicSource;

    [Header("After Crawl")]
    [SerializeField] private string _nextSceneName = "";
    [SerializeField] private float _endDelay = 3f;

    private Vector2 _startPos;

    void Start()
    {
        Debug.Log("[VictoryCrawl] Starting...");

        if (_crawlContainer != null)
        {
            _startPos = _crawlContainer.anchoredPosition;
            Debug.Log($"[VictoryCrawl] Container found at {_startPos}");
        }
        else
        {
            Debug.LogError("[VictoryCrawl] No crawl container assigned!");
        }

        if (_crawlText != null)
        {
            Debug.Log($"[VictoryCrawl] Text length: {_crawlText.text.Length} chars");
        }
        else
        {
            Debug.LogError("[VictoryCrawl] No crawl text assigned!");
        }

        // Check for AudioListener FIRST
        if (FindFirstObjectByType<AudioListener>() == null)
        {
            Debug.Log("[VictoryCrawl] No AudioListener in scene! Adding one...");
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                GameObject camObj = new GameObject("AudioCamera");
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
        }

        // Setup music source
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
        }

        // Use assigned clip, or check if AudioSource already has one
        if (_musicClip != null)
        {
            _musicSource.clip = _musicClip;
        }

        if (_musicSource.clip != null)
        {
            _musicSource.volume = _musicVolume;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f; // 2D sound
            _musicSource.playOnAwake = false;
            Debug.Log($"[VictoryCrawl] Music ready: {_musicSource.clip.name} at volume {_musicVolume}");
        }
        else
        {
            Debug.LogWarning("[VictoryCrawl] No music clip assigned!");
        }

        StartCoroutine(PlayCrawl());
    }

    IEnumerator PlayCrawl()
    {
        // Start music after a brief delay to ensure AudioListener is ready
        yield return null; // Wait one frame

        if (_musicSource != null && _musicSource.clip != null)
        {
            _musicSource.Play();
            Debug.Log($"[VictoryCrawl] Started playing music. isPlaying: {_musicSource.isPlaying}");
        }

        yield return new WaitForSeconds(_startDelay);

        if (_crawlContainer == null)
        {
            Debug.LogError("[VictoryCrawl] No crawl container assigned!");
            yield break;
        }

        // Get actual text height from all text chunks in container
        float scrollDistance = _totalScrollDistance;
        if (_crawlContainer != null)
        {
            // Calculate total height from all TMP children
            float totalHeight = 0f;
            TextMeshProUGUI[] allTexts = _crawlContainer.GetComponentsInChildren<TextMeshProUGUI>();
            Debug.Log($"[VictoryCrawl] Found {allTexts.Length} text chunks in container");
            foreach (var text in allTexts)
            {
                text.ForceMeshUpdate();
                float h = text.preferredHeight;
                totalHeight += h + 50f;
                Debug.Log($"[VictoryCrawl] Chunk '{text.gameObject.name}' height: {h}");
            }
            if (totalHeight > 0)
            {
                scrollDistance = totalHeight + 1500f;
            }
            Debug.Log($"[VictoryCrawl] Total scroll distance: {scrollDistance}");
        }

        float elapsed = 0f;
        while (elapsed < _crawlDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _crawlDuration;
            float newY = _startPos.y + (scrollDistance * t);
            _crawlContainer.anchoredPosition = new Vector2(_startPos.x, newY);
            yield return null;
        }

        yield return new WaitForSeconds(_endDelay);

        if (!string.IsNullOrEmpty(_nextSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }
    }
}
