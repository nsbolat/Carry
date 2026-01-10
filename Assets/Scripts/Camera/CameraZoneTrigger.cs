using UnityEngine;

namespace Sisifos.Camera
{
    /// <summary>
    /// Belirli bölgelerde kamera davranışını değiştiren trigger sistemi.
    /// Journey tarzı sinematik anlar ve atmosferik geçişler için kullanılır.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CameraZoneTrigger : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "CameraZone";
        [SerializeField] private bool isOneShot = false;

        [Header("Camera Preset")]
        [SerializeField] private CameraPreset cameraPreset = new CameraPreset
        {
            distance = 15f,
            height = 3f,
            offset = Vector3.zero,
            fieldOfView = 60f
        };

        [Header("Transition")]
        [SerializeField] private float enterTransitionTime = 1.5f;
        [SerializeField] private float exitTransitionTime = 1f;
        [SerializeField] private bool returnToDefaultOnExit = true;

        [Header("Optional: Cinemachine Override")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera overrideCamera;
        [SerializeField] private int cameraPriority = 15;

        [Header("Debug")]
        [SerializeField] private bool livePreview = true;

        // State
        private bool _hasTriggered = false;
        private bool _isPlayerInside = false;
        private DynamicCameraController _cameraController;
        private int _originalPriority;
        private CameraPreset _lastPreset;

        private void Start()
        {
            // Collider'ın trigger olduğundan emin ol
            var collider = GetComponent<Collider>();
            collider.isTrigger = true;

            // Kamera kontrolcüsünü bul
            _cameraController = FindFirstObjectByType<DynamicCameraController>();

            if (overrideCamera != null)
            {
                _originalPriority = overrideCamera.Priority.Value;
                overrideCamera.Priority = 0; // Başlangıçta devre dışı
            }

            _lastPreset = cameraPreset;
        }

        private void Update()
        {
            // Live preview: Inspector'dan değişiklik yapılınca anlık güncelle
            if (livePreview && _isPlayerInside && overrideCamera == null && _cameraController != null)
            {
                // Preset değişti mi kontrol et
                if (!PresetEquals(_lastPreset, cameraPreset))
                {
                    _lastPreset = cameraPreset;
                    _cameraController.SetCameraPreset(cameraPreset, 0.3f); // Hızlı geçiş
                }
            }
        }

        private bool PresetEquals(CameraPreset a, CameraPreset b)
        {
            return Mathf.Approximately(a.distance, b.distance) &&
                   Mathf.Approximately(a.height, b.height) &&
                   Mathf.Approximately(a.fieldOfView, b.fieldOfView) &&
                   Mathf.Approximately(a.lookIntensity, b.lookIntensity) &&
                   Mathf.Approximately(a.maxVerticalAngle, b.maxVerticalAngle) &&
                   Mathf.Approximately(a.lookAheadDistance, b.lookAheadDistance) &&
                   Mathf.Approximately(a.verticalLookAhead, b.verticalLookAhead) &&
                   Mathf.Approximately(a.followSmoothTime, b.followSmoothTime) &&
                   Mathf.Approximately(a.verticalSmoothTime, b.verticalSmoothTime) &&
                   a.offset == b.offset &&
                   a.rotationOffset == b.rotationOffset;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (isOneShot && _hasTriggered) return;

            _hasTriggered = true;
            _isPlayerInside = true;
            OnPlayerEnterZone();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            _isPlayerInside = false;
            
            if (!returnToDefaultOnExit) return;

            OnPlayerExitZone();
        }

        private void OnPlayerEnterZone()
        {
            Debug.Log($"[CameraZone] Player entered: {zoneName}");

            // Override kamera varsa aktif et
            if (overrideCamera != null)
            {
                overrideCamera.Priority = cameraPriority;
            }
            // Yoksa DynamicCameraController'a preset uygula
            else if (_cameraController != null)
            {
                _cameraController.SetCameraPreset(cameraPreset, enterTransitionTime);
            }
        }

        private void OnPlayerExitZone()
        {
            Debug.Log($"[CameraZone] Player exited: {zoneName}");

            // Override kamera varsa devre dışı bırak
            if (overrideCamera != null)
            {
                overrideCamera.Priority = 0;
            }
            // Yoksa varsayılana dön
            else if (_cameraController != null)
            {
                _cameraController.ResetToDefault(exitTransitionTime);
            }
        }

        /// <summary>
        /// Zone'u resetler (one-shot için).
        /// </summary>
        public void ResetZone()
        {
            _hasTriggered = false;
        }

        private void OnDrawGizmos()
        {
            var collider = GetComponent<Collider>();
            if (collider == null) return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            
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
            // Zone ismini göster
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, zoneName);
            #endif
        }
    }
}
