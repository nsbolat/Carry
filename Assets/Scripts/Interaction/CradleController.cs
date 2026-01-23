using UnityEngine;
using System;

namespace Sisifos.Interaction
{
    /// <summary>
    /// Beşik sallanma ve düşme mekaniğini yönetir.
    /// A tuşu sola, D tuşu sağa sallar. Sağ tarafta belirli açıya ulaşınca sağa devrilir.
    /// </summary>
    public class CradleController : MonoBehaviour
    {
        [Header("Rocking Settings")]
        [Tooltip("Her input'ta uygulanan tork kuvveti")]
        [SerializeField] private float rockingTorque = 15f;
        
        [Tooltip("Momentum kaybı (damping) oranı")]
        [SerializeField] private float dampingFactor = 0.98f;
        
        [Tooltip("Maksimum sallanma açısı (derece) - sol taraf")]
        [SerializeField] private float maxRockAngleLeft = 45f;
        
        [Tooltip("Devrilme için gereken açı (derece) - sağ tarafta")]
        [SerializeField] private float fallAngleThreshold = 35f;

        [Header("Fall Settings")]
        [Tooltip("Devrilme animasyon süresi")]
        [SerializeField] private float fallDuration = 0.8f;
        
        [Tooltip("Devrilme hedef açısı (derece)")]
        [SerializeField] private float fallTargetAngle = 90f;
        
        [Tooltip("Devrilirken beşiğin hareket edeceği pozisyon offset'i (yere gömülmeyi önler)")]
        [SerializeField] private Vector3 fallPositionOffset = new Vector3(0.5f, 0.3f, 0f);
        
        [Tooltip("Karakter çıkış animasyonu süresi")]
        [SerializeField] private float characterExitDuration = 1.5f;

        [Header("References")]
        [Tooltip("Beşik içindeki karakter (devrilince serbest bırakılacak)")]
        [SerializeField] private Transform playerInCradle;
        
        [Tooltip("Karakter beşikten çıktığında gideceği global pozisyon (sahnede boş bir GameObject)")]
        [SerializeField] private Transform exitPoint;
        
        [Tooltip("CharacterEvolutionManager - beşik düşünce aktif edilecek")]
        [SerializeField] private Sisifos.Player.CharacterEvolutionManager evolutionManager;

        [Header("Animation")]
        [Tooltip("Beşikten düşme animasyonu trigger ismi")]
        [SerializeField] private string fallAnimationTrigger = "CradleFall";
        
        [Tooltip("Düşme animasyonunun süresi (saniye)")]
        [SerializeField] private float fallAnimationDuration = 2f;
        
        [Tooltip("Animasyon bittikten sonra controller'un aktif olması için ek bekleme süresi (saniye)")]
        [SerializeField] private float controllerActivationDelay = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip rockingSound;
        [SerializeField] private AudioClip fallSound;
        
        [Tooltip("A tuşu (sola sallanma) için pitch değeri")]
        [SerializeField] private float leftRockPitch = 0.9f;
        
        [Tooltip("D tuşu (sağa sallanma) için pitch değeri")]
        [SerializeField] private float rightRockPitch = 1.1f;

        // Events
        public event Action OnCradleFallen;
        public event Action OnRockingStarted;
        /// <summary>
        /// Karakter çıkış animasyonu tamamlandığında tetiklenir - controller bu event'ten sonra aktif olmalı
        /// </summary>
        public event Action OnCharacterExitComplete;

        // State
        private float _currentAngle = 0f;
        private float _angularVelocity = 0f;
        private float _rockingInput = 0f;
        private bool _isRockingEnabled = false;
        private bool _hasFallen = false;
        private bool _isFalling = false;
        
        // Initial state
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Rigidbody _rigidbody;

        #region Properties
        public bool HasFallen => _hasFallen;
        public bool IsRockingEnabled => _isRockingEnabled;
        public float CurrentAngle => _currentAngle;
        #endregion

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            
            // Başlangıçta rigidbody kinematik
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            
            // Developer mode'da karakteri göster, normal modda gizle
            if (Core.GameStateManager.Instance != null && Core.GameStateManager.Instance.IsDeveloperMode)
            {
                SetCharacterVisible(true);
                Debug.Log("[CradleController] Developer Mode - Karakter görünür başlatıldı");
            }
            else
            {
                SetCharacterVisible(false);
            }
        }

        private void Update()
        {
            if (!_isRockingEnabled || _hasFallen || _isFalling) return;

            UpdateRocking();
            CheckForFall();
        }

