using UnityEngine;

public class AmbientMusicManager : MonoBehaviour
{
    [Header("Music Tracks")]
    [SerializeField] private AudioClip[] _musicTracks;

    [Header("Settings")]
    [SerializeField] private float _volume = 0.5f;
    [SerializeField] private bool _shuffle = false;
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private float _fadeTime = 2f;

    private AudioSource _audioSource;
    private int _currentTrackIndex = 0;
    private bool _isFading = false;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Mathf.Clamp01(value);
            if (_audioSource != null && !_isFading)
                _audioSource.volume = _volume;
        }
    }

    void Awake()
    {
        // Create audio source
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = false;
        _audioSource.playOnAwake = false;
        _audioSource.volume = _volume;

        // Persist across scenes
        DontDestroyOnLoad(gameObject);

        // Make sure only one exists
        AmbientMusicManager[] managers = FindObjectsByType<AmbientMusicManager>(FindObjectsSortMode.None);
        if (managers.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (_playOnStart && _musicTracks.Length > 0)
        {
            if (_shuffle)
                _currentTrackIndex = Random.Range(0, _musicTracks.Length);

            PlayTrack(_currentTrackIndex);
        }
    }

    void Update()
    {
        // When track ends, play next
        if (_audioSource != null && !_audioSource.isPlaying && !_isFading && _musicTracks.Length > 0)
        {
            NextTrack();
        }
    }

    public void PlayTrack(int index)
    {
        if (_musicTracks.Length == 0) return;

        _currentTrackIndex = index % _musicTracks.Length;
        AudioClip clip = _musicTracks[_currentTrackIndex];

        if (clip != null)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
        }
    }

    public void NextTrack()
    {
        if (_musicTracks.Length == 0) return;

        if (_shuffle)
        {
            int newIndex;
            do
            {
                newIndex = Random.Range(0, _musicTracks.Length);
            } while (newIndex == _currentTrackIndex && _musicTracks.Length > 1);

            _currentTrackIndex = newIndex;
        }
        else
        {
            _currentTrackIndex = (_currentTrackIndex + 1) % _musicTracks.Length;
        }

        StartCoroutine(FadeToTrack(_currentTrackIndex));
    }

    public void PreviousTrack()
    {
        if (_musicTracks.Length == 0) return;

        _currentTrackIndex = (_currentTrackIndex - 1 + _musicTracks.Length) % _musicTracks.Length;
        StartCoroutine(FadeToTrack(_currentTrackIndex));
    }

    System.Collections.IEnumerator FadeToTrack(int index)
    {
        _isFading = true;

        // Fade out
        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < _fadeTime / 2f)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (_fadeTime / 2f));
            yield return null;
        }

        // Switch track
        _audioSource.Stop();
        AudioClip clip = _musicTracks[index];
        if (clip != null)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
        }

        // Fade in
        elapsed = 0f;
        while (elapsed < _fadeTime / 2f)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, _volume, elapsed / (_fadeTime / 2f));
            yield return null;
        }

        _audioSource.volume = _volume;
        _isFading = false;
    }

    public void Pause()
    {
        _audioSource.Pause();
    }

    public void Resume()
    {
        _audioSource.UnPause();
    }

    public void Stop()
    {
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        _isFading = true;
        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < _fadeTime)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _fadeTime);
            yield return null;
        }

        _audioSource.Stop();
        _audioSource.volume = _volume;
        _isFading = false;
    }
}
