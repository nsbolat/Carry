using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Belirli bir yol üzerinde birden fazla NPC spawn eder.
/// Kaldırımda yürüyen kalabalık oluşturmak için kullanılır.
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Settings")]
    [Tooltip("Spawn edilecek NPC prefab'ları - eşit şekilde dağıtılır")]
    public List<GameObject> npcPrefabs = new List<GameObject>();
    
    [Tooltip("Spawn edilecek NPC sayısı")]
    public int npcCount = 5;
    
    [Header("Path Settings")]
    [Tooltip("NPC'lerin yürüyeceği yol")]
    public NPCPatrolPath patrolPath;
    
    [Tooltip("Manuel olarak tanımlanan A noktası (PatrolPath yoksa)")]
    public Transform manualPointA;
    
    [Tooltip("Manuel olarak tanımlanan B noktası (PatrolPath yoksa)")]
    public Transform manualPointB;
    
    [Header("Spawn Settings")]
    [Tooltip("Başlangıçta otomatik spawn et")]
    public bool spawnOnStart = true;
    
    [Tooltip("NPC'ler yol üzerinde rastgele pozisyonlarda mı başlasın")]
    public bool randomStartPositions = true;
    
    [Tooltip("Yoldan ne kadar yanlara dağılabilir (kaldırım genişliği) - Z ekseni")]
    public float lateralSpread = 1.0f;
    
    [Tooltip("Her NPC için benzersiz Z pozisyonu (çarpışma önlemek için)")]
    public bool uniqueZPositions = true;
    
    [Header("Speed Variation")]
    [Tooltip("Minimum yürüme hızı")]
    public float minWalkSpeed = 1.5f;
    
    [Tooltip("Maksimum yürüme hızı")]
    public float maxWalkSpeed = 2.5f;
    
    [Header("Random Stop Settings")]
    [Tooltip("NPC'ler yol boyunca rastgele dursun mu")]
    public bool enableRandomStops = true;
    
    [Tooltip("Rastgele durma olasılığı")]
    [Range(0f, 1f)]
    public float randomStopChance = 0.1f;
    
    [Tooltip("Minimum rastgele durma süresi")]
    public float minRandomStopTime = 1f;
    
    [Tooltip("Maksimum rastgele durma süresi")]
    public float maxRandomStopTime = 3f;
    
    [Header("Wait Time Variation (Uç Noktalarda)")]
    [Tooltip("NPC'ler uç noktalarda beklesin mi")]
    public bool enableWaiting = false;
    
    [Tooltip("Minimum bekleme süresi")]
    public float minWaitTime = 0f;
    
    [Tooltip("Maksimum bekleme süresi")]
    public float maxWaitTime = 2f;
    
    [Header("Animation Settings")]
    [Tooltip("Yürüme animasyon parametresi")]
    public string walkParameterName = "IsWalking";
    
    [Tooltip("Hız animasyon parametresi")]
    public string speedParameterName = "Speed";
    
    [Header("Ground Settings")]
    [Tooltip("NPC'leri yere yerleştirmek için raycast kullan")]
    public bool placeOnGround = true;
    
    [Tooltip("Zemin layer mask'ı")]
    public LayerMask groundLayer = ~0; // Default: Everything
    
    [Tooltip("Raycast başlangıç yüksekliği")]
    public float raycastHeight = 10f;
    
    [Tooltip("NPC'nin yerden yüksekliği (pivot offset)")]
    public float groundOffset = 0f;
    
    // Spawn edilen NPC'leri tutmak için
    private List<GameObject> spawnedNPCs = new List<GameObject>();
    
    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnNPCs();
        }
    }
    
    /// <summary>
    /// Belirlenen sayıda NPC spawn eder
    /// </summary>
    [ContextMenu("Spawn NPCs")]
    public void SpawnNPCs()
    {
        if (npcPrefabs == null || npcPrefabs.Count == 0)
        {
            Debug.LogError("[NPCSpawner] NPC Prefab listesi boş!");
            return;
        }
        
        // Null prefabları filtrele
        npcPrefabs.RemoveAll(p => p == null);
        if (npcPrefabs.Count == 0)
        {
            Debug.LogError("[NPCSpawner] NPC Prefab listesinde geçerli prefab yok!");
            return;
        }
        
        // Waypoint'leri belirle
        Transform pointA = patrolPath != null ? patrolPath.startPoint : manualPointA;
        Transform pointB = patrolPath != null ? patrolPath.endPoint : manualPointB;
        
        if (pointA == null || pointB == null)
        {
            Debug.LogError("[NPCSpawner] Waypoint'ler atanmamış! PatrolPath veya manuel noktalar gerekli.");
            return;
        }
        
        Vector3 pathDirection = (pointB.position - pointA.position).normalized;
        Vector3 lateralDirection = Vector3.Cross(Vector3.up, pathDirection).normalized;
        
        for (int i = 0; i < npcCount; i++)
        {
            // Spawn pozisyonunu hesapla
            Vector3 spawnPosition;
            
            if (randomStartPositions)
            {
                float t = Random.Range(0f, 1f);
                spawnPosition = Vector3.Lerp(pointA.position, pointB.position, t);
            }
            else
            {
                // Eşit aralıklarla dağıt
                float t = npcCount > 1 ? (float)i / (npcCount - 1) : 0.5f;
                spawnPosition = Vector3.Lerp(pointA.position, pointB.position, t);
            }
            
            // Z ekseni dağılımı - her NPC için benzersiz pozisyon
            if (lateralSpread > 0)
            {
                float lateralOffset;
                
                if (uniqueZPositions && npcCount > 1)
                {
                    // Her NPC için benzersiz Z pozisyonu (eşit aralıklı)
                    float normalizedIndex = (float)i / (npcCount - 1); // 0 ile 1 arası
                    lateralOffset = Mathf.Lerp(-lateralSpread, lateralSpread, normalizedIndex);
                    
                    // Biraz rastgelelik ekle ama çarpışmayacak kadar
                    float jitter = lateralSpread * 0.1f;
                    lateralOffset += Random.Range(-jitter, jitter);
                }
                else
                {
                    lateralOffset = Random.Range(-lateralSpread, lateralSpread);
                }
                
                spawnPosition += lateralDirection * lateralOffset;
            }
            
            // Yere yerleştir (raycast ile)
            if (placeOnGround)
            {
                Vector3 rayStart = spawnPosition + Vector3.up * raycastHeight;
                RaycastHit hit;
                // QueryTriggerInteraction.Ignore: Trigger collider'ları (kamera zone vs.) yok say
                if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    spawnPosition.y = hit.point.y + groundOffset;
                }
                else
                {
                    Debug.LogWarning($"[NPCSpawner] NPC_{i + 1}: Zemin bulunamadı, waypoint yüksekliği kullanılıyor.");
                    spawnPosition.y = pointA.position.y + groundOffset;
                }
            }
            
            // Prefab seçimi - eşit dağılım için index'e göre seç
            int prefabIndex = i % npcPrefabs.Count;
            GameObject selectedPrefab = npcPrefabs[prefabIndex];
            
            // NPC'yi spawn et
            GameObject npc = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity, transform);
            npc.name = $"NPC_{i + 1}_V{prefabIndex + 1}";
            
            // NPCPatrolWalker bileşenini ayarla
            NPCPatrolWalker walker = npc.GetComponent<NPCPatrolWalker>();
            if (walker == null)
            {
                walker = npc.AddComponent<NPCPatrolWalker>();
            }
            
            walker.pointA = pointA;
            walker.pointB = pointB;
            
            // Rastgele hız
            walker.walkSpeed = Random.Range(minWalkSpeed, maxWalkSpeed);
            
            // Animasyon parametrelerini ayarla
            walker.walkParameterName = walkParameterName;
            walker.speedParameterName = speedParameterName;
            
            // Animator'ü bul ve ata
            Animator anim = npc.GetComponent<Animator>();
            if (anim == null)
            {
                anim = npc.GetComponentInChildren<Animator>();
            }
            if (anim != null)
            {
                walker.animator = anim;
                
                // Animasyonu hemen başlat
                if (!string.IsNullOrEmpty(walkParameterName))
                {
                    // Parametrenin var olup olmadığını kontrol et
                    bool hasParameter = false;
                    foreach (var param in anim.parameters)
                    {
                        if (param.name == walkParameterName && param.type == AnimatorControllerParameterType.Bool)
                        {
                            hasParameter = true;
                            break;
                        }
                    }
                    
                    if (hasParameter)
                    {
                        anim.SetBool(walkParameterName, true);
                        
                        // Rastgele animasyon offset'i - ordu gibi yürümeyi önler
                        // Bir frame sonra animasyonu rastgele bir noktadan başlat
                        float randomOffset = Random.Range(0f, 1f);
                        StartCoroutine(SetRandomAnimationOffset(anim, randomOffset));
                        
                        Debug.Log($"[NPCSpawner] {npc.name}: Animator atandı, {walkParameterName}=true, offset={randomOffset:F2}");
                    }
                    else
                    {
                        Debug.LogWarning($"[NPCSpawner] {npc.name}: Animator'de '{walkParameterName}' (bool) parametresi YOK! Animator Controller'ı kontrol et.");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[NPCSpawner] {npc.name}: Animator component'i bulunamadı!");
            }
            
            // Rastgele durma ayarları
            walker.enableRandomStops = enableRandomStops;
            walker.randomStopChance = randomStopChance;
            walker.minRandomStopTime = minRandomStopTime;
            walker.maxRandomStopTime = maxRandomStopTime;
            
            // Her NPC için farklı bir başlangıç zamanı (hepsi aynı anda durmasın)
            walker.stopCheckInterval = Random.Range(1.5f, 3f);
            
            // Uç nokta bekleme ayarları
            if (enableWaiting)
            {
                walker.waitTimeAtPoints = Random.Range(minWaitTime, maxWaitTime);
                walker.randomizeWaitTime = true;
                walker.minRandomWait = 0f;
                walker.maxRandomWait = maxWaitTime - minWaitTime;
            }
            
            // Rastgele yön (yarısı A'ya, yarısı B'ye)
            if (Random.value > 0.5f)
            {
                npc.transform.rotation = Quaternion.LookRotation(-pathDirection);
            }
            else
            {
                npc.transform.rotation = Quaternion.LookRotation(pathDirection);
            }
            
            spawnedNPCs.Add(npc);
        }
        
        Debug.Log($"[NPCSpawner] {npcCount} NPC spawn edildi.");
    }
    
    /// <summary>
    /// Animasyonu rastgele bir noktadan başlatır - ordu gibi yürümeyi önler
    /// </summary>
    private System.Collections.IEnumerator SetRandomAnimationOffset(Animator anim, float normalizedOffset)
    {
        // Bir frame bekle - animasyon state'inin oluşması için
        yield return null;
        
        if (anim == null)
            yield break;
        
        // Mevcut state'i al ve rastgele bir noktadan başlat
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        anim.Play(stateInfo.fullPathHash, 0, normalizedOffset);
    }
    
    /// <summary>
    /// Tüm spawn edilmiş NPC'leri temizler
    /// </summary>
    [ContextMenu("Clear NPCs")]
    public void ClearNPCs()
    {
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(npc);
                }
                else
                {
                    DestroyImmediate(npc);
                }
            }
        }
        spawnedNPCs.Clear();
        Debug.Log("[NPCSpawner] Tüm NPC'ler temizlendi.");
    }
    
    /// <summary>
    /// Tüm NPC'leri durdurur
    /// </summary>
    public void StopAllNPCs()
    {
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null)
            {
                var walker = npc.GetComponent<NPCPatrolWalker>();
                if (walker != null)
                {
                    walker.StopMoving();
                }
            }
        }
    }
    
    /// <summary>
    /// Tüm NPC'leri tekrar başlatır
    /// </summary>
    public void StartAllNPCs()
    {
        foreach (var npc in spawnedNPCs)
        {
            if (npc != null)
            {
                var walker = npc.GetComponent<NPCPatrolWalker>();
                if (walker != null)
                {
                    walker.StartMoving();
                }
            }
        }
    }
    
    /// <summary>
    /// Spawn edilmiş NPC listesini döndürür
    /// </summary>
    public List<GameObject> GetSpawnedNPCs()
    {
        return new List<GameObject>(spawnedNPCs);
    }
    
    private void OnDrawGizmos()
    {
        // Spawn alanını görselleştir
        Transform pointA = patrolPath != null ? patrolPath.startPoint : manualPointA;
        Transform pointB = patrolPath != null ? patrolPath.endPoint : manualPointB;
        
        if (pointA == null || pointB == null)
            return;
        
        Vector3 pathDirection = (pointB.position - pointA.position).normalized;
        Vector3 lateralDirection = Vector3.Cross(Vector3.up, pathDirection).normalized;
        
        // Kaldırım alanını çiz
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        
        Vector3 corner1 = pointA.position + lateralDirection * lateralSpread;
        Vector3 corner2 = pointA.position - lateralDirection * lateralSpread;
        Vector3 corner3 = pointB.position - lateralDirection * lateralSpread;
        Vector3 corner4 = pointB.position + lateralDirection * lateralSpread;
        
        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);
        
        // NPC pozisyonlarını önizle
        if (uniqueZPositions && npcCount > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < npcCount; i++)
            {
                float normalizedIndex = (float)i / (npcCount - 1);
                float lateralOffset = Mathf.Lerp(-lateralSpread, lateralSpread, normalizedIndex);
                Vector3 previewPos = (pointA.position + pointB.position) / 2f + lateralDirection * lateralOffset;
                Gizmos.DrawWireSphere(previewPos, 0.2f);
            }
        }
    }
}
