using UnityEngine;
using System.Collections.Generic;

namespace Sisifos.Interaction
{
    /// <summary>
    /// Verlet Integration kullanarak gerçekçi halat fiziği simülasyonu yapar.
    /// Her kutu için ayrı LineRenderer oluşturur - huni dizilimi için optimize edilmiş.
    /// </summary>
    public class RopeVisualizer : MonoBehaviour
    {
        [Header("Rope Settings")]
        [Tooltip("Her bağlantı için segment sayısı (daha fazla = daha yumuşak)")]
        [SerializeField] private int segmentsPerConnection = 10;
        
        [Tooltip("Halatın doğal sarkma miktarı")]
        [SerializeField] private float sagAmount = 0.5f;
        
        [Header("Physics")]
        [Tooltip("Yerçekimi kuvveti")]
        [SerializeField] private float gravity = 9.8f;
        
        [Tooltip("Hava direnci (0-1 arası, yüksek = daha az salınım)")]
        [SerializeField, Range(0f, 1f)] private float damping = 0.15f;
        
        [Tooltip("İpin sertliği (constraint çözüm iterasyonu)")]
        [SerializeField, Range(1, 50)] private int constraintIterations = 25;
        
        [Tooltip("Simülasyon hızı")]
        [SerializeField] private float simulationSpeed = 1f;
        
        [Header("Smoothing")]
        [Tooltip("Segment pozisyon yumuşatma hızı (yüksek = daha hızlı takip)")]
        [SerializeField, Range(1f, 30f)] private float positionSmoothSpeed = 15f;
        
        [Tooltip("Uzunluk değişim yumuşatma hızı")]
        [SerializeField, Range(1f, 20f)] private float lengthSmoothSpeed = 8f;
        
        [Header("Visual")]
        [SerializeField] private float ropeWidth = 0.05f;
        [SerializeField] private Color ropeColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Material ropeMaterial;
        
        [Header("Texture Settings")]
        [Tooltip("Texture'un halat üzerinde kaç kez tekrarlanacağı (X: uzunluk, Y: genişlik)")]
        [SerializeField] private Vector2 textureScale = new Vector2(2f, 1f);

        // Her kutu için ayrı LineRenderer ve bağlantı verisi
        private List<RopeConnection> _connections = new List<RopeConnection>();
        
        // Materyal instance (paylaşımlı)
        private Material _sharedMaterial;

        private class RopeConnection
        {
            public LineRenderer lineRenderer;
            public List<RopeSegment> segments = new List<RopeSegment>();
            public float targetRopeLength;
            public float currentRopeLength;
            public bool isInitialized;
            public Vector3 smoothedStartPos;
            public Vector3 smoothedEndPos;
            public Vector3 actualStartPos;
            public Vector3 actualEndPos;
        }

        private struct RopeSegment
        {
            public Vector3 currentPos;
            public Vector3 previousPos;

            public RopeSegment(Vector3 pos)
            {
                currentPos = pos;
                previousPos = pos;
            }
        }

        private void Awake()
        {
            CreateSharedMaterial();
        }

        private void CreateSharedMaterial()
        {
            if (ropeMaterial != null)
            {
                _sharedMaterial = new Material(ropeMaterial);
            }
            else
            {
                // URP için varsayılan Lit shader
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader != null)
                {
                    _sharedMaterial = new Material(litShader);
                    _sharedMaterial.SetColor("_BaseColor", ropeColor);
                }
                else
                {
                    _sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                    _sharedMaterial.color = ropeColor;
                }
            }
        }

        /// <summary>
        /// Yeni bir LineRenderer oluşturur ve ayarlar
        /// </summary>
        private LineRenderer CreateLineRenderer(int index)
        {
            GameObject ropeObj = new GameObject($"Rope_{index}");
            ropeObj.transform.SetParent(transform);
            ropeObj.transform.localPosition = Vector3.zero;
            
            LineRenderer lr = ropeObj.AddComponent<LineRenderer>();
            
            lr.startWidth = ropeWidth;
            lr.endWidth = ropeWidth;
            lr.startColor = ropeColor;
            lr.endColor = ropeColor;
            lr.positionCount = 0;
            lr.useWorldSpace = true;
            
            lr.textureMode = LineTextureMode.Tile;
            lr.textureScale = textureScale;
            lr.generateLightingData = true;
            
            if (_sharedMaterial != null)
            {
                lr.material = _sharedMaterial;
            }
            
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            lr.receiveShadows = true;
            
            return lr;
        }

