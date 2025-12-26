using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string _sceneName = "VictoryScene";

    [Header("Transition Settings")]
    [SerializeField] private float _fadeTime = 1f;
    [SerializeField] private bool _requireInteract = false; // If true, needs button press

    [Header("Optional")]
    [SerializeField] private string _requiredTag = "Player";

    private bool _triggered = false;
    private CanvasGroup _fadePanel;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        if (!string.IsNullOrEmpty(_requiredTag) && !other.CompareTag(_requiredTag))
            return;

        if (_requireInteract)
        {
            // Show prompt - player needs to press a button
            Debug.Log($"[SceneTransition] Press E to enter {_sceneName}");
            return;
        }

        TriggerTransition();
    }

    void OnTriggerStay(Collider other)
    {
        if (_triggered || !_requireInteract) return;

        if (!string.IsNullOrEmpty(_requiredTag) && !other.CompareTag(_requiredTag))
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerTransition();
        }
    }

    public void TriggerTransition()
    {
        if (_triggered) return;
        _triggered = true;

        Debug.Log($"[SceneTransition] Loading {_sceneName}...");
        StartCoroutine(FadeAndLoad());
    }

    System.Collections.IEnumerator FadeAndLoad()
    {
        // Create fade panel
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        UnityEngine.UI.Image img = panelObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0);

        RectTransform rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Fade to black
        float elapsed = 0f;
        while (elapsed < _fadeTime)
        {
            elapsed += Time.deltaTime;
            img.color = new Color(0, 0, 0, elapsed / _fadeTime);
            yield return null;
        }

        // Load scene
        SceneManager.LoadScene(_sceneName);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
        }
    }
}
