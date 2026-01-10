using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Sisifos.Player
{
    /// <summary>
    /// Kutu toplandığında karakter modelini değiştiren evrim sistemi.
    /// Her yaşam dönemi farklı model, animasyon ve gameplay parametreleri içerir.
    /// </summary>
    public class CharacterEvolutionManager : MonoBehaviour
    {
        [System.Serializable]
        public class LifeStageSetup
        {
            [Tooltip("Dönem adı (debug için)")]
            public string stageName = "Stage";
            
            [Tooltip("Sahnedeki karakter modeli (Player'ın child'ı)")]
            public GameObject characterModel;
            
            [Tooltip("Halatın bağlanacağı kemik (spine/sırt) - her karakter için ayrı")]
            public Transform ropeAttachBone;
            
            [Header("Movement Modifiers")]
            [Tooltip("Hareket hızı çarpanı")]
            [Range(0.5f, 2f)]
            public float moveSpeedMultiplier = 1f;
            
            [Tooltip("Zıplama kuvveti çarpanı")]
            [Range(0.5f, 2f)]
            public float jumpForceMultiplier = 1f;
            
            [Tooltip("Hızlanma çarpanı")]
            [Range(0.5f, 2f)]
            public float accelerationMultiplier = 1f;
        }

        [Header("Life Stages")]
        [Tooltip("Yaşam dönemleri sırayla - Model'leri sahneden sürükle!")]
        [SerializeField] private LifeStageSetup[] lifeStages;

        [Header("Transition Settings")]
        [Tooltip("Geçiş süresi (saniye)")]
        [SerializeField] private float transitionDuration = 0.5f;
        
        [Tooltip("Beşik düşene kadar bekle - CradleController tarafından aktif edilecek")]
        [SerializeField] private bool waitForCradleFall = true;
        
        [Tooltip("Fade için UI Image (opsiyonel - Canvas'ta olmalı)")]
        [SerializeField] private Image fadeImage;
        
        [Tooltip("Fade rengi")]
        [SerializeField] private Color fadeColor = Color.black;

        [Header("References")]
        [Tooltip("Ana karakter kontrolcüsü")]
        [SerializeField] private SlopeCharacterController characterController;
        
        [Tooltip("Box carrier (rope attach bone için)")]
        [SerializeField] private Sisifos.Interaction.BoxCarrier boxCarrier;

        // State
        private int _currentStageIndex = 0;
        private bool _isTransitioning = false;
        private bool _isInitialized = false;
        private GameObject _currentCharacterModel;
        private Animator _currentAnimator;

        // Events
        public event System.Action<LifeStageSetup> OnLifeStageChanged;
        public event System.Action<int, int> OnStageIndexChanged; // (oldIndex, newIndex)

        #region Properties
        public int CurrentStageIndex => _currentStageIndex;
        public LifeStageSetup CurrentStage => 
            lifeStages != null && _currentStageIndex < lifeStages.Length 
                ? lifeStages[_currentStageIndex] 
                : null;
        public bool IsTransitioning => _isTransitioning;
        public int TotalStages => lifeStages?.Length ?? 0;
        #endregion

        private void Awake()
        {
            // Referansları otomatik bul
            if (characterController == null)
                characterController = GetComponent<SlopeCharacterController>();
            
            if (boxCarrier == null)
                boxCarrier = GetComponent<Sisifos.Interaction.BoxCarrier>();
        }

        private void Start()
        {
            if (waitForCradleFall)
            {
                // Beşik düşene kadar tüm modelleri gizle
                HideAllStages();
                Debug.Log("[CharacterEvolution] waitForCradleFall aktif - Beşik düşene kadar bekleniyor");
            }
            else
            {
                // Normal başlangıç
                InitializeFirstStage();
            }
        }

        /// <summary>
        /// Tüm stage'leri gizler (beşik düşmeden önce)
        /// </summary>
        private void HideAllStages()
        {
            if (lifeStages == null) return;
            
            foreach (var stage in lifeStages)
            {
                if (stage != null && stage.characterModel != null)
                {
                    stage.characterModel.SetActive(false);
                }
            }
        }

        /// <summary>
        /// İlk yaşam dönemini ayarlar - dışarıdan çağrılabilir (CradleController için)
        /// </summary>
        public void InitializeFirstStage()
        {
            if (_isInitialized) return;
            
            if (lifeStages == null || lifeStages.Length == 0)
            {
                Debug.LogWarning("[CharacterEvolution] No life stages defined!");
                return;
            }

            // Tüm modelleri deaktif et
            foreach (var stage in lifeStages)
            {
                if (stage != null && stage.characterModel != null)
                {
                    stage.characterModel.SetActive(false);
                }
            }

            // İlk dönemi aktifle
            _currentStageIndex = 0;
            ActivateStage(lifeStages[0]);
            _isInitialized = true;
        }

        /// <summary>
        /// Bir sonraki yaşam dönemine geçiş yapar
        /// </summary>
        public void AdvanceToNextStage()
        {
            if (_isTransitioning)
            {
                Debug.Log("[CharacterEvolution] Transition already in progress!");
                return;
            }

            int nextIndex = _currentStageIndex + 1;
            
            // Son dönemdeyse geçiş yapma (veya döngüye al)
            if (nextIndex >= lifeStages.Length)
            {
                Debug.Log("[CharacterEvolution] Already at final life stage!");
                // İsteğe bağlı: döngüsel sistem için nextIndex = 0;
                return;
            }

            StartCoroutine(TransitionToStage(nextIndex));
        }

        /// <summary>
        /// Belirli bir yaşam dönemine geçiş yapar
        /// </summary>
        public void SetLifeStage(int stageIndex)
        {
            if (_isTransitioning)
            {
                Debug.Log("[CharacterEvolution] Transition already in progress!");
                return;
            }

            if (stageIndex < 0 || stageIndex >= lifeStages.Length)
            {
                Debug.LogWarning($"[CharacterEvolution] Invalid stage index: {stageIndex}");
                return;
            }

            if (stageIndex == _currentStageIndex)
            {
                Debug.Log("[CharacterEvolution] Already at this stage!");
                return;
            }

            StartCoroutine(TransitionToStage(stageIndex));
        }

        /// <summary>
        /// Karakter geçişi - anında, fade olmadan
        /// </summary>
        private IEnumerator TransitionToStage(int targetStageIndex)
        {
            _isTransitioning = true;
            int oldIndex = _currentStageIndex;
            
            LifeStageSetup targetStage = lifeStages[targetStageIndex];
            
            Debug.Log($"[CharacterEvolution] Transitioning from {lifeStages[_currentStageIndex].stageName} to {targetStage.stageName}");

            // Karakter değiştir - anında, fade olmadan
            DeactivateCurrentStage();
            _currentStageIndex = targetStageIndex;
            ActivateStage(targetStage);

            _isTransitioning = false;

            // Event'leri tetikle
            OnLifeStageChanged?.Invoke(targetStage);
            OnStageIndexChanged?.Invoke(oldIndex, targetStageIndex);
            
            Debug.Log($"[CharacterEvolution] Transition complete! Now at: {targetStage.stageName}");
            
            yield break;
        }

        /// <summary>
        /// Mevcut dönemi deaktif et
        /// </summary>
        private void DeactivateCurrentStage()
        {
            if (_currentCharacterModel != null)
            {
                _currentCharacterModel.SetActive(false);
            }
        }

        /// <summary>
        /// Yeni dönemi aktifle ve modifierleri uygula
        /// </summary>
        private void ActivateStage(LifeStageSetup stage)
        {
            if (stage == null || stage.characterModel == null)
            {
                Debug.LogError("[CharacterEvolution] Stage or character model is null!");
                return;
            }

            // Modeli aktifle
            stage.characterModel.SetActive(true);
            _currentCharacterModel = stage.characterModel;
            
            // TÜM RENDERER'LARI AÇ (CradleController gizlemiş olabilir)
            Renderer[] renderers = stage.characterModel.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }

            // Animator'ü güncelle - sahnedeki modelden al
            _currentAnimator = stage.characterModel.GetComponentInChildren<Animator>();
            
            if (_currentAnimator == null)
            {
                Debug.LogWarning($"[CharacterEvolution] No Animator found on {stage.characterModel.name}!");
            }
            
            // SlopeCharacterController'a animator ver ve modifierleri uygula
            if (characterController != null)
            {
                characterController.SetAnimator(_currentAnimator);
                characterController.ApplyLifeStageModifiers(
                    stage.moveSpeedMultiplier, 
                    stage.jumpForceMultiplier, 
                    stage.accelerationMultiplier
                );
            }

            // BoxCarrier'ın rope attach bone'unu güncelle
            if (boxCarrier != null && stage.ropeAttachBone != null)
            {
                boxCarrier.SetRopeAttachBone(stage.ropeAttachBone);
            }
        }

        /// <summary>
        /// Fade animasyonu
        /// </summary>
        private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
        {
            if (fadeImage == null) yield break;

            fadeImage.gameObject.SetActive(true);
            Color color = fadeColor;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                fadeImage.color = color;
                yield return null;
            }

            color.a = endAlpha;
            fadeImage.color = color;

            // Tamamen şeffafsa deaktif et
            if (endAlpha <= 0f)
            {
                fadeImage.gameObject.SetActive(false);
            }
        }

        #region Debug
        [ContextMenu("Advance to Next Stage")]
        private void DebugAdvanceStage()
        {
            AdvanceToNextStage();
        }

        [ContextMenu("Reset to First Stage")]
        private void DebugResetStage()
        {
            if (!_isTransitioning)
            {
                StartCoroutine(TransitionToStage(0));
            }
        }
        #endregion
    }
}
