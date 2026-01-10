using UnityEngine;

namespace Sisifos.UI
{
    /// <summary>
    /// Beşik üzerindeki bilgi UI'ını yönetir.
    /// Start'tan sonra fade ile açılır, A-D'ye basınca fade ile kapanır.
    /// </summary>
    public class CradleInfoUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Fade edilecek CanvasGroup (Canvas'a CanvasGroup ekleyin)")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Tooltip("CradleController referansı - input algılamak için")]
        [SerializeField] private Sisifos.Interaction.CradleController cradleController;

        [Header("Fade In Settings")]
        [Tooltip("Start'tan sonra UI'ın açılması için gecikme")]
        [SerializeField] private float fadeInDelay = 1f;
        
        [Tooltip("Fade in süresi (saniye)")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Header("Fade Out Settings")]
        [Tooltip("Fade out süresi (saniye)")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Tooltip("Input algılandıktan sonra fade başlamadan önceki gecikme")]
        [SerializeField] private float fadeOutDelay = 0.2f;

        // State
        private bool _hasFadedIn = false;
        private bool _hasFadedOut = false;
        private bool _isListening = false;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            
            // Başlangıçta GİZLİ
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void Start()
        {
            // CradleController event'ine abone ol
            if (cradleController != null)
            {
                cradleController.OnRockingStarted += OnRockingStarted;
                cradleController.OnCradleFallen += OnCradleFallen;
            }
        }

        private void OnDestroy()
        {
            if (cradleController != null)
            {
                cradleController.OnRockingStarted -= OnRockingStarted;
                cradleController.OnCradleFallen -= OnCradleFallen;
            }
        }

        private void Update()
        {
            if (!_isListening || _hasFadedOut || cradleController == null) return;
            
            // Beşik sallanmaya başladıysa (input algılandı)
            if (cradleController.IsRockingEnabled && Mathf.Abs(cradleController.CurrentAngle) > 0.5f)
            {
                // Oyuncu sallıyor - UI'ı kapat
                StartFadeOut();
            }
        }

        private void OnRockingStarted()
        {
            // Rocking aktif oldu - önce UI'ı fade in ile göster
            if (!_hasFadedIn && !_hasFadedOut)
            {
                StartCoroutine(FadeIn());
            }
        }

        private void OnCradleFallen()
        {
            // Beşik düştü - UI'ı hemen kapat
            if (!_hasFadedOut)
            {
                StartFadeOut();
            }
        }

        private System.Collections.IEnumerator FadeIn()
        {
            // Gecikme
            if (fadeInDelay > 0)
            {
                yield return new WaitForSeconds(fadeInDelay);
            }
            
            // Eğer bu arada fade out başladıysa çık
            if (_hasFadedOut) yield break;
            
            if (canvasGroup == null) yield break;
            
            Debug.Log("[CradleInfoUI] Info UI açılıyor...");
            
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            _hasFadedIn = true;
            _isListening = true;
            
            Debug.Log("[CradleInfoUI] Info UI açıldı - A/D bekliyor");
        }

        private void StartFadeOut()
        {
            if (_hasFadedOut) return;
            _hasFadedOut = true;
            _isListening = false;
            
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        private System.Collections.IEnumerator FadeOut()
        {
            // Kısa gecikme
            if (fadeOutDelay > 0)
            {
                yield return new WaitForSeconds(fadeOutDelay);
            }
            
            if (canvasGroup == null) yield break;
            
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            
            // UI'ı deaktif et
            gameObject.SetActive(false);
            
            Debug.Log("[CradleInfoUI] Info UI kapandı");
        }

        /// <summary>
        /// UI'ı tekrar gösterir (reset için)
        /// </summary>
        public void Reset()
        {
            StopAllCoroutines();
            _hasFadedIn = false;
            _hasFadedOut = false;
            _isListening = false;
            gameObject.SetActive(true);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// UI'ı hemen gizler (fade olmadan)
        /// </summary>
        public void HideImmediate()
        {
            StopAllCoroutines();
            _hasFadedOut = true;
            _isListening = false;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            
            gameObject.SetActive(false);
        }
    }
}
