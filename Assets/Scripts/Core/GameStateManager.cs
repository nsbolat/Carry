using UnityEngine;
using System;

namespace Sisifos.Core
{
    /// <summary>
    /// Oyun durumunu yöneten singleton.
    /// Menu, geçiş ve gameplay state'lerini kontrol eder.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            Transitioning,
            Playing
        }

        [Header("Debug")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        public GameState CurrentState => currentState;

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action OnGameStarted;
        public event Action OnMenuEntered;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Oyun durumunu değiştirir ve event'leri tetikler.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"[GameStateManager] State changed: {previousState} -> {newState}");

            OnStateChanged?.Invoke(newState);

            // Specific events
            if (newState == GameState.Playing)
            {
                OnGameStarted?.Invoke();
            }
            else if (newState == GameState.MainMenu)
            {
                OnMenuEntered?.Invoke();
            }
        }

        /// <summary>
        /// Oyunun başlatılmasını tetikler (Start butonundan çağrılır).
        /// </summary>
        public void StartGame()
        {
            if (currentState != GameState.MainMenu) return;
            SetState(GameState.Transitioning);
        }

        /// <summary>
        /// Gameplay moduna geçiş yapar.
        /// </summary>
        public void EnterGameplay()
        {
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Ana menüye döner.
        /// </summary>
        public void ReturnToMenu()
        {
            SetState(GameState.MainMenu);
        }

        /// <summary>
        /// Oyundan çıkış yapar.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameStateManager] Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
