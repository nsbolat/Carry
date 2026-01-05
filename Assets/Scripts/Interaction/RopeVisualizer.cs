using UnityEngine;
using System.Collections.Generic;

namespace Sisifos.Interaction
{
    /// <summary>
    /// Verlet Integration kullanarak gerçekçi halat fiziği simülasyonu yapar.
    /// BoxCarrier ile birlikte çalışır. Çoklu kutu zincirleme desteği var.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
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

        private LineRenderer _lineRenderer;
        
        // Her bağlantı için ayrı Verlet simülasyonu
        private List<RopeConnection> _connections = new List<RopeConnection>();
        private List<Vector3> _allPoints = new List<Vector3>();

        private class RopeConnection
        {
            public List<RopeSegment> segments = new List<RopeSegment>();
            public float targetRopeLength;
            public float currentRopeLength;
            public bool isInitialized;
            public Vector3 smoothedStartPos;
            public Vector3 smoothedEndPos;
            public Vector3 actualStartPos; // Gerçek başlangıç pozisyonu (çizim için)
            public Vector3 actualEndPos; // Gerçek kutu pozisyonu (çizim için)
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
            _lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            _lineRenderer.startWidth = ropeWidth;
            _lineRenderer.endWidth = ropeWidth;
            _lineRenderer.startColor = ropeColor;
            _lineRenderer.endColor = ropeColor;
            _lineRenderer.positionCount = 0;
            _lineRenderer.useWorldSpace = true;
            
            _lineRenderer.textureMode = LineTextureMode.Tile;
            _lineRenderer.textureScale = textureScale;
            
            // Aydınlatma ve gölge için gerekli
            _lineRenderer.generateLightingData = true;

            if (ropeMaterial != null)
            {
                // Materyal instance'ı oluştur (orijinali değiştirmemek için)
                _lineRenderer.material = new Material(ropeMaterial);
            }
            else
            {
                // URP için varsayılan Lit shader (gölge desteği için)
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader != null)
                {
                    _lineRenderer.material = new Material(litShader);
                    _lineRenderer.material.SetColor("_BaseColor", ropeColor);
                }
                else
                {
                    // Fallback
                    _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    _lineRenderer.material.color = ropeColor;
                }
            }
            
