using UnityEngine;
using System;
using Sisifos.Interaction;

namespace Sisifos.Player
{
    /// <summary>
    /// Oyun başlangıcında beşik mekaniğini yönetir.
    /// Karakter beşikte başlar, A-D tuşlarıyla beşik sallanır ve düşer.
    /// </summary>
    public class PlayerIntroController : MonoBehaviour
    {
        [Header("Cradle Settings")]
        [Tooltip("Beşik kontrolcüsü")]
        [SerializeField] private CradleController cradleController;
        
        [Tooltip("Beşikten düştükten sonra karakterin spawn pozisyonu offset'i")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(1f, 0f, 0f);

        [Header("References")]
        [SerializeField] private SlopeCharacterController characterController;
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private Animator animator;

        [Header("Legacy - Backward Compatibility")]
        [Tooltip("Eski intro sistemi için (artık kullanılmıyor)")]
        [SerializeField] private Vector3 startOffset = new Vector3(-15f, 0f, 0f);
        [SerializeField] private Vector3 targetPosition = Vector3.zero;

        // Events
        public event Action OnIntroComplete;
        public event Action OnCameraSwitchPoint;

        // State
        private Vector3 _originalPosition;
        private bool _introPlaying;
        private bool _introCompleted;
        private bool _cameraSwitchTriggered;

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<SlopeCharacterController>();

            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            _originalPosition = transform.position;
        }

        private void Start()
        {
            if (Core.GameStateManager.Instance != null &&
                Core.GameStateManager.Instance.CurrentState == Core.GameStateManager.GameState.MainMenu)
            {
                InitializeForMenu();
            }

            // Beşik event'lerine abone ol
            if (cradleController != null)
            {
                cradleController.OnCradleFallen += HandleCradleFallen;
                cradleController.OnCharacterExitComplete += HandleCharacterExitComplete;
            }
        }

        private void OnDestroy()
        {
            if (cradleController != null)
            {
                cradleController.OnCradleFallen -= HandleCradleFallen;
                cradleController.OnCharacterExitComplete -= HandleCharacterExitComplete;
            }
        }

        /// <summary>
        /// Menü durumu için başlangıç ayarları.
        /// </summary>
        public void InitializeForMenu()
        {
            _cameraSwitchTriggered = false;
            _introCompleted = false;
            _introPlaying = false;

            // Karakteri beşiğe yerleştir (beşik varsa)
            if (cradleController != null)
            {
                // Karakter beşik içinde olmalı - Unity Editor'da parent olarak ayarlanmalı
                cradleController.ResetCradle();
            }

            // Karakter kontrolcüsünü devre dışı bırak
            if (characterController != null)
            {
                characterController.SetMoveInput(Vector2.zero);
            }
        }

        /// <summary>
        /// Intro'yu başlatır - beşik modunu aktif eder.
        /// </summary>
        public void StartIntro()
        {
            if (_introPlaying || _introCompleted) return;
            
            _introPlaying = true;
            _cameraSwitchTriggered = false;

            Debug.Log("[PlayerIntroController] Intro başlatıldı - Beşik modu (kamera henüz değişmedi)");

            // NOT: Kamera geçişi burada yapılmıyor!
            // Karakter yere düştüğünde (HandleCradleFallen) kamera geçecek

            // Beşik varsa sallanmayı aktif et
            if (cradleController != null)
            {
                cradleController.EnableRocking();
                
                // Input handler'a beşik modunu bildir
                if (inputHandler != null)
                {
                    inputHandler.SetCradleMode(cradleController);
                }
            }
            else
            {
                Debug.LogWarning("[PlayerIntroController] CradleController atanmamış! Intro atlaniyor...");
                // Beşik yoksa direkt tamamla
                CompleteIntro();
            }
        }

        /// <summary>
        /// Beşik düştüğünde çağrılır - kamera geçişi ve input'u devre dışı tut
        /// </summary>
        private void HandleCradleFallen()
        {
            Debug.Log("[PlayerIntroController] Beşik düştü - Kamera geçişi ve animasyon!");
            
            // KAMERA GEÇİŞİ - karakter yere düştüğünde
            if (!_cameraSwitchTriggered)
            {
                _cameraSwitchTriggered = true;
                OnCameraSwitchPoint?.Invoke();
            }
            
            // Beşik modundan çık
            if (inputHandler != null)
            {
                inputHandler.ExitCradleMode();
                // Input'u KAPALI tut - timer bitene kadar hareket edemez
                inputHandler.DisableInput();
            }
            
            // NOT: CompleteIntro() burada çağrılmıyor!
            // Timer bitene kadar bekliyoruz
        }

        /// <summary>
        /// Timer tamamlandığında çağrılır - controller aktif olur
        /// </summary>
        private void HandleCharacterExitComplete()
        {
            Debug.Log("[PlayerIntroController] Timer bitti - Controller AKTİF!");
            
            // Input'u aktif et
            if (inputHandler != null)
            {
                inputHandler.EnableInput();
            }
            
            CompleteIntro();
        }

        /// <summary>
        /// Intro'yu tamamlar ve gameplay'e geçer.
        /// </summary>
        private void CompleteIntro()
        {
            _introPlaying = false;
            _introCompleted = true;

            OnIntroComplete?.Invoke();
            Debug.Log("[PlayerIntroController] Intro tamamlandı - Gameplay başlıyor!");
        }

        /// <summary>
        /// Intro'yu atlar.
        /// </summary>
        public void SkipIntro()
        {
            if (!_introPlaying) return;

            // Beşiği zorla devir
            if (cradleController != null)
            {
                // CradleController'da bir SkipFall methodu eklenebilir
                // Şimdilik direkt complete
            }

            if (inputHandler != null)
            {
                inputHandler.ExitCradleMode();
            }

            if (!_cameraSwitchTriggered)
            {
                _cameraSwitchTriggered = true;
                OnCameraSwitchPoint?.Invoke();
            }

            CompleteIntro();
        }

        /// <summary>
        /// Intro'yu sıfırlar.
        /// </summary>
        public void ResetIntro()
        {
            _introPlaying = false;
            _introCompleted = false;
            _cameraSwitchTriggered = false;
            
            if (cradleController != null)
            {
                cradleController.ResetCradle();
            }

            if (inputHandler != null)
            {
                inputHandler.ExitCradleMode();
            }

            InitializeForMenu();
        }

        #region Debug
        private void OnDrawGizmosSelected()
        {
            Vector3 basePos = Application.isPlaying ? _originalPosition : transform.position;
            
            // Spawn offset'i göster
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(basePos + spawnOffset, 0.3f);
            Gizmos.DrawLine(basePos, basePos + spawnOffset);
        }
        #endregion
    }
}
