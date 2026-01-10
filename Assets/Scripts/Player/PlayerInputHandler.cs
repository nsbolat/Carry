using UnityEngine;
using UnityEngine.InputSystem;
using Sisifos.Interaction;
using Sisifos.Core;

namespace Sisifos.Player
{
    /// <summary>
    /// Unity Input System kullanarak oyuncu girdilerini işler.
    /// SlopeCharacterController ve BoxCarrier ile bağlantılı çalışır.
    /// Mevcut InputSystem_Actions asset'ini kullanır.
    /// </summary>
    [RequireComponent(typeof(SlopeCharacterController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Actions Asset")]
        [Tooltip("Assets klasöründeki InputSystem_Actions asset'ini buraya sürükleyin")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Box Carrying")]
        [Tooltip("Kutu taşıma controller'ı (opsiyonel)")]
        [SerializeField] private BoxCarrier boxCarrier;

        [Header("Menu Integration")]
        [Tooltip("Oyun başladığında input'u devre dışı bırak (menu için)")]
        [SerializeField] private bool startDisabled = true;

        private SlopeCharacterController _characterController;
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _interactAction;
        
        private bool _inputEnabled = true;
        
        // Cradle mode
        private CradleController _cradleController;
        private bool _isCradleMode = false;

        private void Awake()
        {
            _characterController = GetComponent<SlopeCharacterController>();
            SetupInputActions();
        }

        private void Start()
        {
            // GameStateManager event'lerine abone ol
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStarted += EnableInput;
                GameStateManager.Instance.OnMenuEntered += DisableInput;
                
                // Başlangıç durumunu ayarla
                if (startDisabled && GameStateManager.Instance.CurrentState == GameStateManager.GameState.MainMenu)
                {
                    DisableInput();
                }
            }
            else if (startDisabled)
            {
                DisableInput();
            }
        }

        private void OnDestroy()
        {
            // Event'lerden aboneliği kaldır
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStarted -= EnableInput;
                GameStateManager.Instance.OnMenuEntered -= DisableInput;
            }
        }

        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputHandler: InputActionAsset atanmadı! Assets klasöründeki 'InputSystem_Actions' asset'ini sürükleyin.");
                return;
            }

            // "Player" action map'ini al
            _playerActionMap = inputActions.FindActionMap("Player");
            
            if (_playerActionMap == null)
            {
                Debug.LogError("PlayerInputHandler: 'Player' action map bulunamadı!");
                return;
            }

            // Action'ları bul
            _moveAction = _playerActionMap.FindAction("Move");
            _jumpAction = _playerActionMap.FindAction("Jump");
            _sprintAction = _playerActionMap.FindAction("Sprint");
            _interactAction = _playerActionMap.FindAction("Interact");

            if (_moveAction == null) Debug.LogError("PlayerInputHandler: 'Move' action bulunamadı!");
            if (_jumpAction == null) Debug.LogError("PlayerInputHandler: 'Jump' action bulunamadı!");
            if (_sprintAction == null) Debug.LogWarning("PlayerInputHandler: 'Sprint' action bulunamadı (opsiyonel)");
            if (_interactAction == null) Debug.LogWarning("PlayerInputHandler: 'Interact' action bulunamadı. InputSystem_Actions'a 'Interact' (E tuşu) ekleyin.");
        }

        private void OnEnable()
        {
            if (_playerActionMap == null) return;
            
            _playerActionMap.Enable();

            if (_jumpAction != null)
                _jumpAction.performed += OnJumpPerformed;
            
            if (_sprintAction != null)
            {
                _sprintAction.started += OnSprintStarted;
                _sprintAction.canceled += OnSprintCanceled;
            }

            if (_interactAction != null)
                _interactAction.performed += OnInteractPerformed;
        }

        private void OnDisable()
        {
            if (_playerActionMap == null) return;

            if (_jumpAction != null)
                _jumpAction.performed -= OnJumpPerformed;
            
            if (_sprintAction != null)
            {
                _sprintAction.started -= OnSprintStarted;
                _sprintAction.canceled -= OnSprintCanceled;
            }

            if (_interactAction != null)
                _interactAction.performed -= OnInteractPerformed;

            _playerActionMap.Disable();
        }

        private void Update()
        {
            if (_moveAction == null) return;
            
            // Input devre dışıysa hareket verme
            if (!_inputEnabled)
            {
                if (_characterController != null)
                    _characterController.SetMoveInput(Vector2.zero);
                return;
            }
            
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            
            // Beşik modundaysa input'u beşiğe yönlendir
            if (_isCradleMode && _cradleController != null)
            {
                _cradleController.SetRockingInput(moveInput.x);
                return;
            }
            
            // Normal hareket modu
            if (_characterController != null)
                _characterController.SetMoveInput(moveInput);
        }

        #region Public Methods
        /// <summary>
        /// Oyuncu input'unu etkinleştirir.
        /// </summary>
        public void EnableInput()
        {
            _inputEnabled = true;
            Debug.Log("[PlayerInputHandler] Input enabled");
        }

        /// <summary>
        /// Oyuncu input'unu devre dışı bırakır.
        /// </summary>
        public void DisableInput()
        {
            _inputEnabled = false;
            
            // Mevcut hareketi sıfırla
            if (_characterController != null)
            {
                _characterController.SetMoveInput(Vector2.zero);
                _characterController.SetRunning(false);
            }
            
            Debug.Log("[PlayerInputHandler] Input disabled");
        }

        /// <summary>
        /// Input'un aktif olup olmadığını döndürür.
        /// </summary>
        public bool IsInputEnabled => _inputEnabled;

        /// <summary>
        /// Beşik modunu aktif eder. Input beşiğe yönlendirilir.
        /// </summary>
        public void SetCradleMode(CradleController cradle)
        {
            _cradleController = cradle;
            _isCradleMode = true;
            _inputEnabled = true; // Beşik modunda input aktif olmalı
            Debug.Log("[PlayerInputHandler] Cradle mode enabled - A/D ile beşiği sallayın");
        }

        /// <summary>
        /// Beşik modundan çıkar. Input karakter kontrolcüsüne yönlendirilir.
        /// </summary>
        public void ExitCradleMode()
        {
            _isCradleMode = false;
            _cradleController = null;
            Debug.Log("[PlayerInputHandler] Cradle mode disabled - Normal hareket aktif");
        }

        /// <summary>
        /// Beşik modunda olup olmadığını döndürür.
        /// </summary>
        public bool IsCradleMode => _isCradleMode;
        #endregion

        #region Input Callbacks
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            _characterController.SetJumpInput(true);
        }

        private void OnSprintStarted(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            _characterController.SetRunning(true);
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            _characterController.SetRunning(false);
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled) return;
            
            Debug.Log("E tuşuna basıldı! (Interact performed)");
            
            if (boxCarrier != null)
            {
                boxCarrier.OnInteract();
            }
            else
            {
                Debug.LogWarning("BoxCarrier atanmamış! PlayerInputHandler'daki Box Carrier alanını kontrol edin.");
            }
        }
        #endregion
    }
}
