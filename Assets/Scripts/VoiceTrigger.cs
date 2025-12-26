using UnityEngine;

public class VoiceTrigger : MonoBehaviour
{
    [Header("Voice")]
    [SerializeField] private AudioClip _voiceClip;
    [SerializeField] private AudioSource _audioSource;

    [Header("Settings")]
    [SerializeField] private bool _playOnce = true;
    [SerializeField] private string _triggerId = ""; // Unique ID if you want it saved across sessions
    [SerializeField] private float _delay = 0f;

    [Header("Trigger Size")]
    [SerializeField] private float _radius = 3f;

    private bool _hasPlayed = false;
    private SphereCollider _collider;

    void Start()
    {
        // Setup collider
        _collider = GetComponent<SphereCollider>();
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<SphereCollider>();
        }
        _collider.isTrigger = true;
        _collider.radius = _radius;

        // Setup audio source
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound for voice
        }

        // Check if already played (persistent)
        if (_playOnce && !string.IsNullOrEmpty(_triggerId))
        {
            _hasPlayed = PlayerPrefs.GetInt("Voice_" + _triggerId, 0) == 1;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only trigger for player
        if (!other.GetComponent<ThirdPersonController>()) return;

        if (_playOnce && _hasPlayed) return;

        if (_delay > 0)
            Invoke(nameof(PlayVoice), _delay);
        else
            PlayVoice();

        _hasPlayed = true;

        // Save if persistent
        if (_playOnce && !string.IsNullOrEmpty(_triggerId))
        {
            PlayerPrefs.SetInt("Voice_" + _triggerId, 1);
            PlayerPrefs.Save();
        }
    }

    void PlayVoice()
    {
        if (_voiceClip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(_voiceClip);
    }

    // Visualize trigger zone in editor
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, _radius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
