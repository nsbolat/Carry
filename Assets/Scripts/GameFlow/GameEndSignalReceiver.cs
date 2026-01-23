using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Sisifos.GameFlow
{
    /// <summary>
    /// Timeline'dan GameEndController metodlarını tetiklemek için Signal Receiver.
    /// Bu bileşende tanımlı metodlar Timeline Signal'ları ile çağrılabilir.
    /// </summary>
    public class GameEndSignalReceiver : MonoBehaviour, INotificationReceiver
    {
        [Header("References")]
        [Tooltip("GameEndController referansı")]
        public GameEndController gameEndController;
        
        private void Awake()
        {
            if (gameEndController == null)
            {
                gameEndController = GetComponent<GameEndController>();
                if (gameEndController == null)
                {
                    gameEndController = FindFirstObjectByType<GameEndController>();
                }
            }
        }
        
        /// <summary>
        /// Timeline Signal'ı alındığında çağrılır
        /// </summary>
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            // Signal tipine göre işlem yap
            if (notification is GameEndSignal signal)
            {
                ExecuteSignalAction(signal.action);
            }
        }
        
        private void ExecuteSignalAction(GameEndSignalAction action)
        {
            if (gameEndController == null)
            {
                Debug.LogError("[GameEndSignalReceiver] GameEndController bulunamadı!");
                return;
            }
            
            switch (action)
            {
                case GameEndSignalAction.DisableInput:
                    gameEndController.DisablePlayerInput();
                    break;
                    
                case GameEndSignalAction.StartFade:
                    gameEndController.StartFadeOut();
                    break;
                    
                case GameEndSignalAction.ShowEndUI:
                    gameEndController.ShowEndGameUI();
                    break;
                    
                case GameEndSignalAction.FinalizeEnd:
                    gameEndController.FinalizeGameEnd();
                    break;
            }
            
            Debug.Log($"[GameEndSignalReceiver] Signal alındı: {action}");
        }
        
        // === Timeline Signal olmadan direkt çağrılabilir metodlar ===
        
        public void DisableInput() => gameEndController?.DisablePlayerInput();
        public void StartFade() => gameEndController?.StartFadeOut();
        public void ShowEndUI() => gameEndController?.ShowEndGameUI();
        public void FinalizeEnd() => gameEndController?.FinalizeGameEnd();
    }
    
    /// <summary>
    /// Signal aksiyonları
    /// </summary>
    public enum GameEndSignalAction
    {
        DisableInput,
        StartFade,
        ShowEndUI,
        FinalizeEnd
    }
}