        #region Public Methods
        /// <summary>
        /// Beşik sallanmasını aktif eder.
        /// </summary>
        public void EnableRocking()
        {
            if (_hasFallen) return;
            
            _isRockingEnabled = true;
            OnRockingStarted?.Invoke();
            Debug.Log("[CradleController] Rocking enabled - A/D tuşlarıyla beşiği sallayın!");
        }

        /// <summary>
        /// Beşik sallanmasını devre dışı bırakır.
        /// </summary>
        public void DisableRocking()
        {
            _isRockingEnabled = false;
        }

        /// <summary>
        /// Sallama input'unu ayarlar (-1 sol, +1 sağ).
        /// </summary>
        public void SetRockingInput(float input)
        {
            _rockingInput = Mathf.Clamp(input, -1f, 1f);
        }

        /// <summary>
        /// Beşiği başlangıç durumuna sıfırlar.
        /// </summary>
        public void ResetCradle()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            _currentAngle = 0f;
            _angularVelocity = 0f;
            _hasFallen = false;
            _isFalling = false;
            _isRockingEnabled = false;
            
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            
            // Karakteri gizle
            SetCharacterVisible(false);
        }

        /// <summary>
        /// Karakteri görünür veya görünmez yapar
        /// </summary>
        private void SetCharacterVisible(bool visible)
        {
            if (playerInCradle == null) return;
            
            // Tüm renderer'ları aç/kapat
            Renderer[] renderers = playerInCradle.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
            
            Debug.Log($"[CradleController] Karakter {(visible ? "GÖRÜNÜR" : "GİZLİ")}");
        }
        #endregion

        #region Private Methods
        private void UpdateRocking()
        {
            // Input'tan tork uygula
            // A tuşu (_rockingInput < 0) = sola sallanma (negatif açı)
            // D tuşu (_rockingInput > 0) = sağa sallanma (pozitif açı)
            if (Mathf.Abs(_rockingInput) > 0.1f)
            {
                // Sallanma yönünde kuvvet uygula
                _angularVelocity += _rockingInput * rockingTorque * Time.deltaTime;
                
                // Sallama sesi - yöne göre farklı pitch
                if (audioSource != null && rockingSound != null && !audioSource.isPlaying)
                {
                    // A tuşu (sol) = düşük pitch, D tuşu (sağ) = yüksek pitch
                    audioSource.pitch = _rockingInput < 0 ? leftRockPitch : rightRockPitch;
                    audioSource.PlayOneShot(rockingSound, 0.3f);
                }
            }

            // Damping (yavaşlama)
            _angularVelocity *= dampingFactor;

            // "Yerçekimi" efekti - beşik merkeze dönmeye çalışır
            float restoreForce = -_currentAngle * 5f * Time.deltaTime;
            _angularVelocity += restoreForce;

            // Açıyı güncelle
            _currentAngle += _angularVelocity * Time.deltaTime * 60f;

            // Sol tarafta maksimum açıyı, sağ tarafta devrilme açısını kontrol et
            // Negatif açı = sol, Pozitif açı = sağ
            _currentAngle = Mathf.Clamp(_currentAngle, -maxRockAngleLeft, fallAngleThreshold + 10f);

            // Rotasyonu uygula (Z ekseni etrafında)
            // Pozitif açı = sağa eğilmek (saat yönünde dönüş = -Z)
            transform.rotation = _initialRotation * Quaternion.Euler(0f, 0f, -_currentAngle);
        }

        private void CheckForFall()
        {
            // SADECE sağ tarafa devrilir (pozitif açı)
            // D tuşuyla sağa yeterince sallandığında devrilir
            if (_currentAngle >= fallAngleThreshold)
            {
                StartCoroutine(FallSequence());
            }
        }

        private System.Collections.IEnumerator FallSequence()
        {
            _isFalling = true;
            _isRockingEnabled = false;
            
            Debug.Log("[CradleController] Beşik sağa devrildi!");

            // Devrilme sesi
            if (audioSource != null && fallSound != null)
            {
                audioSource.PlayOneShot(fallSound);
            }
            // Karakter görünürlüğü - evolutionManager varsa o yönetsin, yoksa manuel aç
            if (evolutionManager != null)
            {
                // CharacterEvolutionManager'ı başlat - bu tüm modelleri ve renderer'ları açar
                evolutionManager.InitializeFirstStage();
                Debug.Log("[CradleController] EvolutionManager ile karakter aktif edildi");
            }
            else
            {
                // EvolutionManager yoksa manuel olarak renderer'ları aç
                SetCharacterVisible(true);
                Debug.Log("[CradleController] Manuel olarak karakter görünür yapıldı");
            }

            // Karakteri hemen beşikten ayır ve çıkış animasyonunu PARALEL başlat
            if (playerInCradle != null)
            {
                playerInCradle.SetParent(null);
                StartCoroutine(CharacterExitSequence());
            }

            // Başlangıç değerleri - fallTargetAngle ayarlanabilir
            float targetAngle = fallTargetAngle;
            float elapsed = 0f;
            float startAngle = _currentAngle;
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = _initialPosition + fallPositionOffset;

            // Devrilme animasyonu - açı ve pozisyon birlikte değişir
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fallDuration;
                
                // Ease-out curve (hızlı başlayıp yavaşla - yerçekimi hissi)
                float easeT = 1f - (1f - t) * (1f - t);
                
                // Açıyı güncelle
                _currentAngle = Mathf.Lerp(startAngle, targetAngle, easeT);
                transform.rotation = _initialRotation * Quaternion.Euler(0f, 0f, -_currentAngle);
                
                // Pozisyonu güncelle (yere gömülmeyi önle)
                transform.position = Vector3.Lerp(startPosition, targetPosition, easeT);
                
                yield return null;
            }

            // Final değerleri
            _currentAngle = targetAngle;
            transform.rotation = _initialRotation * Quaternion.Euler(0f, 0f, -_currentAngle);
            transform.position = targetPosition;

            _hasFallen = true;
            _isFalling = false;

            OnCradleFallen?.Invoke();
            Debug.Log("[CradleController] Beşik tamamen devrildi!");
        }

        /// <summary>
        /// Karakterin beşikten smooth çıkış animasyonu
        /// </summary>
        private System.Collections.IEnumerator CharacterExitSequence()
        {
            if (playerInCradle == null) yield break;
            
            // Karakteri beşikten ayır
            playerInCradle.SetParent(null);
            
            // Animator'u bul ve düşme animasyonunu tetikle
            Animator playerAnimator = playerInCradle.GetComponentInChildren<Animator>();
            if (playerAnimator != null && !string.IsNullOrEmpty(fallAnimationTrigger))
            {
                playerAnimator.SetTrigger(fallAnimationTrigger);
                Debug.Log($"[CradleController] '{fallAnimationTrigger}' animasyonu tetiklendi");
            }
            
            // Başlangıç ve hedef pozisyonlar
            Vector3 startPos = playerInCradle.position;
            
            // Global exit point kullan (beşiğin rotasyonundan bağımsız)
            Vector3 exitPos = exitPoint != null ? exitPoint.position : _initialPosition + new Vector3(2f, 0f, 0f);
            
            // Karakter exit point'e hareket ediyor
            float elapsed = 0f;
            
            // Smooth hareket animasyonu - sadece POZİSYON değişir, ROTASYON değişmez
            while (elapsed < characterExitDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / characterExitDuration;
                
                // Ease-in-out curve (yumuşak başlayıp yumuşak bitir)
                float smoothT = t * t * (3f - 2f * t);
                
                // Sadece pozisyonu güncelle - rotasyona dokunma
                playerInCradle.position = Vector3.Lerp(startPos, exitPos, smoothT);
                
                yield return null;
            }
            
            // Final pozisyon - KARAKTER EXIT POINT'E VARDI (rotasyon olduğu gibi kalır)
            
            Debug.Log("[CradleController] Karakter exit point'e vardı! Controller timer başlıyor...");
            
            // TIMER BURADA BAŞLIYOR - karakter exit point'e vardıktan sonra
            if (controllerActivationDelay > 0f)
            {
                yield return new WaitForSeconds(controllerActivationDelay);
            }
            
            Debug.Log("[CradleController] Timer bitti - Controller aktif ediliyor!");
            
            // Controller'u aktif etmek için event tetikle
            OnCharacterExitComplete?.Invoke();
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            // Devrilme eşik açısını göster (sadece sağ taraf)
            Vector3 center = transform.position;
            
            // Sağ devrilme eşiği (kırmızı)
            Gizmos.color = Color.red;
            Vector3 rightThreshold = Quaternion.Euler(0, 0, -fallAngleThreshold) * Vector3.up * 2f;
            Gizmos.DrawLine(center, center + rightThreshold);
            
            // Sol maksimum açı (sarı)
            Gizmos.color = Color.yellow;
            Vector3 leftMax = Quaternion.Euler(0, 0, maxRockAngleLeft) * Vector3.up * 1.5f;
            Gizmos.DrawLine(center, center + leftMax);
            
            // Global çıkış pozisyonu (yeşil)
            if (exitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(exitPoint.position, 0.3f);
                Gizmos.DrawLine(center, exitPoint.position);
            }
        }
        #endregion
    }
}