        /// <summary>
        /// Halat görselini oyuncu ve bağlı kutular için günceller.
        /// Her kutu için ayrı LineRenderer kullanır.
        /// </summary>
        public void UpdateRope(Vector3 playerAttachPoint, List<DraggableBox> boxes)
        {
            if (boxes == null || boxes.Count == 0)
            {
                HideRope();
                return;
            }

            // Bağlantı sayısını kontrol et - fazla olanları kaldır, eksik olanları ekle
            while (_connections.Count < boxes.Count)
            {
                RopeConnection newConnection = new RopeConnection();
                newConnection.lineRenderer = CreateLineRenderer(_connections.Count);
                _connections.Add(newConnection);
            }
            
            while (_connections.Count > boxes.Count)
            {
                int lastIndex = _connections.Count - 1;
                if (_connections[lastIndex].lineRenderer != null)
                {
                    Destroy(_connections[lastIndex].lineRenderer.gameObject);
                }
                _connections.RemoveAt(lastIndex);
            }

            float deltaTime = Time.deltaTime;
            
            // Her kutu için ayrı halat güncelle
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] == null) continue;
                
                Vector3 currentPoint = boxes[i].GetStableAttachPoint();
                RopeConnection connection = _connections[i];
                
                // Tüm halatlar oyuncudan başlar
                Vector3 startPos = playerAttachPoint;
                
                // İlk kez başlat
                if (!connection.isInitialized)
                {
                    InitializeConnection(connection, startPos, currentPoint);
                }
                else
                {
                    // Sadece bitiş noktasını yumuşat
                    connection.smoothedEndPos = Vector3.Lerp(
                        connection.smoothedEndPos, 
                        currentPoint, 
                        deltaTime * positionSmoothSpeed
                    );
                    
                    // Hedef uzunluğu güncelle ve yumuşat
                    connection.targetRopeLength = Vector3.Distance(startPos, currentPoint);
                    connection.currentRopeLength = Mathf.Lerp(
                        connection.currentRopeLength,
                        connection.targetRopeLength,
                        deltaTime * lengthSmoothSpeed
                    );
                }
                
                // Gerçek pozisyonları kaydet
                connection.actualStartPos = startPos;
                connection.actualEndPos = currentPoint;
                
                // Fizik simülasyonu
                SimulateConnection(connection, startPos, connection.smoothedEndPos);
                
                // Bu bağlantının LineRenderer'ını güncelle
                DrawConnection(connection);
            }
        }

        private void InitializeConnection(RopeConnection connection, Vector3 start, Vector3 end)
        {
            connection.segments.Clear();
            connection.targetRopeLength = Vector3.Distance(start, end);
            connection.currentRopeLength = connection.targetRopeLength;
            connection.smoothedStartPos = start;
            connection.smoothedEndPos = end;
            
            for (int i = 0; i <= segmentsPerConnection; i++)
            {
                float t = (float)i / segmentsPerConnection;
                Vector3 pos = Vector3.Lerp(start, end, t);
                
                // Doğal sarkma ekle
                float sagFactor = 4f * t * (1f - t);
                pos.y -= sagAmount * sagFactor;
                
                connection.segments.Add(new RopeSegment(pos));
            }
            
            connection.isInitialized = true;
        }

        private void SimulateConnection(RopeConnection connection, Vector3 start, Vector3 end)
        {
            float deltaTime = Time.deltaTime * simulationSpeed;
            
            // Verlet Integration - Her segment için
            for (int i = 1; i < connection.segments.Count - 1; i++)
            {
                RopeSegment segment = connection.segments[i];
                Vector3 velocity = segment.currentPos - segment.previousPos;
                
                velocity *= (1f - damping);
                
                float maxVelocity = 2f;
                if (velocity.magnitude > maxVelocity)
                {
                    velocity = velocity.normalized * maxVelocity;
                }
                
                Vector3 newPos = segment.currentPos + velocity;
                newPos.y -= gravity * deltaTime * deltaTime;
                
                segment.previousPos = segment.currentPos;
                segment.currentPos = newPos;
                connection.segments[i] = segment;
            }
            
            // Uç noktaları sabitle
            RopeSegment firstSegment = connection.segments[0];
            firstSegment.currentPos = start;
            firstSegment.previousPos = start;
            connection.segments[0] = firstSegment;
            
            RopeSegment lastSegment = connection.segments[connection.segments.Count - 1];
            lastSegment.currentPos = end;
            lastSegment.previousPos = end;
            connection.segments[connection.segments.Count - 1] = lastSegment;
            
            // Constraint çözümü
            float segmentLength = connection.currentRopeLength / segmentsPerConnection;
            
            for (int iteration = 0; iteration < constraintIterations; iteration++)
            {
                ApplyConstraints(connection, segmentLength, start, end);
            }
        }

        private void ApplyConstraints(RopeConnection connection, float segmentLength, Vector3 start, Vector3 end)
        {
            RopeSegment first = connection.segments[0];
            first.currentPos = start;
            connection.segments[0] = first;
            
            for (int i = 0; i < connection.segments.Count - 1; i++)
            {
                RopeSegment segA = connection.segments[i];
                RopeSegment segB = connection.segments[i + 1];
                
                Vector3 delta = segB.currentPos - segA.currentPos;
                float distance = delta.magnitude;
                float error = distance - segmentLength;
                
                if (distance > 0.0001f)
                {
                    Vector3 correction = delta.normalized * error;
                    
                    float maxCorrection = 0.5f;
                    if (correction.magnitude > maxCorrection)
                    {
                        correction = correction.normalized * maxCorrection;
                    }
                    
                    if (i == 0)
                    {
                        segB.currentPos -= correction;
                    }
                    else if (i == connection.segments.Count - 2)
                    {
                        segA.currentPos += correction;
                    }
                    else
                    {
                        segA.currentPos += correction * 0.5f;
                        segB.currentPos -= correction * 0.5f;
                    }
                    
                    connection.segments[i] = segA;
                    connection.segments[i + 1] = segB;
                }
            }
            
            RopeSegment last = connection.segments[connection.segments.Count - 1];
            last.currentPos = end;
            connection.segments[connection.segments.Count - 1] = last;
        }

        /// <summary>
        /// Tek bir bağlantının LineRenderer'ını günceller
        /// </summary>
        private void DrawConnection(RopeConnection connection)
        {
            if (connection.lineRenderer == null) return;
            
            int segmentCount = connection.segments.Count;
            Vector3[] points = new Vector3[segmentCount];
            
            int blendSegments = Mathf.Min(3, segmentCount - 1);
            
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 point;
                
                if (i == 0)
                {
                    point = connection.actualStartPos;
                }
                else if (i == segmentCount - 1)
                {
                    point = connection.actualEndPos;
                }
                else if (i >= segmentCount - blendSegments - 1)
                {
                    float t = (float)(segmentCount - 1 - i) / blendSegments;
                    Vector3 simPos = connection.segments[i].currentPos;
                    Vector3 linePos = Vector3.Lerp(connection.actualEndPos, connection.actualStartPos, t / (segmentCount - 1) * blendSegments);
                    point = Vector3.Lerp(simPos, linePos, 0.3f);
                }
                else
                {
                    point = connection.segments[i].currentPos;
                }
                
                points[i] = point;
            }
            
            connection.lineRenderer.positionCount = segmentCount;
            connection.lineRenderer.SetPositions(points);
        }

        /// <summary>
        /// Tüm halatları gizler ve temizler
        /// </summary>
        public void HideRope()
        {
            foreach (var connection in _connections)
            {
                if (connection.lineRenderer != null)
                {
                    Destroy(connection.lineRenderer.gameObject);
                }
            }
            _connections.Clear();
        }

        private void OnDisable()
        {
            HideRope();
        }
        
        private void OnDestroy()
        {
            HideRope();
            if (_sharedMaterial != null)
            {
                Destroy(_sharedMaterial);
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Runtime'da LineRenderer ayarlarını güncelle
            foreach (var connection in _connections)
            {
                if (connection.lineRenderer != null)
                {
                    connection.lineRenderer.startWidth = ropeWidth;
                    connection.lineRenderer.endWidth = ropeWidth;
                    connection.lineRenderer.textureScale = textureScale;
                }
            }
        }
#endif
    }
}
