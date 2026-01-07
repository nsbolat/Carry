using UnityEngine;
using System;
using System.Collections;

namespace Sisifos.Player
{
    /// <summary>
    /// Oyun başlangıcında karakterin sahneye giriş animasyonunu yönetir.
    /// Karakter sol taraftan ekrana yürüyerek girer.
    /// </summary>
    public class PlayerIntroController : MonoBehaviour
    {
        [Header("Intro Settings")]
        [Tooltip("Karakterin başlangıç pozisyonu (ekran dışı, sol taraf)")]
        [SerializeField] private Vector3 startOffset = new Vector3(-15f, 0f, 0f);
        
        [Tooltip("Karakterin hedef pozisyonu (sahne merkezi)")]
        [SerializeField] private Vector3 targetPosition = Vector3.zero;

        [Header("Movement")]
        [Tooltip("Yürüme hızı - SlopeCharacterController ile aynı olmalı")]
        [SerializeField] private float walkSpeed = 5f;
        
        [Tooltip("Yavaşlama mesafesi (hedefe yaklaşırken)")]
        [SerializeField] private float decelerationDistance = 2f;

        [Header("Camera Switch")]
        [Tooltip("Kamera geçişinin başlayacağı mesafe (hedefe kalan)")]
        [SerializeField] private float cameraSwitchDistance = 5f;

        [Header("References")]
        [SerializeField] private SlopeCharacterController characterController;
        [SerializeField] private Animator animator;

        // Events
        public event Action OnIntroComplete;
        public event Action OnCameraSwitchPoint;

        // State
        private Vector3 _originalPosition;
        private bool _introPlaying;
        private bool _introCompleted;
        private bool _cameraSwitchTriggered;
        private float _currentSpeed;

        // Animator parameter hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<SlopeCharacterController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            _originalPosition = transform.position;
            
            if (characterController != null)
            {
                walkSpeed = characterController.WalkSpeed;
            }
        }

        private void Start()
        {
            if (Core.GameStateManager.Instance != null &&
                Core.GameStateManager.Instance.CurrentState == Core.GameStateManager.GameState.MainMenu)
            {
                InitializeForMenu();
            }
        }

        public void InitializeForMenu()
        {
            transform.position = _originalPosition + startOffset;
            _currentSpeed = 0f;
            _cameraSwitchTriggered = false;
            
            if (characterController != null)
            {
                characterController.SetMoveInput(Vector2.zero);
            }
            
            transform.rotation = Quaternion.Euler(0, 90, 0);
        }

        public void StartIntro()
        {
            if (_introPlaying || _introCompleted) return;
            StartCoroutine(PlayIntroCoroutine());
        }

        private IEnumerator PlayIntroCoroutine()
        {
            _introPlaying = true;
            _cameraSwitchTriggered = false;

            Vector3 endPos = _originalPosition + targetPosition;
            
            if (animator != null)
            {
                animator.SetBool(IsGroundedHash, true);
            }

            while (true)
            {
                float remainingDistance = Mathf.Abs(transform.position.x - endPos.x);
                
                // Kamera geçiş noktası
                if (!_cameraSwitchTriggered && remainingDistance <= cameraSwitchDistance)
                {
                    _cameraSwitchTriggered = true;
                    OnCameraSwitchPoint?.Invoke();
                }
                
                // Hedefe vardık mı?
                if (remainingDistance < 0.05f)
                {
                    break;
                }

                // Hedefe yaklaşırken smooth yavaşla (ease-out curve)
                float targetSpeed = walkSpeed;
                if (remainingDistance < decelerationDistance)
                {
                    // Ease-out quadratic curve for smoother stop
                    float t = remainingDistance / decelerationDistance;
                    t = t * t; // Quadratic ease-out (daha yumuşak durma)
                    targetSpeed = Mathf.Lerp(0.1f, walkSpeed, t);
                }

                // Daha yumuşak hız geçişi
                float acceleration = remainingDistance < 1f ? walkSpeed * 1.5f : walkSpeed * 3f;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

                float dirX = endPos.x > transform.position.x ? 1f : -1f;
                
                Vector3 newPos = transform.position;
                newPos.x += dirX * _currentSpeed * Time.deltaTime;
                transform.position = newPos;

                if (animator != null)
                {
                    float runSpeed = characterController != null ? characterController.RunSpeed : walkSpeed * 2f;
                    float normalizedSpeed = _currentSpeed / runSpeed;
                    animator.SetFloat(SpeedHash, normalizedSpeed);
                }

                yield return null;
            }

            // Final pozisyonu ayarla
            Vector3 finalPos = transform.position;
            finalPos.x = endPos.x;
            transform.position = finalPos;
            
            // Animasyonu smooth olarak sıfırla
            if (animator != null)
            {
                float currentAnimSpeed = animator.GetFloat(SpeedHash);
                float fadeTime = 0.3f;
                float elapsed = 0f;
                
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeTime;
                    animator.SetFloat(SpeedHash, Mathf.Lerp(currentAnimSpeed, 0f, t));
                    yield return null;
                }
                
                animator.SetFloat(SpeedHash, 0f);
            }
            
            _currentSpeed = 0f;

            _introPlaying = false;
            _introCompleted = true;

            OnIntroComplete?.Invoke();
        }

        public void SkipIntro()
        {
            if (!_introPlaying) return;

            StopAllCoroutines();

            transform.position = _originalPosition + targetPosition;
            _currentSpeed = 0f;
            
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, 0f);
            }

            _introPlaying = false;
            _introCompleted = true;

            if (!_cameraSwitchTriggered)
            {
                OnCameraSwitchPoint?.Invoke();
            }
            OnIntroComplete?.Invoke();
        }

        public void ResetIntro()
        {
            StopAllCoroutines();
            _introPlaying = false;
            _introCompleted = false;
            _cameraSwitchTriggered = false;
            _currentSpeed = 0f;
            InitializeForMenu();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 startPos = (Application.isPlaying ? _originalPosition : transform.position) + startOffset;
            Vector3 endPos = (Application.isPlaying ? _originalPosition : transform.position) + targetPosition;
            
            // Start position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startPos, 0.5f);
            
            // End position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(endPos, 0.5f);
            
            // Camera switch zone
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(endPos.x - cameraSwitchDistance, endPos.y, endPos.z), 0.3f);
            
            // Deceleration zone
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(endPos, decelerationDistance);

            Gizmos.DrawLine(startPos, endPos);
        }
    }
}