            // Gölge ayarları
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _lineRenderer.receiveShadows = true;
        }

        /// <summary>
        /// Halat görselini oyuncu ve bağlı kutular için günceller
        /// </summary>
        public void UpdateRope(Vector3 playerAttachPoint, List<DraggableBox> boxes)
        {
            if (boxes == null || boxes.Count == 0)
            {
                _lineRenderer.positionCount = 0;
                _connections.Clear();
                return;
            }

            // Bağlantı sayısını kontrol et
            int requiredConnections = boxes.Count;
            
            while (_connections.Count < requiredConnections)
            {
                _connections.Add(new RopeConnection());
            }
            while (_connections.Count > requiredConnections)
            {
                _connections.RemoveAt(_connections.Count - 1);
            }

            float deltaTime = Time.deltaTime;
            
            // Her bağlantıyı güncelle
            Vector3 previousPoint = playerAttachPoint;
            
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] == null) continue;
                
                Vector3 currentPoint = boxes[i].GetStableAttachPoint();
                RopeConnection connection = _connections[i];
                
                // Başlangıç noktası her zaman direkt takip eder (karaktere bağlı kalması için)
                Vector3 startPos = previousPoint;
                
                // İlk kez başlat
                if (!connection.isInitialized)
                {
                    InitializeConnection(connection, startPos, currentPoint);
                }
                else
                {
                    // Sadece bitiş noktasını yumuşat (kutu tarafı)
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
                
                // Gerçek pozisyonları kaydet (çizim için)
                connection.actualStartPos = startPos;
                connection.actualEndPos = currentPoint;
                
                // Fizik simülasyonu - başlangıç direkt, bitiş yumuşatılmış
                SimulateConnection(connection, startPos, connection.smoothedEndPos);
                
                // Sonraki bağlantı için kutu pozisyonunu kullan (yumuşatılmamış)
                previousPoint = currentPoint;
            }
            
            // Tüm noktaları birleştir ve çiz
            DrawAllConnections();
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
                
                // Başlangıçta doğal sarkma ekle
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
                
                // Damping uygula
                velocity *= (1f - damping);
                
                // Hız sınırla (ışınlanma önleme)
                float maxVelocity = 2f;
                if (velocity.magnitude > maxVelocity)
                {
                    velocity = velocity.normalized * maxVelocity;
                }
                
                // Yerçekimi uygula
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
            
            // Constraint çözümü - yumuşatılmış uzunluk kullan
            float segmentLength = connection.currentRopeLength / segmentsPerConnection;
            
            for (int iteration = 0; iteration < constraintIterations; iteration++)
            {
                ApplyConstraints(connection, segmentLength, start, end);
            }
        }

        private void ApplyConstraints(RopeConnection connection, float segmentLength, Vector3 start, Vector3 end)
        {
            // Başlangıç noktasını sabitle
            RopeSegment first = connection.segments[0];
            first.currentPos = start;
            connection.segments[0] = first;
            
            // Segment mesafe constraint'leri
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
                    
                    // Düzeltme miktarını sınırla (ışınlanma önleme)
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
            
            // Bitiş noktasını sabitle
            RopeSegment last = connection.segments[connection.segments.Count - 1];
            last.currentPos = end;
            connection.segments[connection.segments.Count - 1] = last;
        }

        private void DrawAllConnections()
        {
            _allPoints.Clear();
            
            for (int c = 0; c < _connections.Count; c++)
            {
                RopeConnection connection = _connections[c];
                int segmentCount = connection.segments.Count;
                
                // İlk bağlantı hariç, ilk noktayı ekleme
                int startIndex = (c == 0) ? 0 : 1;
                
                // Son birkaç segmenti karaktere doğru yönlendir (düzgün açı için)
                int blendSegments = Mathf.Min(3, segmentCount - 1);
                
                for (int i = startIndex; i < segmentCount; i++)
                {
                    Vector3 point;
                    
                    if (i == 0)
                    {
                        // İlk segment - gerçek başlangıç pozisyonu
                        point = connection.actualStartPos;
                    }
                    else if (i == segmentCount - 1)
                    {
                        // Son segment - gerçek kutu pozisyonu
                        point = connection.actualEndPos;
                    }
                    else if (i >= segmentCount - blendSegments - 1)
                    {
                        // Son birkaç segment - karaktere doğru yumuşak geçiş
                        float t = (float)(segmentCount - 1 - i) / blendSegments;
                        Vector3 simPos = connection.segments[i].currentPos;
                        
                        // Kutu ile karakter arasında doğrusal interpolasyon
                        Vector3 linePos = Vector3.Lerp(connection.actualEndPos, connection.actualStartPos, t / (segmentCount - 1) * blendSegments);
                        
                        // Simülasyon pozisyonu ile doğrusal pozisyon arası karıştır
                        point = Vector3.Lerp(simPos, linePos, 0.3f);
                    }
                    else
                    {
                        point = connection.segments[i].currentPos;
                    }
                    
                    _allPoints.Add(point);
                }
            }
            
            _lineRenderer.positionCount = _allPoints.Count;
            _lineRenderer.SetPositions(_allPoints.ToArray());
        }

        /// <summary>
        /// Halatı gizler
        /// </summary>
        public void HideRope()
        {
            _lineRenderer.positionCount = 0;
            _connections.Clear();
        }

        private void OnDisable()
        {
            HideRope();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.startWidth = ropeWidth;
                _lineRenderer.endWidth = ropeWidth;
                _lineRenderer.textureScale = textureScale;
            }
        }
#endif
    }
}
