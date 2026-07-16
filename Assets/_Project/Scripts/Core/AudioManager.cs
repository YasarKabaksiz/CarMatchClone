using UnityEngine;
using CarMatchClone.Core.Events;
using CarMatchClone.Data;
using CarMatchClone.Gameplay;

namespace CarMatchClone.Core
{
    public class AudioManager : MonoBehaviour
    {
        [Header("AudioSource'lar")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;

        [Header("Event Channels")]
        [SerializeField] private FruitEventChannel   _onFruitSelectedChannel;
        [SerializeField] private ColorEventChannel   _onMatchOccurredChannel;
        [SerializeField] private BoosterEventChannel _onBoosterUsedChannel;
        [SerializeField] private VoidEventChannel    _onLevelCompleteChannel;
        [SerializeField] private VoidEventChannel    _onGameOverChannel;

        [Header("Meyve Seçimi (rastgele)")]
        [SerializeField] private AudioClip[] _fruitSelectedSounds;

        [Header("Eşleşme (rastgele)")]
        [SerializeField] private AudioClip[] _matchSounds;

        [Header("Tekil Sesler")]
        [SerializeField] private AudioClip _boosterSound;
        [SerializeField] private AudioClip _levelCompleteSound;
        [SerializeField] private AudioClip _gameOverSound;
        [SerializeField] private AudioClip _uiClickSound;

        private void Awake()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);

            if (_musicSource != null && _musicSource.clip != null)
                _musicSource.Play();
        }

        private void OnEnable()
        {
            _onFruitSelectedChannel?.Subscribe(HandleFruitSelected);
            _onMatchOccurredChannel?.Subscribe(HandleMatchOccurred);
            _onBoosterUsedChannel?.Subscribe(HandleBoosterUsed);
            _onLevelCompleteChannel?.Subscribe(HandleLevelComplete);
            _onGameOverChannel?.Subscribe(HandleGameOver);
        }

        private void OnDisable()
        {
            _onFruitSelectedChannel?.Unsubscribe(HandleFruitSelected);
            _onMatchOccurredChannel?.Unsubscribe(HandleMatchOccurred);
            _onBoosterUsedChannel?.Unsubscribe(HandleBoosterUsed);
            _onLevelCompleteChannel?.Unsubscribe(HandleLevelComplete);
            _onGameOverChannel?.Unsubscribe(HandleGameOver);
        }

        private void HandleFruitSelected(Fruit _)     => PlayRandom(_fruitSelectedSounds);
        private void HandleMatchOccurred(FruitType _)  => PlayRandom(_matchSounds);
        private void HandleBoosterUsed(BoosterType _)  => Play(_boosterSound);
        private void HandleLevelComplete()             => Play(_levelCompleteSound);
        private void HandleGameOver()                  => Play(_gameOverSound);

        public void PlayUIClick() => Play(_uiClickSound);

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            _sfxSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        }
    }
}
