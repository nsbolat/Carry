using UnityEngine;
using System.Collections.Generic;
using Sisifos.Player;

namespace Sisifos.Interaction
{
    /// <summary>
    /// Oyuncunun kutuları bağlayıp sürüklemesini yöneten ana controller.
    /// PlayerInputHandler ile birlikte çalışır.
    /// </summary>
    public class BoxCarrier : MonoBehaviour
    {
        [Header("Carry Settings")]
        [Tooltip("Maksimum taşınabilir kutu sayısı (0 = sınırsız)")]
        [SerializeField] private int maxBoxes = 5;
        
        [Tooltip("Halatın bağlanacağı kemik (spine/sırt)")]
        [SerializeField] private Transform ropeAttachBone;
        
        [Tooltip("Kemik üzerindeki ek offset")]
        [SerializeField] private Vector3 attachPointOffset = new Vector3(0f, 0f, -0.2f);

        [Header("Funnel Layout - Huni Dizilimi")]
        [Tooltip("Kutular arası yatay mesafe (huni genişliği)")]
        [SerializeField] private float funnelSpreadX = 1.5f;
        
        [Tooltip("Her kutu için arkaya (Z) eklenen mesafe")]
        [SerializeField] private float funnelSpreadZ = 1.0f;
        
        [Tooltip("Minimum halat uzunluğu - kutular bu mesafeden yakın olamaz")]
        [SerializeField] private float baseRopeLength = 1.0f;
        
        [Tooltip("Funnel offset'e eklenen ekstra halat boşluğu")]
        [SerializeField] private float ropeLengthIncrement = 0.3f;

        [Header("Interaction")]
        [Tooltip("Etkileşim için kutu arama yarıçapı")]
        [SerializeField] private float detectionRadius = 3f;
        
        [Tooltip("Etkileşim için layer mask")]
        [SerializeField] private LayerMask boxLayer;

        [Header("Visual")]
        [Tooltip("Halat görselleştirici (opsiyonel)")]
        [SerializeField] private RopeVisualizer ropeVisualizer;

        [Header("Audio")]
        [Tooltip("Kutu bağlandığında çalınacak ses")]
        [SerializeField] private AudioClip attachSound;
        
        [Tooltip("Ses kaynağı (opsiyonel - yoksa otomatik oluşturulur)")]
        [SerializeField] private AudioSource audioSource;

        // Bağlı kutuların listesi (sıralı - ilk oyuncuya bağlı)
        private List<DraggableBox> _attachedBoxes = new List<DraggableBox>();
        
        // En yakın etkileşilebilir kutu (highlight için)
        private DraggableBox _nearestBox;
        
        // Karakter kontrolcüsü referansı (ağırlık sistemi için)
        private SlopeCharacterController _characterController;
        
        // Karakter evrim yöneticisi referansı
        private CharacterEvolutionManager _evolutionManager;

        #region Properties
        public int AttachedBoxCount => _attachedBoxes.Count;
        public bool CanAttachMore => maxBoxes <= 0 || _attachedBoxes.Count < maxBoxes;
        public DraggableBox NearestInteractableBox => _nearestBox;
        public IReadOnlyList<DraggableBox> AttachedBoxes => _attachedBoxes.AsReadOnly();
        
        /// <summary>
        /// Halatın bağlandığı nokta - spine bone varsa onu kullan
        /// </summary>
        public Vector3 AttachPoint
        {
            get
            {
                if (ropeAttachBone != null)
                {
                    // Bone pozisyonu + lokal offset
                    return ropeAttachBone.position + ropeAttachBone.TransformDirection(attachPointOffset);
                }
                // Fallback - ana transform
                return transform.position + transform.TransformDirection(attachPointOffset);
            }
        }

        /// <summary>
        /// Rope attach bone'u değiştirir (karakter modeli değiştiğinde)
        /// </summary>
        public void SetRopeAttachBone(Transform newBone)
        {
            ropeAttachBone = newBone;
            Debug.Log($"[BoxCarrier] Rope attach bone updated to: {(newBone != null ? newBone.name : "null")}");
        }
        #endregion

        private void Awake()
        {
            _characterController = GetComponent<SlopeCharacterController>();
            _evolutionManager = GetComponent<CharacterEvolutionManager>();
        }

        private void Update()
        {
            UpdateNearestBox();
            
            // Otomatik kutu bağlama - yakındaki kutuyu bul ve bağla
            if (_nearestBox != null && !_nearestBox.IsAttached && CanAttachMore)
            {
                AttachBox(_nearestBox);
            }
            
            UpdateRopeVisual();
            UpdateTensionFeedback();
        }

        /// <summary>
        /// Tüm kutuların gerginliğini hesaplar ve karaktere iletir
        /// </summary>
        private void UpdateTensionFeedback()
        {
            if (_characterController == null || _attachedBoxes.Count == 0)
            {
                if (_characterController != null)
                {
                    _characterController.SetRopeTension(0f);
                }
                return;
            }

            // Tüm kutuların maksimum gerginliğini bul
            float maxTension = 0f;
            foreach (var box in _attachedBoxes)
            {
                if (box != null && box.Tension > maxTension)
                {
                    maxTension = box.Tension;
                }
            }

            _characterController.SetRopeTension(maxTension);
        }

        /// <summary>
        /// Etkileşim tuşuna basıldığında çağrılır - sadece kutu bağlar
        /// </summary>
        public void OnInteract()
        {
            // Eğer yakında bağlanabilir kutu varsa, bağla
            if (_nearestBox != null && !_nearestBox.IsAttached && CanAttachMore)
            {
                AttachBox(_nearestBox);
            }
        }

