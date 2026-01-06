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

        [Header("Interaction")]
        [Tooltip("Etkileşim için kutu arama yarıçapı")]
        [SerializeField] private float detectionRadius = 3f;
        
        [Tooltip("Etkileşim için layer mask")]
        [SerializeField] private LayerMask boxLayer;

        [Header("Visual")]
        [Tooltip("Halat görselleştirici (opsiyonel)")]
        [SerializeField] private RopeVisualizer ropeVisualizer;

        // Bağlı kutuların listesi (sıralı - ilk oyuncuya bağlı)
        private List<DraggableBox> _attachedBoxes = new List<DraggableBox>();
        
        // En yakın etkileşilebilir kutu (highlight için)
        private DraggableBox _nearestBox;
        
        // Karakter kontrolcüsü referansı (ağırlık sistemi için)
        private SlopeCharacterController _characterController;

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
        #endregion

        private void Awake()
        {
            _characterController = GetComponent<SlopeCharacterController>();
        }

        private void Update()
        {
            UpdateNearestBox();
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
        /// Belirli bir kutuyu bağlar
        /// </summary>
        public void AttachBox(DraggableBox box)
        {
            if (box == null || box.IsAttached) return;
            if (!CanAttachMore) return;

            // Bağlantı hedefini belirle
            if (_attachedBoxes.Count == 0)
            {
                // İlk kutu - oyuncuya bağla
                box.AttachTo(transform, null);
            }
            else
            {
                // Sonraki kutular - son kutuya bağla
                DraggableBox lastBox = _attachedBoxes[_attachedBoxes.Count - 1];
                box.AttachTo(lastBox.transform, lastBox);
            }

            _attachedBoxes.Add(box);
            
            // Ağırlık güncelle
            UpdateCarriedWeight();
            
            Debug.Log($"Box attached! Total: {_attachedBoxes.Count}");
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

                float distance = Vector3.Distance(transform.position, box.transform.position);
                
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
