using UnityEngine;
using Unity.Cinemachine;

namespace Sisifos.Camera
{
    /// <summary>
    /// Journey tarzı dinamik kamera kontrolcüsü.
    /// Side-scroller görünümü korurken oyuncu hızına ve yönüne göre 
    /// kamera framing'ini dinamik olarak ayarlar.
    /// </summary>
    public class DynamicCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform player;
        [SerializeField] private CinemachineCamera virtualCamera;

        [Header("Side-Scroller Settings")]
        [SerializeField] private float cameraHeight = 3f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, 0f);

        [Header("Dynamic Framing")]
        [SerializeField] private float lookAheadDistance = 3f;
        [SerializeField] private float lookAheadSmoothTime = 0.5f;
        [SerializeField] private float verticalLookAhead = 1.5f;

        [Header("Distance-Based Zoom")]
        [SerializeField] private bool enableSpeedZoom = true;
        [SerializeField] private float idleDistance = 12f;
        [SerializeField] private float walkDistance = 15f;
        [SerializeField] private float runDistance = 20f;
        [SerializeField] private float zoomSmoothTime = 1f;

        [Header("FOV Control")]
        [SerializeField] private bool enableFOVControl = true;
        [SerializeField] private float idleFOV = 40f;
        [SerializeField] private float walkFOV = 45f;
        [SerializeField] private float runFOV = 55f;
        [SerializeField] private float fovSmoothTime = 0.5f;

        [Header("Slope Response")]
        [SerializeField] private bool respondToSlope = true;
        [SerializeField] private float slopeHeightOffset = 2f;
        [SerializeField] private float slopeSmoothTime = 0.8f;

        [Header("Smooth Follow")]
        [SerializeField] private float followSmoothTime = 0.3f;
        [SerializeField] private float verticalSmoothTime = 0.5f;

        [Header("Soft Look Rotation")]
        [Tooltip("Kameranın oyuncuya doğru ne kadar döneceği (0-1)")]
        [SerializeField, Range(0f, 1f)] private float lookIntensity = 0.3f;
        [Tooltip("Rotasyon yumuşaklığı (düşük = daha yumuşak)")]
        [SerializeField] private float rotationSmoothSpeed = 3f;
        [Tooltip("Maksimum dikey rotasyon açısı")]
        [SerializeField] private float maxVerticalAngle = 15f;

        // Components
        private CinemachineFollow _followComponent;
        private Player.SlopeCharacterController _playerController;

        // State
        private Vector3 _currentLookAhead;
        private Vector3 _lookAheadVelocity;
        private float _currentDistance;
        private float _distanceVelocity;
        private float _currentHeightOffset;
        private float _heightVelocity;
        private Vector3 _smoothDampVelocity;
        private float _lastMoveDirection;
        private Quaternion _currentRotation;
        private Vector3 _smoothLookTarget;
        private float _currentFOV;
        private float _fovVelocity;
        private bool _firstFrame = true;

        private void Awake()
        {
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
                else
                {
                    Debug.LogError("DynamicCameraController: Player bulunamadı!");
                    return;
                }
            }

            _playerController = player.GetComponent<Player.SlopeCharacterController>();

            if (virtualCamera == null)
            {
                virtualCamera = GetComponent<CinemachineCamera>();
            }

            if (virtualCamera != null)
            {
                _followComponent = virtualCamera.GetComponent<CinemachineFollow>();
                virtualCamera.Target.TrackingTarget = player;
                
                // FollowOffset'i baştan minDistance ile ayarla
                if (_followComponent != null)
                {
                    _followComponent.FollowOffset = new Vector3(
                        cameraOffset.x,
                        cameraOffset.y + cameraHeight,
                        -idleDistance
                    );
                    
                    // Y ekseninde yumuşak takip için damping ayarla (zıplama için)
                    _followComponent.TrackerSettings.PositionDamping = new Vector3(
                        followSmoothTime,    // X damping
                        verticalSmoothTime,  // Y damping
                        followSmoothTime     // Z damping
                    );
                }
            }

            _currentDistance = idleDistance;
            _currentHeightOffset = 0f;
            _currentFOV = idleFOV;
            if (virtualCamera != null) virtualCamera.Lens.FieldOfView = _currentFOV;

            // Rotasyon başlangıç değerlerini hesapla ve ayarla
            if (player != null)
            {
                _smoothLookTarget = player.position + cameraOffset;
                
                // Hedef rotasyonu hesapla
                Vector3 directionToTarget = _smoothLookTarget - transform.position;
                if (directionToTarget.sqrMagnitude > 0.01f && lookIntensity > 0f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, lookIntensity);
                    
                    Vector3 euler = blendedRotation.eulerAngles;
                    if (euler.x > 180f) euler.x -= 360f;
                    if (euler.y > 180f) euler.y -= 360f;
                    euler.x = Mathf.Clamp(euler.x, -maxVerticalAngle, maxVerticalAngle);
                    euler.y = -euler.y;
                    euler.y = Mathf.Clamp(euler.y, -maxVerticalAngle, maxVerticalAngle);
                    
                    _currentRotation = Quaternion.Euler(euler);
                    transform.rotation = _currentRotation;
                }
            }
        }

        private void LateUpdate()
        {
            if (player == null || virtualCamera == null) return;

            // İlk frame'de rotasyonu direkt hedef değere snap et
            if (_firstFrame)
            {
                _firstFrame = false;
                SnapRotationToTarget();
            }

            UpdateLookAhead();
            UpdateSpeedZoom();
            UpdateFOV();
            UpdateSlopeResponse();
            ApplyCameraSettings();
            UpdateCameraRotation();
        }

        private void SnapRotationToTarget()
        {
            if (player == null) return;
            
            _smoothLookTarget = player.position + cameraOffset;
            Vector3 directionToTarget = _smoothLookTarget - transform.position;
            
            if (directionToTarget.sqrMagnitude > 0.01f && lookIntensity > 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, lookIntensity);
                
                Vector3 euler = blendedRotation.eulerAngles;
                if (euler.x > 180f) euler.x -= 360f;
                if (euler.y > 180f) euler.y -= 360f;
                euler.x = Mathf.Clamp(euler.x, -maxVerticalAngle, maxVerticalAngle);
                euler.y = -euler.y;
                euler.y = Mathf.Clamp(euler.y, -maxVerticalAngle, maxVerticalAngle);
                
                _currentRotation = Quaternion.Euler(euler);
                transform.rotation = _currentRotation;
            }
        }

        private void UpdateLookAhead()
        {
            if (_playerController == null) return;

            Vector3 velocity = _playerController.Velocity;
            float targetLookAheadX = 0f;
            float targetLookAheadY = 0f;

            // Yatay look-ahead (hareket yönüne göre)
            if (Mathf.Abs(velocity.x) > 0.1f)
            {
                _lastMoveDirection = Mathf.Sign(velocity.x);
            }
            targetLookAheadX = _lastMoveDirection * lookAheadDistance;

            // Dikey look-ahead (eğime göre)
            if (_playerController.CurrentSlopeAngle > 5f)
            {
                float slopeDirection = _playerController.GroundNormal.y < 1f 
                    ? Mathf.Sign(Vector3.Dot(_playerController.GroundNormal, Vector3.right) * _lastMoveDirection) 
                    : 0f;
                targetLookAheadY = slopeDirection * verticalLookAhead * (_playerController.CurrentSlopeAngle / 45f);
            }

            Vector3 targetLookAhead = new Vector3(targetLookAheadX, targetLookAheadY, 0f);
            _currentLookAhead = Vector3.SmoothDamp(_currentLookAhead, targetLookAhead, ref _lookAheadVelocity, lookAheadSmoothTime);
        }

        private void UpdateSpeedZoom()
        {
            if (!enableSpeedZoom || _playerController == null) return;

            float speed = _playerController.CurrentSpeed;
            float targetDistance;

            // Hıza göre hedef mesafe belirle
            if (speed < 0.1f)
            {
                targetDistance = idleDistance;
            }
            else if (speed < _playerController.WalkSpeed + 0.1f)
            {
                // Idle -> Walk arası interpolasyon
                float t = Mathf.Clamp01(speed / _playerController.WalkSpeed);
                targetDistance = Mathf.Lerp(idleDistance, walkDistance, t);
            }
            else
            {
                // Walk -> Run arası interpolasyon
                float runRange = _playerController.RunSpeed - _playerController.WalkSpeed;
                float t = Mathf.Clamp01((speed - _playerController.WalkSpeed) / runRange);
                targetDistance = Mathf.Lerp(walkDistance, runDistance, t);
            }

            _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDistance, ref _distanceVelocity, zoomSmoothTime);
        }

        private void UpdateFOV()
        {
            if (!enableFOVControl || _playerController == null || virtualCamera == null) return;

            float speed = _playerController.CurrentSpeed;
            float targetFOV;

            // Hıza göre hedef FOV belirle
            if (speed < 0.1f)
            {
                targetFOV = idleFOV;
            }
            else if (speed < _playerController.WalkSpeed + 0.1f)
            {
                // Idle -> Walk arası interpolasyon
                float t = Mathf.Clamp01(speed / _playerController.WalkSpeed);
                targetFOV = Mathf.Lerp(idleFOV, walkFOV, t);
            }
            else
            {
                // Walk -> Run arası interpolasyon
                float runRange = _playerController.RunSpeed - _playerController.WalkSpeed;
                float t = Mathf.Clamp01((speed - _playerController.WalkSpeed) / runRange);
                targetFOV = Mathf.Lerp(walkFOV, runFOV, t);
            }

            _currentFOV = Mathf.SmoothDamp(_currentFOV, targetFOV, ref _fovVelocity, fovSmoothTime);
            virtualCamera.Lens.FieldOfView = _currentFOV;
        }

        private void UpdateSlopeResponse()
        {
            if (!respondToSlope || _playerController == null) return;

            float targetHeightOffset = 0f;

            // Eğime göre kamera yüksekliği ayarla
            if (_playerController.CurrentSlopeAngle > 10f)
            {
                targetHeightOffset = slopeHeightOffset * (_playerController.CurrentSlopeAngle / 45f);
            }

            _currentHeightOffset = Mathf.SmoothDamp(_currentHeightOffset, targetHeightOffset, ref _heightVelocity, slopeSmoothTime);
        }

        private void ApplyCameraSettings()
        {
            if (_followComponent != null)
            {
                // Side-scroller için kamera pozisyonu: oyuncunun arkasında (Z ekseninde)
                Vector3 offset = new Vector3(
                    cameraOffset.x + _currentLookAhead.x,
                    cameraOffset.y + cameraHeight + _currentHeightOffset + _currentLookAhead.y,
                    -_currentDistance
                );
                
                _followComponent.FollowOffset = offset;
            }
        }

        private void UpdateCameraRotation()
        {
            if (lookIntensity <= 0f) return;

            // Oyuncunun pozisyonunu hedef al (look-ahead dahil)
            Vector3 lookTarget = player.position + cameraOffset + _currentLookAhead * 0.5f;
            
            // Smooth look target
            _smoothLookTarget = Vector3.Lerp(_smoothLookTarget, lookTarget, Time.deltaTime * rotationSmoothSpeed);

            // Kameradan hedefe yön
            Vector3 directionToTarget = _smoothLookTarget - transform.position;
            
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                // Hedef rotasyonu hesapla
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                
                // Side-scroller için varsayılan rotasyon (düz ileri bakış)
                Quaternion defaultRotation = Quaternion.identity;
                
                // Soft look - intensity'ye göre interpolasyon
                Quaternion blendedRotation = Quaternion.Slerp(defaultRotation, targetRotation, lookIntensity);
                
                // Açıları sınırla
                Vector3 euler = blendedRotation.eulerAngles;
                // Euler açılarını -180 / +180 aralığına çevir
                if (euler.x > 180f) euler.x -= 360f;
                if (euler.y > 180f) euler.y -= 360f;
                euler.x = Mathf.Clamp(euler.x, -maxVerticalAngle, maxVerticalAngle);
                euler.y = -euler.y; // Y rotasyonunu tersine çevir
                euler.y = Mathf.Clamp(euler.y, -maxVerticalAngle, maxVerticalAngle); // Y limiti
                blendedRotation = Quaternion.Euler(euler);
                
                // Smooth rotasyon uygula
                _currentRotation = Quaternion.Slerp(_currentRotation, blendedRotation, Time.deltaTime * rotationSmoothSpeed);
                
                transform.rotation = _currentRotation;
            }
        }

        #region Public Methods
        /// <summary>
        /// Kamera ayarlarını geçici olarak değiştirir (zone trigger'lar için).
        /// </summary>
        public void SetCameraPreset(CameraPreset preset, float transitionTime = 1f)
        {
            StartCoroutine(TransitionToPreset(preset, transitionTime));
        }

        /// <summary>
        /// Kamerayı varsayılan ayarlara döndürür.
        /// </summary>
        public void ResetToDefault(float transitionTime = 1f)
        {
            StartCoroutine(TransitionToDefault(transitionTime));
        }
        #endregion

        #region Coroutines
        private System.Collections.IEnumerator TransitionToPreset(CameraPreset preset, float duration)
        {
            float elapsed = 0f;
            float startDistance = _currentDistance;
            float startHeight = cameraHeight;
            Vector3 startOffset = cameraOffset;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // Smoothstep

                _currentDistance = Mathf.Lerp(startDistance, preset.distance, t);
                cameraHeight = Mathf.Lerp(startHeight, preset.height, t);
                cameraOffset = Vector3.Lerp(startOffset, preset.offset, t);

                yield return null;
            }
        }

        private System.Collections.IEnumerator TransitionToDefault(float duration)
        {
            // Varsayılan değerleri saklayın ve kullanın
            yield return TransitionToPreset(new CameraPreset
            {
                distance = 15f,
                height = 3f,
                offset = new Vector3(0f, 2f, 0f)
            }, duration);
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            if (player == null) return;

            // Kamera hedef pozisyonu
            Vector3 targetPos = player.position + cameraOffset + _currentLookAhead;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPos, 0.5f);

            // Look-ahead gösterimi
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(player.position, player.position + _currentLookAhead);
        }
    }

    /// <summary>
    /// Kamera preset'i için veri yapısı.
    /// </summary>
    [System.Serializable]
    public struct CameraPreset
    {
        public float distance;
        public float height;
        public Vector3 offset;
        public float fieldOfView;
    }
}