        /// <summary>
        /// Belirli bir kutuyu bağlar - Tüm kutular oyuncuya doğrudan bağlanır ve huni şeklinde dizilir
        /// </summary>
        public void AttachBox(DraggableBox box)
        {
            if (box == null || box.IsAttached) return;
            if (!CanAttachMore) return;

            // Kutu indeksi (0'dan başlar)
            int boxIndex = _attachedBoxes.Count;
            
            // Huni offset hesapla
            Vector3 funnelOffset = CalculateFunnelOffset(boxIndex);
            
            // Halat uzunluğu: SABİT DEĞER - offset'ten bağımsız
            // baseRopeLength Inspector'dan ayarlanabilir
            float ropeLength = baseRopeLength;
            
            // Tüm kutular oyuncuya doğrudan bağlanır (zincirleme değil)
            box.AttachToPlayer(transform, funnelOffset, ropeLength, boxIndex);

            _attachedBoxes.Add(box);
            
            // Karakter evrimini tetikle
            if (_evolutionManager != null)
            {
                _evolutionManager.AdvanceToNextStage();
            }
            
            // Ağırlık güncelle
            UpdateCarriedWeight();
            
            // Halat sesi çal
            PlayAttachSound();
            
            Debug.Log($"Box attached! Total: {_attachedBoxes.Count}, Index: {boxIndex}, Funnel Offset: {funnelOffset}");
        }
        
        /// <summary>
        /// Kutu bağlandığında sesi çalar
        /// </summary>
        private void PlayAttachSound()
        {
            if (attachSound == null) return;
            
            // AudioSource yoksa oluştur
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D ses
                }
            }
            
            audioSource.PlayOneShot(attachSound);
        }
        
        /// <summary>
        /// Huni/yelpaze dizilimi için offset hesaplar.
        /// Kutular zigzag şeklinde yanlara açılarak dizilir:
        ///   Oyuncu
        ///     |
        ///   [0]  ← İlk kutu, tam arkada ortada
        ///  /   \
        /// [1]  [2]  ← Sol ve sağa açılıyor
        ///  |     |
        /// [3]  [4]  ← Daha arkada, daha açık
        /// </summary>
        private Vector3 CalculateFunnelOffset(int boxIndex)
        {
            if (boxIndex == 0)
            {
                // İlk kutu tam ortada, biraz arkada
                return new Vector3(0f, 0f, -funnelSpreadZ);
            }
            
            // Diğer kutular için: çift indeksler sağda, tek indeksler solda
            // boxIndex 1 -> sol 1. sıra
            // boxIndex 2 -> sağ 1. sıra  
            // boxIndex 3 -> sol 2. sıra
            // boxIndex 4 -> sağ 2. sıra
            
            int pairIndex = (boxIndex - 1) / 2; // Hangi "çift" te (0, 1, 2...)
            bool isLeft = (boxIndex % 2 == 1);  // Tek indeksler sol, çift indeksler sağ
            
            // X offset: Her çift için biraz daha dışa açıl
            float xMultiplier = pairIndex + 1;
            float xOffset = isLeft ? -xMultiplier * funnelSpreadX : xMultiplier * funnelSpreadX;
            
            // Z offset: Her kutu kendi sırasına göre arkaya
            float zOffset = -(boxIndex + 1) * funnelSpreadZ;
            
            return new Vector3(xOffset, 0f, zOffset);
        }


        /// <summary>
        /// Son bağlı kutuyu çözer
        /// </summary>
        public void DetachLastBox()
        {
            if (_attachedBoxes.Count == 0) return;

            DraggableBox lastBox = _attachedBoxes[_attachedBoxes.Count - 1];
            lastBox.Detach();
            _attachedBoxes.RemoveAt(_attachedBoxes.Count - 1);
            
            // Ağırlık güncelle
            UpdateCarriedWeight();
            
            Debug.Log($"Box detached! Remaining: {_attachedBoxes.Count}");
        }

        /// <summary>
        /// Tüm kutuları çözer
        /// </summary>
        public void DetachAllBoxes()
        {
            foreach (var box in _attachedBoxes)
            {
                if (box != null)
                {
                    box.Detach();
                }
            }
            _attachedBoxes.Clear();
            
            // Ağırlık güncelle
            UpdateCarriedWeight();
            
            Debug.Log("All boxes detached!");
        }

        /// <summary>
        /// Karakter kontrolücüsüne taşınan ağırlığı bildirir
        /// </summary>
        private void UpdateCarriedWeight()
        {
            if (_characterController != null)
            {
                _characterController.SetCarriedWeight(_attachedBoxes.Count);
            }
        }

        private void UpdateNearestBox()
        {
            _nearestBox = null;
            float nearestDistance = float.MaxValue;

            // Yakındaki tüm kutuları bul
            Collider[] colliders = Physics.OverlapSphere(
                transform.position, 
                detectionRadius, 
                boxLayer
            );

            foreach (var collider in colliders)
            {
                DraggableBox box = collider.GetComponent<DraggableBox>();
                
                if (box == null || box.IsAttached) continue;

                // Etkileşim noktasına olan mesafe (offset uygulanmış)
                float distance = Vector3.Distance(transform.position, box.InteractionPoint);
                
                // Etkileşim mesafesi kontrolü
                if (distance <= box.InteractionRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _nearestBox = box;
                }
            }
        }

        private void UpdateRopeVisual()
        {
            if (ropeVisualizer == null) return;

            if (_attachedBoxes.Count > 0)
            {
                ropeVisualizer.UpdateRope(AttachPoint, _attachedBoxes);
            }
            else
            {
                ropeVisualizer.HideRope();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Detection radius göster
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Attach point göster
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(AttachPoint, 0.15f);

            // Bağlı kutu sayısını göster (Editor'da)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f, 
                $"Attached: {_attachedBoxes?.Count ?? 0}"
            );
            #endif
        }
    }
}
