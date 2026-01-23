using UnityEngine;
using UnityEngine.Rendering;

namespace Sisifos.Lighting
{
    /// <summary>
    /// İç mekanlara girişte lighting ayarlarını değiştiren trigger sistemi.
    /// Directional Light, Ambient, Fog ve Environment ayarlarını smooth şekilde değiştirir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteriorZoneTrigger : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "InteriorZone";
        
        [Header("Directional Light")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private float interiorLightIntensity = 0f;
        
        [Header("Interior Ambient Settings")]
        [Tooltip("İç mekanda Ambient Mode'u Flat'e çevirir (Skybox yerine)")]
        [SerializeField] private bool useFloorAmbientInInterior = true;
        [SerializeField] private Color interiorAmbientColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private float interiorAmbientIntensity = 0f;
        
        [Header("Interior Fog Settings")]
        [SerializeField] private bool enableInteriorFog = true;
        [SerializeField] private Color interiorFogColor = new Color(0.1f, 0.1f, 0.12f);
        [SerializeField] private float interiorFogDensity = 0.02f;
        
        [Header("Environment Reflections")]
        [SerializeField] private float interiorReflectionIntensity = 0f;
        
        [Header("Post-Processing")]
        [Tooltip("Dış mekan post-processing volume (içeri girilince 0 olur)")]
        [SerializeField] private Volume exteriorVolume;
        [Tooltip("Bu interior zone için özel post-processing volume")]
        [SerializeField] private Volume interiorVolume;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 1.5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Original outdoor values (saved on Start)
        private float _originalLightIntensity;
        private UnityEngine.Rendering.AmbientMode _originalAmbientMode;
        private Color _originalAmbientColor;
        private float _originalAmbientIntensity;
        private bool _originalFogEnabled;
        private Color _originalFogColor;
        private float _originalFogDensity;
        private float _originalReflectionIntensity;
        
        // Current transition state
        private float _transitionProgress = 0f; // 0 = outdoor, 1 = indoor
        private float _targetProgress = 0f;
        private bool _isTransitioning = false;
        private bool _isPlayerInside = false;
        
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
            
            // Interior volume başlangıçta weight = 0
            if (interiorVolume != null)
            {
                interiorVolume.weight = 0f;
            }
            
            // Başlangıç değerlerini kaydet
            SaveOriginalSettings();
        }
        
        private Light FindDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    return light;
                }
            }
            Debug.LogWarning($"[InteriorZone] {zoneName}: Directional Light bulunamadı!");
            return null;
        }
        
        private void SaveOriginalSettings()
        {
            // Directional Light
            if (directionalLight != null)
            {
                _originalLightIntensity = directionalLight.intensity;
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
                Debug.Log($"[InteriorZone] {zoneName}: Original settings saved - Light: {_originalLightIntensity}, Ambient: {_originalAmbientIntensity}, Reflection: {_originalReflectionIntensity}");
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
                    string state = _targetProgress > 0.5f ? "INTERIOR" : "EXTERIOR";
                    Debug.Log($"[InteriorZone] {zoneName}: Transition complete - {state}");
                }
            }
        }
        
        private void ApplyLightingSettings(float t)
        {
            // Smooth interpolation curve
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // Directional Light intensity
            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(_originalLightIntensity, interiorLightIntensity, smoothT);
            }
            
            // Ambient Mode - switch to Flat when inside
            if (useFloorAmbientInInterior)
            {
                // Eşik değerinde mod değiştir
                RenderSettings.ambientMode = smoothT > 0.1f 
                    ? UnityEngine.Rendering.AmbientMode.Flat 
                    : _originalAmbientMode;
            }
            
            // Ambient Light
            RenderSettings.ambientLight = Color.Lerp(_originalAmbientColor, interiorAmbientColor, smoothT);
            RenderSettings.ambientIntensity = Mathf.Lerp(_originalAmbientIntensity, interiorAmbientIntensity, smoothT);
            
            // Fog
            if (enableInteriorFog || _originalFogEnabled)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(_originalFogColor, interiorFogColor, smoothT);
                RenderSettings.fogDensity = Mathf.Lerp(_originalFogDensity, interiorFogDensity, smoothT);
            }
            
            // Reflection Intensity
            RenderSettings.reflectionIntensity = Mathf.Lerp(_originalReflectionIntensity, interiorReflectionIntensity, smoothT);
            
            // Post-Processing Volume weights
            // Exterior: 1 → 0 (içeri girerken)
            // Interior: 0 → 1 (içeri girerken)
            if (exteriorVolume != null)
            {
                exteriorVolume.weight = 1f - smoothT;
            }
            if (interiorVolume != null)
            {
                interiorVolume.weight = smoothT;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            _isPlayerInside = true;
            _targetProgress = 1f;
            _isTransitioning = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[InteriorZone] Player entered: {zoneName}");
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            _isPlayerInside = false;
            _targetProgress = 0f;
            _isTransitioning = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[InteriorZone] Player exited: {zoneName}");
            }
        }
        
        /// <summary>
        /// Force transition to interior settings immediately.
        /// </summary>
        public void ForceInterior()
        {
            _transitionProgress = 1f;
            _targetProgress = 1f;
            ApplyLightingSettings(1f);
            _isTransitioning = false;
        }
        
        /// <summary>
        /// Force transition to exterior settings immediately.
        /// </summary>
        public void ForceExterior()
        {
            _transitionProgress = 0f;
            _targetProgress = 0f;
            ApplyLightingSettings(0f);
            _isTransitioning = false;
        }
        
        /// <summary>
        /// Manually refresh original settings (call if outdoor lighting changed).
        /// </summary>
        public void RefreshOriginalSettings()
        {
            if (!_isPlayerInside)
            {
                SaveOriginalSettings();
            }
        }
        
        private void OnDrawGizmos()
        {
            var collider = GetComponent<Collider>();
            if (collider == null) return;
            
            // İç mekan için turuncu/sarı renk
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.3f);
            
            if (collider is BoxCollider box)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"🏠 {zoneName}");
            #endif
        }
    }
}
