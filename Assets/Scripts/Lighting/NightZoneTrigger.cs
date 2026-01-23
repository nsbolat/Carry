using UnityEngine;
using UnityEngine.Rendering;

namespace Sisifos.Lighting
{
    /// <summary>
    /// Gece bölgelerine girişte lighting ayarlarını değiştiren trigger sistemi.
    /// Tek Directional Light kullanarak gölgelerin smooth geçişini sağlar.
    /// Skybox, Ambient, Fog ve Environment ayarlarını smooth şekilde değiştirir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NightZoneTrigger : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "NightZone";
        
        [Header("Directional Light (Tek ışık - smooth geçiş için)")]
        [Tooltip("Ana Directional Light - gündüz/gece arasında smooth geçiş yapılır")]
        [SerializeField] private Light directionalLight;
        
        [Header("Night Light Settings")]
        [Tooltip("Gece ışık rengi")]
        [SerializeField] private Color nightLightColor = new Color(0.4f, 0.5f, 0.8f);
        [Tooltip("Gece ışık yoğunluğu")]
        [SerializeField] private float nightLightIntensity = 0.3f;
        [Tooltip("Gece ışık rotasyonu (Euler angles)")]
        [SerializeField] private Vector3 nightLightRotation = new Vector3(30f, -130f, 0f);
        
        [Header("Skybox Settings")]
        [Tooltip("Gece skybox materyali")]
        [SerializeField] private Material nightSkyboxMaterial;
        
        [Header("Night Ambient Settings")]
        [SerializeField] private AmbientMode nightAmbientMode = AmbientMode.Flat;
        [SerializeField] private Color nightAmbientColor = new Color(0.05f, 0.05f, 0.15f);
        [SerializeField] private float nightAmbientIntensity = 0.3f;
        
        [Header("Night Fog Settings")]
        [SerializeField] private bool enableNightFog = true;
        [SerializeField] private Color nightFogColor = new Color(0.02f, 0.02f, 0.08f);
        [SerializeField] private float nightFogDensity = 0.015f;
        
        [Header("Environment Reflections")]
        [SerializeField] private float nightReflectionIntensity = 0.3f;
        
        [Header("Post-Processing")]
        [Tooltip("Gündüz post-processing volume (geceye girilince 0 olur)")]
        [SerializeField] private Volume dayVolume;
        [Tooltip("Gece post-processing volume")]
        [SerializeField] private Volume nightVolume;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2f;
        [Tooltip("Geçiş eğrisi")]
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Original day values (saved on Start)
        private Material _originalSkyboxMaterial;
        private AmbientMode _originalAmbientMode;
        private Color _originalAmbientColor;
        private float _originalAmbientIntensity;
        private bool _originalFogEnabled;
        private Color _originalFogColor;
        private float _originalFogDensity;
        private float _originalReflectionIntensity;
        
        // Directional light original values
        private Color _originalLightColor;
        private float _originalLightIntensity;
        private Quaternion _originalLightRotation;
        
        // Current transition state
        private float _transitionProgress = 0f; // 0 = day, 1 = night
        private float _targetProgress = 0f;
        private bool _isTransitioning = false;
        private bool _isPlayerInNight = false;
        
        private void Start()
        {
            // Collider'ın trigger olduğundan emin ol
            var collider = GetComponent<Collider>();
            collider.isTrigger = true;
            
            // Directional light'ı otomatik bul (eğer atanmamışsa)
            if (directionalLight == null)
            {
                directionalLight = FindDirectionalLight();
            }
            
            // Night volume başlangıçta weight = 0
            if (nightVolume != null)
            {
                nightVolume.weight = 0f;
            }
            
            // Başlangıç değerlerini kaydet
            SaveOriginalSettings();
        }
        
