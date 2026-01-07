using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Sisifos.UI
{
    /// <summary>
    /// Ana menü UI elementlerini yönetir.
    /// Logo, Start ve Quit butonlarını içerir.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Logo görseli (PNG/Sprite)")]
        [SerializeField] private Image logoImage;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        [Header("Button Styling")]
        [SerializeField] private TextMeshProUGUI startButtonText;
        [SerializeField] private TextMeshProUGUI quitButtonText;

        [Header("Animation Settings")]
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private float buttonHoverScale = 1.1f;
        [SerializeField] private float buttonScaleSpeed = 8f;

        [Header("Colors")]
        [SerializeField] private Color normalButtonColor = new Color(0.4f, 0.4f, 0.8f, 1f);
        [SerializeField] private Color hoverButtonColor = new Color(0.5f, 0.5f, 1f, 1f);

        // Button states
        private RectTransform _startButtonRect;
        private RectTransform _quitButtonRect;
        private Vector3 _startButtonOriginalScale;
        private Vector3 _quitButtonOriginalScale;
        private bool _startHovered;
        private bool _quitHovered;
        private Image _startButtonImage;
        private Image _quitButtonImage;

        // Events
        public event System.Action OnStartClicked;
        public event System.Action OnQuitClicked;

        private void Awake()
        {
            SetupReferences();
            SetupButtonEvents();
            ApplyInitialStyling();
        }

        private void SetupReferences()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (startButton != null)
            {
                _startButtonRect = startButton.GetComponent<RectTransform>();
                _startButtonOriginalScale = _startButtonRect.localScale;
                _startButtonImage = startButton.GetComponent<Image>();
            }

            if (quitButton != null)
            {
                _quitButtonRect = quitButton.GetComponent<RectTransform>();
                _quitButtonOriginalScale = _quitButtonRect.localScale;
                _quitButtonImage = quitButton.GetComponent<Image>();
            }
        }

        private void SetupButtonEvents()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
                
                // Hover events via EventTrigger
                var startTrigger = startButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                AddHoverEvents(startTrigger, true);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
                
                var quitTrigger = quitButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                AddHoverEvents(quitTrigger, false);
            }
        }

        private void AddHoverEvents(UnityEngine.EventSystems.EventTrigger trigger, bool isStartButton)
        {
            // Pointer Enter
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((_) => {
                if (isStartButton) _startHovered = true;
                else _quitHovered = true;
            });
            trigger.triggers.Add(enterEntry);

            // Pointer Exit
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((_) => {
                if (isStartButton) _startHovered = false;
                else _quitHovered = false;
            });
            trigger.triggers.Add(exitEntry);
        }

        private void ApplyInitialStyling()
        {
            if (_startButtonImage != null)
                _startButtonImage.color = normalButtonColor;

            if (_quitButtonImage != null)
                _quitButtonImage.color = normalButtonColor;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void Update()
        {
            UpdateButtonAnimations();
        }

        private void UpdateButtonAnimations()
        {
            // Start button hover animation
            if (_startButtonRect != null)
            {
                Vector3 targetScale = _startHovered 
                    ? _startButtonOriginalScale * buttonHoverScale 
                    : _startButtonOriginalScale;
                _startButtonRect.localScale = Vector3.Lerp(
                    _startButtonRect.localScale, 
                    targetScale, 
                    Time.deltaTime * buttonScaleSpeed
                );

                if (_startButtonImage != null)
                {
                    _startButtonImage.color = Color.Lerp(
                        _startButtonImage.color,
                        _startHovered ? hoverButtonColor : normalButtonColor,
                        Time.deltaTime * buttonScaleSpeed
                    );
                }
            }

            // Quit button hover animation
            if (_quitButtonRect != null)
            {
                Vector3 targetScale = _quitHovered 
                    ? _quitButtonOriginalScale * buttonHoverScale 
                    : _quitButtonOriginalScale;
                _quitButtonRect.localScale = Vector3.Lerp(
                    _quitButtonRect.localScale, 
                    targetScale, 
                    Time.deltaTime * buttonScaleSpeed
                );

                if (_quitButtonImage != null)
                {
                    _quitButtonImage.color = Color.Lerp(
                        _quitButtonImage.color,
                        _quitHovered ? hoverButtonColor : normalButtonColor,
                        Time.deltaTime * buttonScaleSpeed
                    );
                }
            }
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("[MainMenuUI] Start button clicked");
            OnStartClicked?.Invoke();
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("[MainMenuUI] Quit button clicked");
            OnQuitClicked?.Invoke();
        }

        /// <summary>
        /// UI'ı fade out yapar.
        /// </summary>
        public void FadeOut(System.Action onComplete = null)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }

        private IEnumerator FadeOutCoroutine(System.Action onComplete)
        {
            if (canvasGroup == null) yield break;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                // Ease out quad
                t = 1f - (1f - t) * (1f - t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);

            onComplete?.Invoke();
        }

        /// <summary>
        /// UI'ı fade in yapar (menu'ye dönüş için).
        /// </summary>
        public void FadeIn(float duration = 1f)
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeInCoroutine(duration));
        }

        private IEnumerator FadeInCoroutine(float duration)
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease out quad
                t = 1f - (1f - t) * (1f - t);
                canvasGroup.alpha = t;
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Butonları etkinleştirir/devre dışı bırakır.
        /// </summary>
        public void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null) startButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
        }
    }
}
