using UnityEngine;

namespace Sisifos.Interaction
{
    /// <summary>
    /// Sürüklenebilir kutu component'i.
    /// Fizik tabanlı yuvarlanma ile oyuncuyu takip eder.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DraggableBox : MonoBehaviour
    {
        [Header("Drag Settings")]
        [Tooltip("Halatın kutuya bağlandığı nokta offset")]
        [SerializeField] private Vector3 attachPointOffset = new Vector3(0f, 0.5f, 0f);
        
        [Tooltip("Kutunun çekilme kuvveti")]
        [SerializeField] private float pullForce = 50f;
        
        [Tooltip("Maksimum hız")]
        [SerializeField] private float maxSpeed = 12f;
        
        [Tooltip("Kutunun bir önceki noktaya olan mesafesi")]
        [SerializeField] private float ropeLength = 1.5f;
        
        [Tooltip("Halat gerginlik mesafesi (bu mesafeden sonra çekme başlar)")]
        [SerializeField] private float tensionDistance = 0.2f;

        [Header("Physics")]
        [Tooltip("Fizik materyali (sürtünme için)")]
        [SerializeField] private PhysicsMaterial physicsMaterial;
        
        [Tooltip("Kütle - daha yüksek = daha ağır hissiyat")]
        [SerializeField] private float mass = 3f;
        
        [Tooltip("Lineer sürükleme - hız kaybı")]
        [SerializeField] private float linearDrag = 1f;
        
        [Tooltip("Açısal sürükleme (yuvarlanma direnci)")]
        [SerializeField] private float angularDrag = 1f;
        
        [Tooltip("Hız sönümleme - ani hareketleri yumuşatır (1 = sönümleme yok)")]
        [SerializeField] private float velocityDamping = 0.98f;

        [Header("Interaction")]
        [Tooltip("Oyuncunun kutuyu bağlayabilmesi için maksimum mesafe")]
        [SerializeField] private float interactionRange = 2f;

        // State
        private bool _isAttached;
        private DraggableBox _previousBox;
        private Transform _followTarget;
        private Rigidbody _rigidbody;
        private Collider _collider;
        private Vector3 _previousTargetPos;
        private float _currentTension; // 0-1 arası gerginlik değeri
        
        // Huni dizilimi için yeni alanlar
        private Vector3 _funnelOffset; // Oyuncuya göre hedef offset (huni pozisyonu)
        private float _dynamicRopeLength; // Dinamik halat uzunluğu
        private int _boxIndex; // Kutu indeksi (0 = ilk kutu)

        #region Properties
        public bool IsAttached => _isAttached;
        public Vector3 AttachPoint => transform.position + transform.TransformDirection(attachPointOffset);
        
        /// <summary>
        /// Halat uzunluğu - her kutunun kendi Inspector değerini kullanır
        /// </summary>
        public float RopeLength => ropeLength;
        
        public float InteractionRange => interactionRange;
        public int BoxIndex => _boxIndex;
        
        /// <summary>
        /// Halatın gerginlik değeri (0 = gevşek, 1 = maksimum gergin)
        /// </summary>
        public float Tension => _currentTension;
        
        /// <summary>
        /// Rotasyondan bağımsız stabil bağlantı noktası.
        /// Kutu dönerken ipin sapıtmamasını sağlar.
        /// </summary>
        public Vector3 GetStableAttachPoint()
        {
            // Sadece Y offset'i kullan, X ve Z dünya koordinatlarında sabit
            return transform.position + new Vector3(0f, attachPointOffset.y, 0f);
        }
        #endregion

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            SetupRigidbody();
        }

        private void SetupRigidbody()
        {
            // Dinamik rigidbody - fizik simülasyonu aktif
            _rigidbody.isKinematic = false;
            _rigidbody.mass = mass;
            _rigidbody.linearDamping = linearDrag;
            _rigidbody.angularDamping = angularDrag;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Freeze Z ekseni (side-scroller)
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionZ;

            // Fizik materyali uygula
            if (physicsMaterial != null)
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.material = physicsMaterial;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!_isAttached || _followTarget == null) return;
            
