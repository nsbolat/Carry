using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Bir patrol yolunu temsil eder. Birden fazla NPC aynı yolu kullanabilir.
/// Sahnede waypoint'leri görselleştirir ve kolay yerleştirme sağlar.
/// </summary>
public class NPCPatrolPath : MonoBehaviour
{
    [Header("Path Points")]
    [Tooltip("Yolun başlangıç noktası")]
    public Transform startPoint;
    
    [Tooltip("Yolun bitiş noktası")]
    public Transform endPoint;
    
    [Header("Visualization")]
    public Color pathColor = Color.yellow;
    public Color startPointColor = Color.green;
    public Color endPointColor = Color.red;
    public float pointRadius = 0.5f;
    
    [Header("Auto Setup")]
    [Tooltip("True ise otomatik olarak child objelerden nokta oluşturur")]
    public bool autoCreatePoints = false;
    
    private void OnValidate()
    {
        if (autoCreatePoints && startPoint == null && endPoint == null)
        {
            CreateChildPoints();
        }
    }
    
    [ContextMenu("Create Child Points")]
    public void CreateChildPoints()
    {
        // Start Point
        if (startPoint == null)
        {
            GameObject startObj = new GameObject("StartPoint");
            startObj.transform.SetParent(transform);
            startObj.transform.localPosition = Vector3.left * 5f;
            startPoint = startObj.transform;
        }
        
        // End Point
        if (endPoint == null)
        {
            GameObject endObj = new GameObject("EndPoint");
            endObj.transform.SetParent(transform);
            endObj.transform.localPosition = Vector3.right * 5f;
            endPoint = endObj.transform;
        }
    }
    
    /// <summary>
    /// Bu yola bir NPC atar
    /// </summary>
    public void AssignToNPC(NPCPatrolWalker npc)
    {
        if (npc != null)
        {
            npc.pointA = startPoint;
            npc.pointB = endPoint;
        }
    }
    
    /// <summary>
    /// Yolun ortasındaki pozisyonu döndürür
    /// </summary>
    public Vector3 GetMidPoint()
    {
        if (startPoint != null && endPoint != null)
        {
            return (startPoint.position + endPoint.position) / 2f;
        }
        return transform.position;
    }
    
    /// <summary>
    /// Yolun uzunluğunu döndürür
    /// </summary>
    public float GetPathLength()
    {
        if (startPoint != null && endPoint != null)
        {
            return Vector3.Distance(startPoint.position, endPoint.position);
        }
        return 0f;
    }
    
    /// <summary>
    /// Yol üzerinde rastgele bir pozisyon döndürür
    /// </summary>
    public Vector3 GetRandomPointOnPath()
    {
        if (startPoint != null && endPoint != null)
        {
            float t = Random.Range(0f, 1f);
            return Vector3.Lerp(startPoint.position, endPoint.position, t);
        }
        return transform.position;
    }
    
    private void OnDrawGizmos()
    {
        // Start point
        if (startPoint != null)
        {
            Gizmos.color = startPointColor;
            Gizmos.DrawWireSphere(startPoint.position, pointRadius);
            Gizmos.DrawSphere(startPoint.position, pointRadius * 0.3f);
        }
        
        // End point
        if (endPoint != null)
        {
            Gizmos.color = endPointColor;
            Gizmos.DrawWireSphere(endPoint.position, pointRadius);
            Gizmos.DrawSphere(endPoint.position, pointRadius * 0.3f);
        }
        
        // Path line
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = pathColor;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            
            // Yön okları çiz
            Vector3 direction = (endPoint.position - startPoint.position).normalized;
            Vector3 midPoint = GetMidPoint();
            DrawArrow(midPoint, direction, 0.5f);
        }
    }
    
    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;
            
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        
        Vector3 arrowTip = position + direction * size;
        Vector3 arrowLeft = position - direction * size * 0.3f + right * size * 0.3f;
        Vector3 arrowRight = position - direction * size * 0.3f - right * size * 0.3f;
        
        Gizmos.DrawLine(position, arrowTip);
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(NPCPatrolPath))]
public class NPCPatrolPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        NPCPatrolPath path = (NPCPatrolPath)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Child Points"))
        {
            path.CreateChildPoints();
            EditorUtility.SetDirty(path);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Path Info", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Length: {path.GetPathLength():F2} units");
    }
}
#endif
