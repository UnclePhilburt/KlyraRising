using UnityEngine;
using System.Collections;

public class BossIntro : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private Animator _bossAnimator;
    [SerializeField] private Transform _bossTransform;

    [Header("Death Sequence Timing")]
    [SerializeField] private float _pauseBeforeAttack = 1f;     // Dramatic pause before boss attacks
    [SerializeField] private float _slashHitTime = 0.3f;        // When the slash connects (match your animation)
    [SerializeField] private float _splitDelay = 0.1f;          // Delay after hit before splitting
    [SerializeField] private float _fadeToBlackDelay = 2.5f;    // Time before respawn

    [Header("Effects")]
    [SerializeField] private GameObject _slashEffectPrefab;
    [SerializeField] private GameObject _bloodEffectPrefab;
    [SerializeField] private AudioClip _bossSlashSound;
    [SerializeField] private AudioClip _deathSound;

    [Header("Respawn Voice")]
    [SerializeField] private AudioClip _respawnVoice;
    [SerializeField] private float _respawnVoiceDelay = 1f;

    [Header("After Death")]
    [SerializeField] private string _sceneToLoad = "";          // Leave empty to just respawn
    [SerializeField] private Transform _respawnPoint;

    [Header("Trigger Zone")]
    [SerializeField] private float _triggerRadius = 10f;

    private bool _triggered = false;
    private bool _playerDead = false;
    private AudioSource _audioSource;
    private ThirdPersonController _player;
    private Animator _playerAnimator;

    void Start()
    {
        // Setup trigger
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;
        col.radius = _triggerRadius;

        // Audio
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 0.5f;

        // Check if already beaten this boss intro
        if (PlayerPrefs.GetInt("BossIntro_" + gameObject.name, 0) == 1)
        {
            // Player already saw this, disable the instakill
            enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        _player = other.GetComponent<ThirdPersonController>();
        if (_player == null) return;

        _triggered = true;
        StartCoroutine(BossIntroSequence());
    }

    IEnumerator BossIntroSequence()
    {
        // Disable player control
        _player.enabled = false;
        _playerAnimator = _player.GetComponentInChildren<Animator>();

        // Disable player weapon
        WeaponController weapon = _player.GetComponent<WeaponController>();
        if (weapon != null) weapon.enabled = false;

        // Make player face boss
        Vector3 lookDir = (_bossTransform.position - _player.transform.position).normalized;
        lookDir.y = 0;
        _player.transform.rotation = Quaternion.LookRotation(lookDir);

        // Boss faces player
        Vector3 bossLookDir = (_player.transform.position - _bossTransform.position).normalized;
        bossLookDir.y = 0;
        _bossTransform.rotation = Quaternion.LookRotation(bossLookDir);

        // ===== DRAMATIC PAUSE - staredown =====
        yield return new WaitForSeconds(_pauseBeforeAttack);

        // ===== BOSS ATTACKS =====
        if (_bossAnimator != null)
        {
            _bossAnimator.SetTrigger("Attack");
        }

        // ===== WAIT FOR SLASH TO CONNECT =====
        yield return new WaitForSeconds(_slashHitTime);

        // Slash sound at moment of impact
        if (_bossSlashSound != null)
        {
            _audioSource.PlayOneShot(_bossSlashSound);
        }

        // Slash effect on player
        if (_slashEffectPrefab != null)
        {
            Vector3 effectPos = _player.transform.position + Vector3.up * 1f;
            GameObject slash = Instantiate(_slashEffectPrefab, effectPos, _bossTransform.rotation);
            Destroy(slash, 2f);
        }

        // ===== TINY PAUSE - the "oh no" moment =====
        yield return new WaitForSeconds(_splitDelay);

        // ===== PLAYER SPLITS IN HALF =====
        _playerDead = true;

        // Blood spray
        if (_bloodEffectPrefab != null)
        {
            Vector3 bloodPos = _player.transform.position + Vector3.up * 1f;
            GameObject blood = Instantiate(_bloodEffectPrefab, bloodPos, Quaternion.identity);
            Destroy(blood, 3f);
        }

        // Death sound
        if (_deathSound != null)
        {
            _audioSource.PlayOneShot(_deathSound);
        }

        // Split the player in half!
        SplitPlayer();

        // ===== WAIT THEN RESPAWN =====
        yield return new WaitForSeconds(_fadeToBlackDelay);

        // Mark as seen so next time they can actually fight
        PlayerPrefs.SetInt("BossIntro_" + gameObject.name, 1);
        PlayerPrefs.Save();

        // Respawn or load scene
        if (!string.IsNullOrEmpty(_sceneToLoad))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(_sceneToLoad);
        }
        else if (_respawnPoint != null)
        {
            RespawnPlayer();
        }
    }

    void SplitPlayer()
    {
        // Duplicate player and hide half on each - creates clean "cut in half" effect
        PlayerSplitEffect.SplitPlayer(_player.gameObject);
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(name.ToLower()))
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    void RespawnPlayer()
    {
        // Reset player
        _player.gameObject.SetActive(true);
        _player.transform.position = _respawnPoint.position;
        _player.transform.rotation = _respawnPoint.rotation;

        // Re-enable everything
        CharacterController cc = _player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        Rigidbody rb = _player.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        _player.enabled = true;

        WeaponController weapon = _player.GetComponent<WeaponController>();
        if (weapon != null) weapon.enabled = true;

        _triggered = false;
        _playerDead = false;

        // Play respawn voice after delay
        if (_respawnVoice != null)
        {
            Invoke(nameof(PlayRespawnVoice), _respawnVoiceDelay);
        }

        // Disable this intro trigger - boss is now a regular enemy
        enabled = false;

        // Disable the trigger collider so player can walk through
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null) col.enabled = false;
    }

    void PlayRespawnVoice()
    {
        if (_respawnVoice != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_respawnVoice);
        }
    }

    void OnDrawGizmos()
    {
        // Trigger zone
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, _triggerRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);

        // Line to boss
        if (_bossTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _bossTransform.position);
        }
    }
}
