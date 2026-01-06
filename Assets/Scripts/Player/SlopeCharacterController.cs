using UnityEngine;

namespace Sisifos.Player
{
    /// <summary>
    /// 3D Side-Scroller karakter kontrolcüsü.
    /// Terrain eğimine göre rotasyon yapar ve smooth hareket sağlar.
    /// Journey tarzı çevresel anlatı oyunları için tasarlanmıştır.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SlopeCharacterController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 15f;
        [SerializeField] private float turnSmoothTime = 0.05f;

        [Header("Carry Weight")]
        [Tooltip("Her kutu başına hız azalma yüzdesi (0.05 = %5)")]
        [SerializeField] private float speedReductionPerBox = 0.05f;
        [Tooltip("Minimum hız çarpanı (örn: 0.5 = en fazla %50 yavaşlama)")]
        [SerializeField] private float minSpeedMultiplier = 0.5f;
        [Tooltip("Gerginliğin hıza maksimum etkisi (0.3 = en fazla %30 ek yavaşlama)")]
        [SerializeField] private float maxTensionEffect = 0.3f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float gravity = -15f;
        [Tooltip("Düşerken yerçekimi çarpanı - daha hızlı iniş için")]
        [SerializeField] private float fallMultiplier = 2f;
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Slope Settings")]
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float slopeRotationSpeed = 8f;
        [SerializeField] private float groundRayLength = 1.5f;
        [SerializeField] private float groundRayOffset = 0.5f;

        [Header("Side-Scroller Settings")]
        [SerializeField] private float forwardAxis = 0f; // Z ekseni değeri (sabit tutulacak)
        [SerializeField] private bool lockZAxis = true;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string groundedParameter = "IsGrounded";
        [SerializeField] private string jumpParameter = "Jump";

        // Components
        private CharacterController _characterController;
        
        // Movement state
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private float _currentSpeed;
        private float _targetSpeed;
        private float _turnSmoothVelocity;
        private bool _isGrounded;
        private bool _isRunning;
        private bool _isJumping;
        
        // Slope state
        private Vector3 _groundNormal = Vector3.up;
        private float _currentSlopeAngle;
        private Quaternion _targetRotation;
        
        // Facing direction (side-scroller için)
        private bool _facingRight = true;
        
        // Input
        private Vector2 _inputMove;
        private bool _inputJump;
        
        // Carry weight
        private int _carriedBoxCount = 0;
        private float _speedMultiplier = 1f;
        private float _ropeTension = 0f; // 0-1 arası halat gerginliği

