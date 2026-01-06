using UnityEngine;
using Sisifos.Player;

namespace Sisifos.Audio
{
    /// <summary>
    /// Gelişmiş adım sesi sistemi.
    /// Terrain texture'larını ve obje tag'lerini algılayarak dinamik ses çalar.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SurfaceCollection surfaceCollection;
        
        [Header("Audio Settings")]
        [SerializeField] private float baseVolume = 0.5f;
        [Tooltip("Rastgele pitch aralığı (örn. 0.8 - 1.2)")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 1.15f);
        
        [Header("Automation")]
        [Tooltip("Animation Event kullanılamıyorsa, hıza göre otomatik çal")]
        [SerializeField] private bool useTimer = true;
        [SerializeField] private float walkInterval = 0.5f;
        [SerializeField] private float runInterval = 0.3f;

        // Dependencies
        private AudioSource _audioSource;
        private SlopeCharacterController _characterController;
        private Terrain _currentTerrain;

        // State
        private float _stepTimer;
        private AudioClip _lastPlayedClip; // Aynı sesin üst üste çalmasını önle
        private float _cooldownTimer; // Minimum bekleme süresi
        private const float MIN_STEP_COOLDOWN = 0.15f; // Sesler arası minimum süre

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            // Parent'ta da arayabilmek için GetComponentInParent kullanıyoruz
            _characterController = GetComponentInParent<SlopeCharacterController>();
            
            // Temel AudioSource ayarları (distance ayarları Inspector'dan yapılacak)
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // Tam 3D ses
            _audioSource.dopplerLevel = 0f; // Doppler efekti kapalı (adım sesi için gereksiz)
        }

        private void Update()
        {
            // Cooldown timer'ı güncelle
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
            
            if (useTimer)
            {
                HandleAutoFootsteps();
            }
        }

        /// <summary>
        /// Timer tabanlı otomatik adım sesi (fallback için)
        /// </summary>
        private void HandleAutoFootsteps()
        {
            if (_characterController == null) return;

            // Sadece yerde ve hareket halindeyken
            if (_characterController.IsGrounded && _characterController.CurrentSpeed > 0.1f)
            {
                float currentInterval = _characterController.IsRunning ? runInterval : walkInterval;
                // Hıza göre intervali biraz daha dinamik yapabiliriz
                // Örn: Hızlı koşarken interval düşer
                
                _stepTimer += Time.deltaTime;
                if (_stepTimer >= currentInterval)
                {
                    PlayFootstep();
                    _stepTimer = 0f;
                }
            }
            else
            {
                _stepTimer = 0f; // Durunca timer sıfırla
            }
        }

        /// <summary>
        /// Adım sesi çalar. Animation Event tarafından çağrılabilir.
        /// </summary>
        public void PlayFootstep()
        {
            // Cooldown kontrolü - çok hızlı tetiklenmeyi önle
            if (_cooldownTimer > 0f) return;
            if (surfaceCollection == null || _audioSource == null) return;

            SurfaceDefinition surface = DetectSurface();
            if (surface != null && surface.footstepSounds.Count > 0)
            {
                // Rastgele ses seç (aynı sesi tekrar çalmamaya çalış)
                AudioClip clip = GetRandomClip(surface);
                
                // Rastgele pitch
                _audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                
                // Hıza göre volume biraz artabilir (opsiyonel)
                float speedVolumeFactor = (_characterController != null && _characterController.IsRunning) ? 1.2f : 1f;
                _audioSource.PlayOneShot(clip, baseVolume * surface.volumeMultiplier * speedVolumeFactor);
                
                // Cooldown başlat
                _cooldownTimer = MIN_STEP_COOLDOWN;
                _lastPlayedClip = clip;
            }
        }

        /// <summary>
        /// Aynı sesin üst üste çalmaması için akıllı seçim.
        /// </summary>
        private AudioClip GetRandomClip(SurfaceDefinition surface)
        {
            if (surface.footstepSounds.Count == 1)
                return surface.footstepSounds[0];
            
            // Farklı bir ses seçmeye çalış (max 3 deneme)
            for (int i = 0; i < 3; i++)
            {
                AudioClip candidate = surface.footstepSounds[Random.Range(0, surface.footstepSounds.Count)];
                if (candidate != _lastPlayedClip)
                    return candidate;
            }
            
            // 3 denemede de aynı geldiyse yine de çal
            return surface.footstepSounds[Random.Range(0, surface.footstepSounds.Count)];
        }

        /// <summary>
        /// Karakterin altındaki zemini algılar.
        /// </summary>
        private SurfaceDefinition DetectSurface()
        {
            RaycastHit hit;
            // Karakterin hafif yukarısından aşağıya ray at
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f))
            {
                // 1. Terrain kontrolü
                Terrain terrain = hit.collider.GetComponent<Terrain>();
                if (terrain != null)
                {
                    string textureName = GetDominantTexture(terrain, hit.point);
                    return surfaceCollection.GetSurfaceByTexture(textureName);
                }
                
                // 2. Mesh/Tag kontrolü
                // Mesh renderer üzerindeki materyal adına da bakılabilir ama Tag genelde daha performanslı/kolay
                return surfaceCollection.GetSurfaceByTag(hit.collider.tag);
            }

            return surfaceCollection.defaultSurface;
        }

        /// <summary>
        /// Terrain üzerindeki baskın texture'ın ismini bulur.
        /// </summary>
        private string GetDominantTexture(Terrain terrain, Vector3 worldPos)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;

            // Dünya pozisyonunu splatmap koordinatına çevir
            int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
            int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

            // Koordinatlar geçerli mi?
            if (mapX < 0 || mapX >= terrainData.alphamapWidth || mapZ < 0 || mapZ >= terrainData.alphamapHeight)
            {
                return "";
            }

            // Splatmap verisini al (3. boyut layer sayısıdır)
            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            // En baskın layer'ı bul
            float maxMix = 0;
            int maxIndex = 0;

            // texture sayısı
            int numTextures = splatmapData.GetLength(2);
            
            for (int i = 0; i < numTextures; i++)
            {
                if (splatmapData[0, 0, i] > maxMix)
                {
                    maxMix = splatmapData[0, 0, i];
                    maxIndex = i;
                }
            }

            // Terrain layer ismini döndür
            if (maxIndex < terrainData.terrainLayers.Length)
            {
                return terrainData.terrainLayers[maxIndex].name;
            }
            
            return "";
        }
    }
}
