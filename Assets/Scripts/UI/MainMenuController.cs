using UnityEngine;
using Sisifos.Core;
using Sisifos.Player;
using Sisifos.Camera;

namespace Sisifos.UI
{
    /// <summary>
    /// Ana menü akışını koordine eder.
    /// UI, karakter intro ve kamera geçişlerini yönetir.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainMenuUI menuUI;
        [SerializeField] private PlayerIntroController playerIntro;
        [SerializeField] private MenuCameraController menuCamera;

        [Header("Settings")]
        [SerializeField] private float delayBeforeIntro = 0.5f;

        private GameStateManager _gameState;

        private void Start()
        {
            _gameState = GameStateManager.Instance;
            
            if (_gameState == null)
            {
                Debug.LogError("[MainMenuController] GameStateManager bulunamadı!");
                return;
            }

            // Developer mode: menu ve intro atla
            if (_gameState.IsDeveloperMode)
            {
                // UI'ı gizle
                if (menuUI != null)
                {
                    menuUI.gameObject.SetActive(false);
                }
                // Menu kamerasını deaktif et
                if (menuCamera != null)
                {
                    menuCamera.SetMenuCameraActive(false);
                }
                return;
            }

            SetupEventListeners();
            InitializeMenu();
        }

        private void SetupEventListeners()
        {
            if (menuUI != null)
            {
                menuUI.OnStartClicked += HandleStartClicked;
                menuUI.OnQuitClicked += HandleQuitClicked;
            }

            if (playerIntro != null)
            {
                playerIntro.OnCameraSwitchPoint += HandleCameraSwitchPoint;
                playerIntro.OnIntroComplete += HandleIntroComplete;
            }
        }

        private void InitializeMenu()
        {
            _gameState.SetState(GameStateManager.GameState.MainMenu);

            if (menuCamera != null)
            {
                menuCamera.SetMenuCameraActive(true);
            }
        }

        private void HandleStartClicked()
        {
            if (_gameState.CurrentState != GameStateManager.GameState.MainMenu) return;

            _gameState.StartGame();

            if (menuUI != null)
            {
                menuUI.FadeOut(OnUIFadeComplete);
            }
            else
            {
                OnUIFadeComplete();
            }
        }

        private void OnUIFadeComplete()
        {
            StartCoroutine(StartIntroAfterDelay());
        }

        private System.Collections.IEnumerator StartIntroAfterDelay()
        {
            yield return new WaitForSeconds(delayBeforeIntro);

            if (playerIntro != null)
            {
                playerIntro.StartIntro();
            }
            else
            {
                HandleCameraSwitchPoint();
                HandleIntroComplete();
            }
        }

        private void HandleCameraSwitchPoint()
        {
            // Karakter hedefe yaklaştığında kamerayı gameplay moduna geçir
            if (menuCamera != null)
            {
                menuCamera.SetMenuCameraActive(false);
            }
        }

        private void HandleIntroComplete()
        {
            _gameState.EnterGameplay();
        }

        private void HandleQuitClicked()
        {
            _gameState.QuitGame();
        }

        private void OnDestroy()
        {
            if (menuUI != null)
            {
                menuUI.OnStartClicked -= HandleStartClicked;
                menuUI.OnQuitClicked -= HandleQuitClicked;
            }

            if (playerIntro != null)
            {
                playerIntro.OnCameraSwitchPoint -= HandleCameraSwitchPoint;
                playerIntro.OnIntroComplete -= HandleIntroComplete;
            }
        }
    }
}
