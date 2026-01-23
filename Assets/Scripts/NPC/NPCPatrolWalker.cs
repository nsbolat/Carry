using UnityEngine;

/// <summary>
/// NPC'nin iki waypoint arasında ileri geri yürümesini sağlar.
/// Kaldırımda yürüyen NPC'ler için tasarlanmıştır.
/// </summary>
public class NPCPatrolWalker : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Başlangıç noktası (Transform)")]
    public Transform pointA;
    
    [Tooltip("Bitiş noktası (Transform)")]
    public Transform pointB;
    
    [Header("Movement Settings")]
    [Tooltip("Yürüme hızı")]
    public float walkSpeed = 2f;
    
    [Tooltip("Dönüş hızı (derece/saniye)")]
    public float rotationSpeed = 180f;
    
    [Tooltip("Hedefe ne kadar yaklaşınca dönmeli")]
    public float arrivalThreshold = 0.3f;
    
    [Header("Animation")]
    [Tooltip("Animator bileşeni (opsiyonel)")]
    public Animator animator;
    
    [Tooltip("Yürüme animasyon parametresi")]
    public string walkParameterName = "IsWalking";
    
    [Tooltip("Hız animasyon parametresi (blend tree için)")]
    public string speedParameterName = "Speed";
    
    [Header("Wait Settings - Uç Noktalarda")]
    [Tooltip("Her noktada bekleme süresi (0 = beklemeden devam et)")]
    public float waitTimeAtPoints = 0f;
    
    [Tooltip("Rastgele bekleme süresi ekle")]
    public bool randomizeWaitTime = false;
    
    [Tooltip("Minimum rastgele bekleme")]
    public float minRandomWait = 0f;
    
    [Tooltip("Maksimum rastgele bekleme")]
    public float maxRandomWait = 2f;
    
    [Header("Random Stop - Yol Boyunca Rastgele Durma")]
    [Tooltip("Yol boyunca rastgele dursun mu?")]
    public bool enableRandomStops = true;
    
    [Tooltip("Rastgele durma olasılığı (her saniye)")]
    [Range(0f, 1f)]
    public float randomStopChance = 0.1f;
    
    [Tooltip("Minimum rastgele durma süresi")]
    public float minRandomStopTime = 1f;
    
    [Tooltip("Maksimum rastgele durma süresi")]
    public float maxRandomStopTime = 3f;
    
    [Tooltip("Durma kontrolleri arası minimum süre")]
    public float stopCheckInterval = 2f;
    
    [Header("Ground Following - Eğimli Yollarda")]
    [Tooltip("Zemine yapışarak yürüsün mü")]
    public bool followGround = true;
    
    [Tooltip("Zemin layer mask'ı")]
    public LayerMask groundLayer = ~0;
    
    [Tooltip("Raycast yüksekliği")]
    public float groundRayHeight = 2f;
    
    [Tooltip("Yerden yükseklik offset'i")]
    public float groundOffset = 0f;
    
    [Tooltip("Y pozisyonu yumuşatma hızı")]
    public float groundSmoothSpeed = 10f;
    
    [Header("Debug")]
    [Tooltip("Gizmo'ları göster")]
    public bool showGizmos = true;
    
    // Private variables
    private Transform currentTarget;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isMoving = true;
    private float nextStopCheckTime = 0f;
    private bool isRandomStopping = false;
    
    private void Start()
    {
        // Eğer animator atanmamışsa, bileşende ara
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Debug: Animator durumunu kontrol et
        if (animator != null)
        {
            Debug.Log($"[NPCPatrolWalker] {gameObject.name}: Animator bulundu - {animator.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[NPCPatrolWalker] {gameObject.name}: Animator BULUNAMADI!");
        }
        
        // Başlangıç hedefini belirle - en yakın noktaya git
        if (pointA != null && pointB != null)
        {
            float distToA = Vector3.Distance(transform.position, pointA.position);
            float distToB = Vector3.Distance(transform.position, pointB.position);
            currentTarget = distToA < distToB ? pointB : pointA;
        }
        else if (pointA != null)
        {
            currentTarget = pointA;
        }
        else if (pointB != null)
        {
            currentTarget = pointB;
        }
        else
        {
            Debug.LogWarning($"[NPCPatrolWalker] {gameObject.name}: Waypoint'ler atanmamış!");
            isMoving = false;
        }
        
        // Başlangıçta animasyonu aktif et
        if (isMoving)
        {
            UpdateAnimation(true);
        }
    }
    
    private void Update()
    {
        if (!isMoving || currentTarget == null)
            return;
        
        // Bekleme kontrolü (hem uç nokta hem rastgele durma)
        if (isWaiting || isRandomStopping)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                isRandomStopping = false;
                UpdateAnimation(true);
            }
            return;
        }
        
        // Rastgele durma kontrolü
        if (enableRandomStops && Time.time >= nextStopCheckTime)
        {
            nextStopCheckTime = Time.time + stopCheckInterval;
            
            if (Random.value < randomStopChance)
            {
                // Rastgele dur
                isRandomStopping = true;
                waitTimer = Random.Range(minRandomStopTime, maxRandomStopTime);
                UpdateAnimation(false);
                return;
            }
        }
        
        // Hedefe doğru hareket
        MoveTowardsTarget();
        
        // Hedefe ulaşıldı mı kontrolü
        CheckArrival();
    }
    
    private void MoveTowardsTarget()
    {
        Vector3 currentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPos = new Vector3(currentTarget.position.x, 0, currentTarget.position.z);
        Vector3 direction = targetPos - currentPos;
        float distance = direction.magnitude;
        
        // Hedefe çok yakınsa hareket etme
        if (distance < arrivalThreshold)
        {
            return;
        }
        
        direction = direction.normalized;
        
        // Hedefe doğru dön
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );
        
        // Sadece doğru yöne bakıyorsa hareket et (dönüş tamamlandıysa)
        float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleToTarget < 45f)
        {
            // Yatay hareket
            Vector3 movement = transform.forward * walkSpeed * Time.deltaTime;
            Vector3 newPos = transform.position + movement;
            
            // Eğimli yolda zemine yapış
            if (followGround)
            {
                newPos.y = GetGroundHeight(newPos);
            }
            
            transform.position = newPos;
        }
    }
    
    /// <summary>
    /// Belirtilen pozisyondaki zemin yüksekliğini döndürür
    /// </summary>
    private float GetGroundHeight(Vector3 position)
    {
        Vector3 rayStart = position + Vector3.up * groundRayHeight;
        RaycastHit hit;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundRayHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y + groundOffset;
            // Y pozisyonunu yumuşak geçiş ile güncelle
            return Mathf.Lerp(transform.position.y, targetY, groundSmoothSpeed * Time.deltaTime);
        }
        
        return transform.position.y; // Zemin bulunamazsa mevcut yüksekliği koru
    }
    
    private void CheckArrival()
    {
        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(currentTarget.position.x, 0, currentTarget.position.z)
        );
        
        if (distance <= arrivalThreshold)
        {
            // Hedefi değiştir
            SwitchTarget();
            
            // Bekleme süresini ayarla
            float waitTime = waitTimeAtPoints;
            if (randomizeWaitTime)
            {
                waitTime += Random.Range(minRandomWait, maxRandomWait);
            }
            
            if (waitTime > 0)
            {
                isWaiting = true;
                waitTimer = waitTime;
                UpdateAnimation(false);
            }
        }
    }
    
    private void SwitchTarget()
    {
        currentTarget = (currentTarget == pointA) ? pointB : pointA;
    }
    
    private void UpdateAnimation(bool walking)
    {
        if (animator == null)
            return;
        
        // Bool parametresi
        if (!string.IsNullOrEmpty(walkParameterName))
        {
            animator.SetBool(walkParameterName, walking);
        }
        
        // Float parametresi (blend tree için)
        if (!string.IsNullOrEmpty(speedParameterName))
        {
            animator.SetFloat(speedParameterName, walking ? walkSpeed : 0f);
        }
    }
    
    /// <summary>
    /// NPC'yi durdur
    /// </summary>
    public void StopMoving()
    {
        isMoving = false;
        UpdateAnimation(false);
    }
    
    /// <summary>
    /// NPC'yi tekrar başlat
    /// </summary>
    public void StartMoving()
    {
        isMoving = true;
        isWaiting = false;
        UpdateAnimation(true);
    }
    
    /// <summary>
    /// Yürüme hızını değiştir
    /// </summary>
    public void SetWalkSpeed(float newSpeed)
    {
        walkSpeed = newSpeed;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;
        
        // Waypoint'leri çiz
        if (pointA != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.DrawIcon(pointA.position, "d_Animation.FirstKey", true);
        }
        
        if (pointB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
            Gizmos.DrawIcon(pointB.position, "d_Animation.LastKey", true);
        }
        
        // Yolu çiz
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
        
        // Mevcut hedefi göster
        if (Application.isPlaying && currentTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Seçiliyken arrival threshold'u göster
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, arrivalThreshold);
    }
}
