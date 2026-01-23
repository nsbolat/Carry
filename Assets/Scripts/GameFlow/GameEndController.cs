using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Sisifos.Player;

namespace Sisifos.GameFlow
{
    /// <summary>
    /// Oyun sonu sürecini yöneten controller.
    /// Timeline tarafından Signal ile tetiklenebilir.
    /// </summary>
    public class GameEndController : MonoBehaviour
    {
        [Header("Player Reference")]
        [Tooltip("Oyuncu transform'u (input disable için)")]
        public Transform player;
        
        [Header("UI References")]
        [Tooltip("Fade panel (CanvasGroup olmalı)")]
        public CanvasGroup fadePanel;
        
        [Tooltip("Oyun sonu UI paneli")]
        public GameObject endGameUI;
        
        [Header("Fade Settings")]
        [Tooltip("Fade süresi (saniye)")]
        public float fadeDuration = 2f;
        
        [Header("End Options")]
        [Tooltip("Oyun sonunda yüklenecek sahne (boş = aynı sahnede kal)")]
        public string endSceneName = "";
        
        [Tooltip("Oyun sonunda sahne yüklemeden önce bekleme")]
        public float delayBeforeSceneLoad = 3f;
        
        [Header("Events")]
        public UnityEvent onGameEndStarted;
        public UnityEvent onFadeComplete;
        public UnityEvent onGameEndComplete;
        
        // State
        private bool _gameEnded = false;
        private PlayerInputHandler _playerInput;
        private SlopeCharacterController _characterController;
        
        private void Awake()
        {
            // Player referanslarını bul
            if (player != null)
            {
                _playerInput = player.GetComponent<PlayerInputHandler>();
                _characterController = player.GetComponent<SlopeCharacterController>();
            }
            
            // Fade panel başlangıçta görünmez
            if (fadePanel != null)
            {
                fadePanel.alpha = 0f;
                fadePanel.gameObject.SetActive(true);
            }
            
            // End UI başlangıçta gizli
            if (endGameUI != null)
            {
                endGameUI.SetActive(false);
            }
        }
        
        /// <summary>
        /// Oyun sonunu başlatır - Timeline Signal veya GameEndTrigger tarafından çağrılır
        /// </summary>
        public void StartGameEnd()
        {
            if (_gameEnded) return;
            _gameEnded = true;
            
            Debug.Log("[GameEndController] Oyun sonu başladı!");
            
            // Input'u devre dışı bırak
            DisablePlayerInput();
            
            // Event tetikle
            onGameEndStarted?.Invoke();
        }
        
        /// <summary>
        /// Oyuncu input'unu devre dışı bırakır - Timeline Signal ile çağrılabilir
        /// </summary>
        public void DisablePlayerInput()
        {
            if (_playerInput != null)
            {
                _playerInput.enabled = false;
            }
            
            // Karakteri durdur
            if (_characterController != null)
            {
                _characterController.SetMoveInput(Vector2.zero);
            }
            
            Debug.Log("[GameEndController] Oyuncu input'u devre dışı!");
        }
        
        /// <summary>
        /// Ekranı karartır - Timeline Signal ile çağrılabilir
        /// </summary>
        public void StartFadeOut()
        {
            StartCoroutine(FadeOutCoroutine());
        }
        
        private System.Collections.IEnumerator FadeOutCoroutine()
        {
            if (fadePanel == null)
            {
                Debug.LogWarning("[GameEndController] Fade panel atanmamış!");
                yield break;
            }
            
            float elapsed = 0f;
            fadePanel.alpha = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            
            fadePanel.alpha = 1f;
            
            Debug.Log("[GameEndController] Fade tamamlandı!");
            onFadeComplete?.Invoke();
        }
        
        /// <summary>
        /// Oyun sonu UI'ını gösterir - Timeline Signal ile çağrılabilir
        /// </summary>
        public void ShowEndGameUI()
        {
            if (endGameUI != null)
            {
                endGameUI.SetActive(true);
            }
            
            Debug.Log("[GameEndController] Oyun sonu ekranı gösterildi!");
            onGameEndComplete?.Invoke();
        }
        
        /// <summary>
        /// Oyunu tamamen bitirir - yeni sahne yükler veya uygulamadan çıkar
        /// </summary>
        public void FinalizeGameEnd()
        {
            StartCoroutine(FinalizeCoroutine());
        }
        
        private System.Collections.IEnumerator FinalizeCoroutine()
        {
            yield return new WaitForSeconds(delayBeforeSceneLoad);
            
            if (!string.IsNullOrEmpty(endSceneName))
            {
                SceneManager.LoadScene(endSceneName);
            }
            else
            {
                Debug.Log("[GameEndController] Oyun sonu - sahne yüklenmiyor (endSceneName boş)");
            }
        }
        
        /// <summary>
        /// Ana menüye döner - UI butonu için
        /// </summary>
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(0); // İlk sahne (Main Menu)
        }
        
        /// <summary>
        /// Oyundan çıkar - UI butonu için
        /// </summary>
        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