            ApplyPullForce();
            ApplyVelocityDamping();
        }

        /// <summary>
        /// Kutuyu bir hedefe bağlar (eski zincirleme sistem - geriye uyumluluk)
        /// </summary>
        public void AttachTo(Transform target, DraggableBox previousBox = null)
        {
            _followTarget = target;
            _previousBox = previousBox;
            _isAttached = true;
            _funnelOffset = Vector3.zero;
            _dynamicRopeLength = ropeLength;
            _boxIndex = 0;
            
            // Bağlandığında hızı sıfırla ve wake up
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.WakeUp();
            
            // Hedef pozisyonunu kaydet
            _previousTargetPos = GetTargetAnchor();
            
            // Önceki kutuyla çarpışmayı ignore et (zincirleme kutular birbirini engellemesin)
            if (_previousBox != null && _previousBox._collider != null && _collider != null)
            {
                Physics.IgnoreCollision(_collider, _previousBox._collider, true);
            }
        }
        
        /// <summary>
        /// Kutuyu oyuncuya doğrudan bağlar - huni dizilimi için
        /// </summary>
        /// <param name="playerTransform">Oyuncu transform'u</param>
        /// <param name="funnelOffset">Huni diziliminde bu kutunun offset'i</param>
        /// <param name="customRopeLength">Bu kutu için halat uzunluğu</param>
        /// <param name="boxIndex">Kutu indeksi (0 = ilk kutu)</param>
        public void AttachToPlayer(Transform playerTransform, Vector3 funnelOffset, float customRopeLength, int boxIndex)
        {
            _followTarget = playerTransform;
            _previousBox = null; // Tüm kutular oyuncuya direkt bağlı
            _isAttached = true;
            _funnelOffset = funnelOffset;
            _dynamicRopeLength = customRopeLength;
            _boxIndex = boxIndex;
            
            // Bağlandığında hızı sıfırla ve wake up
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.WakeUp();
            
            // Hedef pozisyonunu kaydet
            _previousTargetPos = GetTargetAnchor();
            
            Debug.Log($"[DraggableBox] Attached to player - Index: {boxIndex}, Offset: {funnelOffset}, RopeLength: {customRopeLength}");
        }

        /// <summary>
        /// Kutuyu serbest bırakır
        /// </summary>
        public void Detach()
        {
            _followTarget = null;
            _previousBox = null;
            _isAttached = false;
            _funnelOffset = Vector3.zero;
            _dynamicRopeLength = 0f;
            _boxIndex = 0;
        }

        /// <summary>
        /// Hedef anchor noktasını hesaplar.
        /// Tüm kutular oyuncuya doğru çekilir - rotasyondan bağımsız.
        /// </summary>
        private Vector3 GetTargetAnchor()
        {
            if (_followTarget == null) return transform.position;
            
            // Sadece oyuncu pozisyonunu döndür - rotasyondan bağımsız
            // Kutular doğal halat gibi arkadan sürüklenecek
            return _followTarget.position;
        }

        private void ApplyPullForce()
        {
            Vector3 targetAnchor = GetTargetAnchor();

            // Kutudan hedefe olan vektör
            Vector3 toTarget = targetAnchor - AttachPoint;
            float distance = toTarget.magnitude;

            // Gerginlik hesapla
            float currentRopeLength = RopeLength;
            float excessDistance = Mathf.Max(0, distance - currentRopeLength);
            _currentTension = Mathf.Clamp01(excessDistance / 2f);

            // Eğer halat gergin değilse kuvvet uygulama
            if (distance <= currentRopeLength + tensionDistance)
            {
                return;
            }
            
            // Çekme yönü
            Vector3 pullDirection = toTarget.normalized;
            
            // Kuvvet hesapla - yumuşak spring
            float forceMagnitude = pullForce * Mathf.Sqrt(excessDistance);

            // Kuvveti uygula
            _rigidbody.AddForce(pullDirection * forceMagnitude, ForceMode.Force);

            // Sadece aşırı hızı sınırla
            if (_rigidbody.linearVelocity.magnitude > maxSpeed)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * maxSpeed;
            }
        }
        
        private void ApplyVelocityDamping()
        {
            // Hafif damping - titreme önlemek için çok düşük tut
            _rigidbody.linearVelocity *= velocityDamping;
        }

        private void OnDrawGizmosSelected()
        {
            // Attach point göster
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(AttachPoint, 0.1f);

            // Interaction range göster
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // Rope length göster
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, ropeLength);

            // Eğer bağlıysa, bağlantı çizgisi göster
            if (_isAttached && _followTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 targetAnchor = _previousBox != null 
                    ? _previousBox.AttachPoint 
                    : _followTarget.position;
                Gizmos.DrawLine(AttachPoint, targetAnchor);
            }
        }
    }
}