        private Light FindDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional && light.enabled)
                {
                    return light;
                }
            }
            
            Debug.LogWarning($"[NightZone] {zoneName}: Directional Light bulunamadı!");
            return null;
        }
        
        private void SaveOriginalSettings()
        {
            // Skybox
            _originalSkyboxMaterial = RenderSettings.skybox;
            
            // Directional Light
            if (directionalLight != null)
            {
                _originalLightColor = directionalLight.color;
                _originalLightIntensity = directionalLight.intensity;
                _originalLightRotation = directionalLight.transform.rotation;
            }
            
            // Ambient
            _originalAmbientMode = RenderSettings.ambientMode;
            _originalAmbientColor = RenderSettings.ambientLight;
            _originalAmbientIntensity = RenderSettings.ambientIntensity;
            
            // Fog
            _originalFogEnabled = RenderSettings.fog;
            _originalFogColor = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;
            
            // Reflections
            _originalReflectionIntensity = RenderSettings.reflectionIntensity;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NightZone] {zoneName}: Original settings saved - Light: {_originalLightIntensity}, Color: {_originalLightColor}");
            }
        }
        
        private void Update()
        {
            if (!_isTransitioning) return;
            
            // Smooth transition
            float step = Time.deltaTime / transitionDuration;
            
            if (_targetProgress > _transitionProgress)
            {
                _transitionProgress = Mathf.Min(_transitionProgress + step, _targetProgress);
            }
            else
            {
                _transitionProgress = Mathf.Max(_transitionProgress - step, _targetProgress);
            }
            
            // Apply interpolated values
            ApplyLightingSettings(_transitionProgress);
            
            // Check if transition complete
            if (Mathf.Approximately(_transitionProgress, _targetProgress))
            {
                _isTransitioning = false;
                
                if (showDebugLogs)
                {
                    string state = _targetProgress > 0.5f ? "NIGHT" : "DAY";
                    Debug.Log($"[NightZone] {zoneName}: Transition complete - {state}");
                }
            }
        }
        
        private void ApplyLightingSettings(float t)
        {
            // Animation curve ile smooth interpolation
            float smoothT = transitionCurve.Evaluate(t);
            
            // === DIRECTIONAL LIGHT SMOOTH TRANSITION ===
            // Tek ışık kullanarak gölgeler de smooth geçiş yapar
            if (directionalLight != null)
            {
                // Renk geçişi
                directionalLight.color = Color.Lerp(_originalLightColor, nightLightColor, smoothT);
                
                // Intensity geçişi
                directionalLight.intensity = Mathf.Lerp(_originalLightIntensity, nightLightIntensity, smoothT);
                
                // Rotasyon geçişi (Slerp ile smooth)
                Quaternion nightRotation = Quaternion.Euler(nightLightRotation);
                directionalLight.transform.rotation = Quaternion.Slerp(_originalLightRotation, nightRotation, smoothT);
            }
            
            // === SKYBOX TRANSITION ===
            if (nightSkyboxMaterial != null)
            {
                // Belirli bir eşikte skybox değiştir (crossfade için özel shader gerekir)
                if (smoothT > 0.5f && RenderSettings.skybox != nightSkyboxMaterial)
                {
                    RenderSettings.skybox = nightSkyboxMaterial;
                    DynamicGI.UpdateEnvironment();
                }
                else if (smoothT <= 0.5f && RenderSettings.skybox != _originalSkyboxMaterial)
                {
                    RenderSettings.skybox = _originalSkyboxMaterial;
                    DynamicGI.UpdateEnvironment();
                }
            }
            
            // === AMBIENT SETTINGS ===
            if (smoothT > 0.1f)
            {
                RenderSettings.ambientMode = nightAmbientMode;
            }
            else
            {
                RenderSettings.ambientMode = _originalAmbientMode;
            }
            
            // Ambient Light
            RenderSettings.ambientLight = Color.Lerp(_originalAmbientColor, nightAmbientColor, smoothT);
            RenderSettings.ambientIntensity = Mathf.Lerp(_originalAmbientIntensity, nightAmbientIntensity, smoothT);
            
            // === FOG ===
            if (enableNightFog || _originalFogEnabled)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(_originalFogColor, nightFogColor, smoothT);
                RenderSettings.fogDensity = Mathf.Lerp(_originalFogDensity, nightFogDensity, smoothT);
            }
            
            // === REFLECTION INTENSITY ===
            RenderSettings.reflectionIntensity = Mathf.Lerp(_originalReflectionIntensity, nightReflectionIntensity, smoothT);
            
            // === POST-PROCESSING VOLUME WEIGHTS ===
            if (dayVolume != null)
            {
                dayVolume.weight = 1f - smoothT;
            }
            if (nightVolume != null)
            {
                nightVolume.weight = smoothT;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            _isPlayerInNight = true;
            _targetProgress = 1f;
            _isTransitioning = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NightZone] Player entered: {zoneName}");
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            _isPlayerInNight = false;
            _targetProgress = 0f;
            _isTransitioning = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[NightZone] Player exited: {zoneName}");
            }
        }
        
        /// <summary>
        /// Force transition to night settings immediately.
        /// </summary>
        public void ForceNight()
        {
            _transitionProgress = 1f;
            _targetProgress = 1f;
            ApplyLightingSettings(1f);
            _isTransitioning = false;
        }
        
        /// <summary>
        /// Force transition to day settings immediately.
        /// </summary>
        public void ForceDay()
        {
            _transitionProgress = 0f;
            _targetProgress = 0f;
            ApplyLightingSettings(0f);
            _isTransitioning = false;
        }
        
        /// <summary>
        /// Current transition progress (0 = day, 1 = night).
        /// </summary>
        public float TransitionProgress => _transitionProgress;
        
        /// <summary>
        /// Is player currently in a night zone?
        /// </summary>
        public bool IsPlayerInNight => _isPlayerInNight;
        
        /// <summary>
        /// Manually refresh original settings (call if day lighting changed).
        /// </summary>
        public void RefreshOriginalSettings()
        {
            if (!_isPlayerInNight)
            {
                SaveOriginalSettings();
            }
        }
        
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            
            // Gece için koyu mavi/mor renk
            Gizmos.color = new Color(0.2f, 0.1f, 0.5f, 0.3f);
            
            if (col is BoxCollider box)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0.4f, 0.2f, 0.8f, 0.8f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.color = new Color(0.4f, 0.2f, 0.8f, 0.8f);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"🌙 {zoneName}");
            #endif
        }
        
        private void OnValidate()
        {
            if (transitionCurve == null || transitionCurve.length == 0)
            {
                transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
}
