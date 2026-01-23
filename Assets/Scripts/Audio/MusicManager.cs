using UnityEngine;
using System.Collections;

namespace Sisifos.Audio
{
    /// <summary>
    /// Yaşam dönemleri arası müzik geçişlerini yöneten sistem.
    /// Kutu toplandıkça müzik yumuşak bir şekilde değişir.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }
        
        [Header("Music Tracks")]
        [Tooltip("Ana menü müziği")]
        public AudioClip mainMenuMusic;
        
        [Tooltip("Çocukluk dönemi müziği")]
        public AudioClip childhoodMusic;
        
        [Tooltip("Ergenlik dönemi müziği")]
        public AudioClip teenageMusic;
        
        [Tooltip("Yetişkinlik dönemi müziği")]
        public AudioClip adulthoodMusic;
        
        [Tooltip("Yaşlılık dönemi müziği")]
        public AudioClip elderhoodMusic;
        
        [Header("Crossfade Settings")]
        [Tooltip("Geçiş süresi (saniye)")]
        [Range(1f, 10f)]
        public float crossfadeDuration = 3f;
        
        [Tooltip("Genel müzik ses seviyesi")]
        [Range(0f, 1f)]
        public float masterVolume = 0.7f;
        
        [Header("Playback Settings")]
        [Tooltip("Başlangıçta hangi müzik çalsın?")]
        public MusicTrack startingTrack = MusicTrack.MainMenu;
        
        [Tooltip("Oyun başladığında otomatik başlat")]
        public bool playOnStart = true;
        
        // İki AudioSource - crossfade için
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool _isSourceAActive = true;
        
        // Aktif olan source
        private AudioSource ActiveSource => _isSourceAActive ? _sourceA : _sourceB;
        private AudioSource InactiveSource => _isSourceAActive ? _sourceB : _sourceA;
        
        // Geçiş durumu
        private Coroutine _crossfadeCoroutine;
        private MusicTrack _currentTrack = MusicTrack.None;
        
        public MusicTrack CurrentTrack => _currentTrack;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // AudioSource'ları oluştur
            CreateAudioSources();
        }
        
        private void Start()
        {
            if (playOnStart)
            {
                PlayTrack(startingTrack);
            }
        }
        
        private void CreateAudioSources()
        {
            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceB = gameObject.AddComponent<AudioSource>();
            
            ConfigureAudioSource(_sourceA);
            ConfigureAudioSource(_sourceB);
        }
        
        private void ConfigureAudioSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D ses
            source.volume = 0f;
        }
        
        /// <summary>
        /// Belirtilen müziği yumuşak geçişle çalar
        /// </summary>
        public void PlayTrack(MusicTrack track)
        {
            if (track == _currentTrack) return;
            
            AudioClip clip = GetClipForTrack(track);
            if (clip == null)
            {
                Debug.LogWarning($"[MusicManager] {track} için müzik atanmamış!");
                return;
            }
            
            _currentTrack = track;
            
            // Crossfade başlat
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }
            _crossfadeCoroutine = StartCoroutine(CrossfadeToClip(clip));
            
            Debug.Log($"[MusicManager] Müzik değişiyor: {track}");
        }
        
        /// <summary>
        /// Yaşam dönemine göre müzik çalar (CharacterEvolution ile entegre)
        /// Stage 0-1 = MainMenu (başlangıç ve 1. kutu - aynı müzik)
        /// Stage 2 = 2. kutu sonrası (Teenage)
        /// Stage 3 = 3. kutu sonrası (Adulthood)
        /// Stage 4+ = 4. kutu sonrası (Elderwood)
        /// </summary>
        public void PlayMusicForLifeStage(int stageIndex)
        {
            // Stage 0 ve 1 aynı müzik - ilk kutuda değişim yok
            MusicTrack track = stageIndex switch
            {
                0 => MusicTrack.MainMenu,     // Başlangıç
                1 => MusicTrack.MainMenu,     // 1. kutu - aynı müzik (değişmez)
                2 => MusicTrack.Teenage,      // 2. kutu sonrası
                3 => MusicTrack.Adulthood,    // 3. kutu sonrası
                _ => MusicTrack.Elderwood     // 4+ kutu sonrası
            };
            
            PlayTrack(track);
        }
        
        private AudioClip GetClipForTrack(MusicTrack track)
        {
            return track switch
            {
                MusicTrack.MainMenu => mainMenuMusic,
                MusicTrack.Childhood => childhoodMusic,
                MusicTrack.Teenage => teenageMusic,
                MusicTrack.Adulthood => adulthoodMusic,
                MusicTrack.Elderwood => elderhoodMusic,
                _ => null
            };
        }
        
        private IEnumerator CrossfadeToClip(AudioClip newClip)
        {
            // Yeni müziği inactive source'a yükle ve başlat
            InactiveSource.clip = newClip;
            InactiveSource.volume = 0f;
            InactiveSource.Play();
            
            float elapsed = 0f;
            float startVolumeActive = ActiveSource.volume;
            
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossfadeDuration;
                
                // Smooth geçiş (ease in-out)
                float smoothT = t * t * (3f - 2f * t);
                
                // Eski müzik azalır
                ActiveSource.volume = Mathf.Lerp(startVolumeActive, 0f, smoothT);
                
                // Yeni müzik artar
                InactiveSource.volume = Mathf.Lerp(0f, masterVolume, smoothT);
                
                yield return null;
            }
            
            // Geçiş tamamlandı
            ActiveSource.Stop();
            ActiveSource.volume = 0f;
            InactiveSource.volume = masterVolume;
            
            // Source'ları değiştir
            _isSourceAActive = !_isSourceAActive;
            
            _crossfadeCoroutine = null;
        }
        
        /// <summary>
        /// Müziği durdurur (fade out ile)
        /// </summary>
        public void StopMusic(float fadeOutDuration = 2f)
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }
            StartCoroutine(FadeOutCoroutine(fadeOutDuration));
        }
        
        private IEnumerator FadeOutCoroutine(float duration)
        {
            float startVolume = ActiveSource.volume;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                ActiveSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            
            ActiveSource.Stop();
            _currentTrack = MusicTrack.None;
        }
        
        /// <summary>
        /// Master volume'u değiştirir
        /// </summary>
        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (ActiveSource.isPlaying)
            {
                ActiveSource.volume = masterVolume;
            }
        }
        
        /// <summary>
        /// Müziği duraklatır
        /// </summary>
        public void Pause()
        {
            ActiveSource.Pause();
            InactiveSource.Pause();
        }
        
        /// <summary>
        /// Müziği devam ettirir
        /// </summary>
        public void Resume()
        {
            ActiveSource.UnPause();
        }
    }
    
    /// <summary>
    /// Müzik parçası türleri
    /// </summary>
    public enum MusicTrack
    {
        None,
        MainMenu,
        Childhood,
        Teenage,
        Adulthood,
        Elderwood
    }
}
