using UnityEngine;

public class TutorialVoice : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip _voiceClip;
    [SerializeField] private AudioSource _audioSource;

    [Header("Settings")]
    [SerializeField] private bool _playOnFirstTime = true;
    [Range(0f, 5f)]
    [SerializeField] private float _delay = 1f;

    private const string KEY = "HeardTutorial";

    void Start()
    {
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (_playOnFirstTime && PlayerPrefs.GetInt(KEY, 0) == 0)
        {
            Invoke(nameof(PlayVoice), _delay);
        }
    }

    public void PlayVoice()
    {
        if (_voiceClip == null)
        {
            Debug.LogWarning("[TutorialVoice] No voice clip assigned!");
            return;
        }

        _audioSource.PlayOneShot(_voiceClip);
        PlayerPrefs.SetInt(KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("[TutorialVoice] Playing tutorial voice");
    }

    public void ResetFlag()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
