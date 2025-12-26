using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private const string POOL_KEY = "DamagePopup";

    private TextMeshProUGUI _text;
    private TextMeshProUGUI[] _allTexts; // Cached to avoid GetComponentsInChildren every frame
    private Canvas _canvas;
    private RectTransform _canvasRect;
    private float _lifetime = 1.5f;
    private float _spawnTime;
    private Vector3 _velocity;
    private Color _startColor;
    private bool _initialized = false;

    public static void Create(Vector3 position, float damage)
    {
        GameObject popup = ObjectPool.Instance.Get(POOL_KEY);
        popup.transform.position = position;

        DamagePopup dp = popup.GetComponent<DamagePopup>();
        if (dp == null)
            dp = popup.AddComponent<DamagePopup>();

        dp.Activate(damage);
    }

    void Activate(float damage)
    {
        _spawnTime = Time.time;

        if (!_initialized)
        {
            SetupComponents(damage);
            _initialized = true;
        }
        else
        {
            // Reuse existing components
            UpdateDamageText(damage);
            ResetAlpha();
        }

        // Random upward velocity
        _velocity = new Vector3(Random.Range(-0.5f, 0.5f), 2f, Random.Range(-0.5f, 0.5f));
    }

    void SetupComponents(float damage)
    {
        // Create a world-space canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 100;

        // Set canvas size
        _canvasRect = _canvas.GetComponent<RectTransform>();
        _canvasRect.sizeDelta = new Vector2(2f, 1f);
        _canvasRect.localScale = Vector3.one * 0.02f;

        string damageText = "-" + Mathf.RoundToInt(damage).ToString();

        // Create shadow/3D depth layers (back to front)
        for (int i = 3; i >= 1; i--)
        {
            GameObject shadowObj = new GameObject($"Shadow{i}");
            shadowObj.transform.SetParent(transform);
            shadowObj.transform.localPosition = Vector3.zero;
            shadowObj.transform.localRotation = Quaternion.identity;
            shadowObj.transform.localScale = Vector3.one;

            TextMeshProUGUI shadow = shadowObj.AddComponent<TextMeshProUGUI>();
            shadow.text = damageText;
            shadow.fontSize = 48;
            shadow.fontStyle = FontStyles.Bold;
            shadow.fontWeight = FontWeight.Black;
            shadow.alignment = TextAlignmentOptions.Center;
            shadow.color = new Color32(80, 0, 0, 255);
            shadow.outlineWidth = 0.35f;
            shadow.outlineColor = new Color32(0, 0, 0, 255);

            RectTransform shadowRect = shadow.GetComponent<RectTransform>();
            shadowRect.sizeDelta = new Vector2(200f, 100f);
            shadowRect.anchoredPosition = new Vector2(i * 2f, -i * 2f);
        }

        // Create main text on top
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one;

        _text = textObj.AddComponent<TextMeshProUGUI>();
        _text.text = damageText;
        _text.fontSize = 48;
        _text.fontStyle = FontStyles.Bold;
        _text.fontWeight = FontWeight.Black;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = new Color32(255, 50, 50, 255);
        _text.outlineWidth = 0.35f;
        _text.outlineColor = new Color32(0, 0, 0, 255);
        _startColor = _text.color;

        RectTransform textRect = _text.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200f, 100f);
        textRect.anchoredPosition = Vector2.zero;

        // Cache all text components once
        _allTexts = GetComponentsInChildren<TextMeshProUGUI>();
    }

    void UpdateDamageText(float damage)
    {
        string damageText = "-" + Mathf.RoundToInt(damage).ToString();
        for (int i = 0; i < _allTexts.Length; i++)
        {
            _allTexts[i].text = damageText;
        }
    }

    void ResetAlpha()
    {
        for (int i = 0; i < _allTexts.Length; i++)
        {
            Color c = _allTexts[i].color;
            c.a = 1f;
            _allTexts[i].color = c;
        }
        _canvasRect.localScale = Vector3.one * 0.02f;
    }

    void Update()
    {
        if (_text == null) return;

        float elapsed = Time.time - _spawnTime;
        float t = elapsed / _lifetime;

        // Move upward and slow down
        transform.position += _velocity * Time.deltaTime;
        _velocity *= 0.95f;

        // Fade out all text layers (using cached array)
        float alpha = 1f - t;
        for (int i = 0; i < _allTexts.Length; i++)
        {
            Color c = _allTexts[i].color;
            c.a = alpha;
            _allTexts[i].color = c;
        }

        // Scale down slightly
        float scale = Mathf.Lerp(0.02f, 0.01f, t);
        _canvasRect.localScale = Vector3.one * scale;

        // Face camera (billboard)
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        // Return to pool when done
        if (elapsed >= _lifetime)
        {
            ObjectPool.Instance.Return(POOL_KEY, gameObject);
        }
    }
}
