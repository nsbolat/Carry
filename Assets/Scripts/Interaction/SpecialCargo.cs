using UnityEngine;

namespace Sisifos.Interaction
{
    /// <summary>
    /// Özel yük bileşeni - Bu yük alındığında diğer tüm yükler otomatik olarak bırakılır.
    /// DraggableBox ile birlikte kullanılmalıdır.
    /// </summary>
    [RequireComponent(typeof(DraggableBox))]
    public class SpecialCargo : MonoBehaviour
    {
        [Header("Special Cargo Settings")]
        [Tooltip("Bu yük alındığında diğer yükler bırakılsın mı?")]
        public bool detachOthersOnPickup = true;
        
        [Tooltip("Bu yük taşınırken başka yük alınabilsin mi?")]
        public bool allowOtherCargoWhileCarrying = false;
        
        [Tooltip("Özel efekt prefab'ı (alındığında spawn edilir)")]
        public GameObject pickupEffectPrefab;
        
        [Tooltip("Diğer yükler bırakıldığında çalınacak ses")]
        public AudioClip detachOthersSound;
        
        [Header("Visual Feedback")]
        [Tooltip("Özel yük göstergesi (glow, particle vb.)")]
        public GameObject specialIndicator;
        
        [Tooltip("Özel yük rengi")]
        public Color specialCargoColor = new Color(1f, 0.8f, 0.2f, 1f); // Altın sarısı
        
        // Components
        private DraggableBox _draggableBox;
        private Renderer _renderer;
        private Color _originalColor;
        
        /// <summary>
        /// Bu objenin özel yük olup olmadığını döndürür
        /// </summary>
        public bool IsSpecialCargo => true;
        
        /// <summary>
        /// DraggableBox referansı
        /// </summary>
        public DraggableBox DraggableBox => _draggableBox;
        
        private void Awake()
        {
            _draggableBox = GetComponent<DraggableBox>();
            _renderer = GetComponentInChildren<Renderer>();
            
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }
            
            // Özel göstergeyi aktifleştir
            if (specialIndicator != null)
            {
                specialIndicator.SetActive(true);
            }
        }
        
        private void Start()
        {
            // Başlangıçta özel rengi uygula
            ApplySpecialVisual();
        }
        
        /// <summary>
        /// Özel yük görselini uygular
        /// </summary>
        public void ApplySpecialVisual()
        {
            if (_renderer != null)
            {
                // Emission ekle veya renk değiştir
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", specialCargoColor * 0.3f);
                _renderer.SetPropertyBlock(block);
            }
        }
        
        /// <summary>
        /// Alındığında çağrılır
        /// </summary>
        public void OnPickedUp()
        {
            // Efekt spawn et
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Debug.Log("[SpecialCargo] Özel yük alındı!");
        }
        
        /// <summary>
        /// Diğer yükler çözüldüğünde çağrılır
        /// </summary>
        public void OnOthersDetached(int detachedCount)
        {
            if (detachOthersSound != null)
            {
                AudioSource.PlayClipAtPoint(detachOthersSound, transform.position);
            }
            
            Debug.Log($"[SpecialCargo] {detachedCount} yük bırakıldı, sadece özel yük kaldı.");
        }
    }
}
