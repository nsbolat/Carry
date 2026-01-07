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

        [Header("Developer Mode")]
        [Tooltip("Aktif olduğunda menu ve intro atlanır, direkt gameplay başlar")]
        [SerializeField] private bool developerMode = false;

        [Header("Debug")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        public GameState CurrentState => currentState;
        public bool IsDeveloperMode => developerMode;

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action OnGameStarted;
        public event Action OnMenuEntered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Developer mode: direkt Playing state ile başla
            if (developerMode)
            {
                currentState = GameState.Playing;
            }
        }

        private void Start()
        {
            // Developer mode'da direkt gameplay event'ini tetikle
            if (developerMode)
            {
                Debug.Log("[GameStateManager] Developer Mode aktif - Menu ve intro atlandı");
                OnGameStarted?.Invoke();
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            OnStateChanged?.Invoke(newState);

            if (newState == GameState.Playing)
            {
                OnGameStarted?.Invoke();
            }
            else if (newState == GameState.MainMenu)
            {
                OnMenuEntered?.Invoke();
            }
        }

        public void StartGame()
        {
            if (currentState != GameState.MainMenu) return;
            SetState(GameState.Transitioning);
        }

        public void EnterGameplay()
        {
            SetState(GameState.Playing);
        }

        public void ReturnToMenu()
        {
            SetState(GameState.MainMenu);
        }

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
