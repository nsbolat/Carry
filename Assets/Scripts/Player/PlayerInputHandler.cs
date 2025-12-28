using UnityEngine;
using UnityEngine.InputSystem;

namespace Sisifos.Player
{
    /// <summary>
    /// Unity Input System kullanarak oyuncu girdilerini işler.
    /// SlopeCharacterController ile bağlantılı çalışır.
    /// Mevcut InputSystem_Actions asset'ini kullanır.
    /// </summary>
    [RequireComponent(typeof(SlopeCharacterController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Actions Asset")]
        [Tooltip("Assets klasöründeki InputSystem_Actions asset'ini buraya sürükleyin")]
        [SerializeField] private InputActionAsset inputActions;

        private SlopeCharacterController _characterController;
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private void Awake()
        {
            _characterController = GetComponent<SlopeCharacterController>();
            SetupInputActions();
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

            if (_moveAction == null) Debug.LogError("PlayerInputHandler: 'Move' action bulunamadı!");
            if (_jumpAction == null) Debug.LogError("PlayerInputHandler: 'Jump' action bulunamadı!");
            if (_sprintAction == null) Debug.LogWarning("PlayerInputHandler: 'Sprint' action bulunamadı (opsiyonel)");
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

            _playerActionMap.Disable();
        }

        private void Update()
        {
            if (_moveAction == null || _characterController == null) return;
            
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            _characterController.SetMoveInput(moveInput);
        }

        #region Input Callbacks
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _characterController.SetJumpInput(true);
        }

        private void OnSprintStarted(InputAction.CallbackContext context)
        {
            _characterController.SetRunning(true);
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _characterController.SetRunning(false);
        }
        #endregion
    }
}