        #region Properties
        public bool IsGrounded => _isGrounded;
        public bool IsRunning => _isRunning;
        public bool IsJumping => _isJumping;
        public float CurrentSpeed => _currentSpeed;
        public float CurrentSlopeAngle => _currentSlopeAngle;
        public Vector3 Velocity => _velocity;
        public Vector3 GroundNormal => _groundNormal;
        public float SpeedMultiplier => _speedMultiplier;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        #endregion

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            _targetRotation = transform.rotation;
            forwardAxis = transform.position.z;
        }

        private void Update()
        {
            CheckGround();
            UpdateSlopeRotation();
            HandleMovement();
            HandleJump();
            ApplyGravity();
            MoveCharacter();
            UpdateAnimator();
            
            // Z eksenini kilitle (side-scroller için)
            if (lockZAxis)
            {
                LockZPosition();
            }
        }

        #region Public Input Methods
        public void SetMoveInput(Vector2 input)
        {
            _inputMove = input;
        }

        public void SetJumpInput(bool jump)
        {
            _inputJump = jump;
        }

        public void SetRunning(bool running)
        {
            _isRunning = running;
        }

        /// <summary>
        /// Taşınan kutu sayısını ayarlar ve hız çarpanını günceller
        /// </summary>
        public void SetCarriedWeight(int boxCount)
        {
            _carriedBoxCount = boxCount;
            // Hız çarpanı hesapla: her kutu için belirli yüzde azalma
            _speedMultiplier = Mathf.Max(minSpeedMultiplier, 1f - (boxCount * speedReductionPerBox));
        }

        /// <summary>
        /// Halat gerginliğini ayarlar (0-1 arası). 
        /// Yüksek gerginlik = kutular zorlanıyor = karakter yavaşlar
        /// </summary>
        public void SetRopeTension(float tension)
        {
            _ropeTension = Mathf.Clamp01(tension);
        }
        #endregion

        #region Ground & Slope Detection
        private void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * groundRayOffset;
            
            // Her zaman raycast ile ground check yap (daha güvenilir)
            bool raycastGrounded = false;
            RaycastHit groundHit;
            
            if (groundLayer.value != 0)
            {
                // LayerMask ayarlandıysa sadece o layer'a bak
                raycastGrounded = Physics.SphereCast(
                    origin,
                    _characterController.radius * 0.9f,
                    Vector3.down,
                    out groundHit,
                    groundCheckDistance + groundRayOffset,
                    groundLayer,
                    QueryTriggerInteraction.Ignore
                );
            }
            else
            {
                // Ground Layer yoksa tüm collider'lara bak
                raycastGrounded = Physics.SphereCast(
                    origin,
                    _characterController.radius * 0.9f,
                    Vector3.down,
                    out groundHit,
                    groundCheckDistance + groundRayOffset,
                    ~0, // Tüm layer'lar
                    QueryTriggerInteraction.Ignore
                );
            }
            
            // CharacterController check ile kombine et
            _isGrounded = raycastGrounded || _characterController.isGrounded;
            
            // DEBUG: Yere inme kontrolü
            if (_isJumping && _isGrounded)
            {
                Debug.Log($"Landing detected! raycastGrounded={raycastGrounded}, ccGrounded={_characterController.isGrounded}");
            }

            // Eğim algılama
            if (_isGrounded)
            {
                // Yere indiğinde jump'ı resetle
                _isJumping = false;
                
                if (raycastGrounded)
                {
                    _groundNormal = groundHit.normal;
                    _currentSlopeAngle = Vector3.Angle(Vector3.up, _groundNormal);
                }
                else if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit slopeHit, groundRayLength))
                {
                    _groundNormal = slopeHit.normal;
                    _currentSlopeAngle = Vector3.Angle(Vector3.up, _groundNormal);
                }
                else
                {
                    _groundNormal = Vector3.up;
                    _currentSlopeAngle = 0f;
                }
            }
            else
            {
                _groundNormal = Vector3.up;
                _currentSlopeAngle = 0f;
            }
        }



        private void UpdateSlopeRotation()
        {
            // Hareket yönüne göre facing direction güncelle
            if (_inputMove.x > 0.1f)
            {
                _facingRight = true;
            }
            else if (_inputMove.x < -0.1f)
            {
                _facingRight = false;
            }
            // Hareket yoksa son yöne bakmaya devam et

            Vector3 forwardOnSlope;
            
            if (!_isGrounded)
            {
                // Havadayken sadece yatay rotasyon (eğim yok)
                forwardOnSlope = _facingRight ? Vector3.right : Vector3.left;
                _targetRotation = Quaternion.LookRotation(forwardOnSlope, Vector3.up);
            }
            else
            {
                // Yerdeyken eğime göre rotasyon hesapla
                if (_facingRight)
                {
                    forwardOnSlope = Vector3.Cross(_groundNormal, Vector3.forward);
                }
                else
                {
                    forwardOnSlope = Vector3.Cross(Vector3.forward, _groundNormal);
                }

                if (forwardOnSlope != Vector3.zero)
                {
                    _targetRotation = Quaternion.LookRotation(forwardOnSlope.normalized, _groundNormal);
                }
            }

            // Smooth rotasyon uygula
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _targetRotation,
                Time.deltaTime * slopeRotationSpeed
            );
        }
        #endregion

        #region Movement
        private Vector3 _lastMoveDirection = Vector3.right; // Son hareket yönü
        
        private void HandleMovement()
        {
            // Side-scroller için sadece X ekseninde hareket (sol-sağ)
            float horizontal = _inputMove.x;
            
            // Hedef hız hesapla (ağırlık çarpanı + gerginlik ile)
            float baseSpeed = _isRunning ? runSpeed : walkSpeed;
            
            // Gerginlik faktörü: 0 gerginlik = 1.0 çarpan, maksimum gerginlik = (1-maxTensionEffect) çarpan
            float tensionMultiplier = 1f - (_ropeTension * maxTensionEffect);
            
            _targetSpeed = Mathf.Abs(horizontal) > 0.1f 
                ? baseSpeed * _speedMultiplier * tensionMultiplier
                : 0f;

            // Smooth hız geçişi
            float accelerationRate = _targetSpeed > _currentSpeed ? acceleration : deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, accelerationRate * Time.deltaTime);

            // Hareket yönü - input varsa güncelle, yoksa son yönü kullan
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                _moveDirection = new Vector3(horizontal, 0f, 0f).normalized;
                _lastMoveDirection = _moveDirection; // Son yönü sakla
            }
            else
            {
                // Input yoksa son yönde hareket et (yavaşlarken kayma efekti)
                _moveDirection = _lastMoveDirection;
            }

            // Eğimde hareket - eğime paralel hareket et
            if (_isGrounded && _currentSlopeAngle > 0f && _currentSlopeAngle <= maxSlopeAngle)
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(_moveDirection, _groundNormal).normalized;
                _moveDirection = slopeDirection;
            }

            // Karakter yönünü ayarla (side-scroller için sadece sağa veya sola bak)
            if (horizontal > 0.1f)
            {
                float targetAngle = 90f; // Sağa bak
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
                // Y rotasyonunu sadece 90 veya -90 yap
            }
            else if (horizontal < -0.1f)
            {
                float targetAngle = -90f; // Sola bak
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
            }
        }

        private void HandleJump()
        {
            if (_inputJump)
            {
                if (_isGrounded)
                {
                    _velocity.y = jumpForce;
                    _isJumping = true;
                    
                    if (animator != null)
                    {
                        animator.SetTrigger(jumpParameter);
                    }
                }
                // Zıplama inputunu her zaman resetle (havadayken de)
                // Bu sayede yere indiğinde otomatik zıplama olmaz
                _inputJump = false;
            }
        }

        private void ApplyGravity()
        {
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Yere yapışık tut
                _isJumping = false;
            }
            else
            {
                // Düşerken daha hızlı yerçekimi (daha doğal his)
                float currentGravity = gravity;
                if (_velocity.y < 0)
                {
                    currentGravity *= fallMultiplier;
                }
                _velocity.y += currentGravity * Time.deltaTime;
            }
        }

        private void MoveCharacter()
        {
            Vector3 move = _moveDirection * _currentSpeed + Vector3.up * _velocity.y;
            _characterController.Move(move * Time.deltaTime);
        }

        private void LockZPosition()
        {
            Vector3 pos = transform.position;
            pos.z = Mathf.Lerp(pos.z, forwardAxis, Time.deltaTime * 10f);
            transform.position = pos;
        }
        #endregion

        #region Animation
        private void UpdateAnimator()
        {
            if (animator == null) return;

            // Animasyon hızı sadece karakterin gerçek hızına bağlı
            // _currentSpeed zaten kutu taşırken yavaşlıyor (speedMultiplier etkisi)
            float normalizedSpeed = _currentSpeed / runSpeed;
            animator.SetFloat(speedParameter, normalizedSpeed);
            animator.SetBool(groundedParameter, _isGrounded);
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            // CharacterController referansı için
            CharacterController cc = GetComponent<CharacterController>();
            float radius = cc != null ? cc.radius * 0.9f : 0.3f;
            
            // Ground check visualization - SphereCast görselleştirmesi
            Vector3 origin = transform.position + Vector3.up * groundRayOffset;
            Vector3 endPoint = origin + Vector3.down * (groundCheckDistance + groundRayOffset);
            
            // SphereCast başlangıç noktası
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin, radius);
            
            // SphereCast bitiş noktası
            Gizmos.DrawWireSphere(endPoint, radius);
            
            // Bağlantı çizgileri
            Gizmos.DrawLine(origin + Vector3.right * radius, endPoint + Vector3.right * radius);
            Gizmos.DrawLine(origin - Vector3.right * radius, endPoint - Vector3.right * radius);
            Gizmos.DrawLine(origin + Vector3.forward * radius, endPoint + Vector3.forward * radius);
            Gizmos.DrawLine(origin - Vector3.forward * radius, endPoint - Vector3.forward * radius);
            
            // Merkez çizgisi
            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, endPoint);

            // Slope raycast visualization
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundRayLength);

            // Slope normal visualization
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _groundNormal * 2f);

            // Movement direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up, _moveDirection * 2f);
            
            // Jump state indicator
            if (_isJumping)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
            }
        }
        #endregion
    }
}
