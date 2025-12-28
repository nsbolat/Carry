using UnityEngine;
using System.Collections.Generic;

namespace Sisifos.Interaction
{
    /// <summary>
    /// LineRenderer kullanarak halat görselleştirmesi yapar.
    /// BoxCarrier ile birlikte çalışır.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RopeVisualizer : MonoBehaviour
    {
        [Header("Rope Settings")]
        [Tooltip("Her segment için ara nokta sayısı (eğri için)")]
        [SerializeField] private int segmentsPerConnection = 3;
        
        [Tooltip("Halatın sarkma miktarı")]
        [SerializeField] private float sagAmount = 0.3f;
        
        [Header("Visual")]
        [SerializeField] private float ropeWidth = 0.05f;
        [SerializeField] private Color ropeColor = new Color(0.6f, 0.4f, 0.2f); // Kahverengi
        [SerializeField] private Material ropeMaterial;

        private LineRenderer _lineRenderer;
        private List<Vector3> _ropePoints = new List<Vector3>();

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

            if (ropeMaterial != null)
            {
                _lineRenderer.material = ropeMaterial;
            }
            else
            {
                // Varsayılan unlit materyal
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                _lineRenderer.material.color = ropeColor;
            }
        }

        /// <summary>
        /// Halat görselini oyuncu ve bağlı kutular için günceller
        /// </summary>
        /// <param name="playerAttachPoint">Oyuncunun halat bağlantı noktası</param>
        /// <param name="boxes">Bağlı kutuların listesi (sıralı)</param>
        public void UpdateRope(Vector3 playerAttachPoint, List<DraggableBox> boxes)
        {
            _ropePoints.Clear();

            if (boxes == null || boxes.Count == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            // Başlangıç noktası: oyuncu
            _ropePoints.Add(playerAttachPoint);

            // Her kutu için bağlantı noktaları ekle
            Vector3 previousPoint = playerAttachPoint;
            
            foreach (var box in boxes)
            {
                if (box == null) continue;

                Vector3 boxPoint = box.AttachPoint;
                
                // Sarkan halat eğrisi için ara noktalar ekle
                AddSaggingPoints(previousPoint, boxPoint);
                
                previousPoint = boxPoint;
            }

            // LineRenderer'ı güncelle
            _lineRenderer.positionCount = _ropePoints.Count;
            _lineRenderer.SetPositions(_ropePoints.ToArray());
        }

        private void AddSaggingPoints(Vector3 start, Vector3 end)
        {
            for (int i = 1; i <= segmentsPerConnection; i++)
            {
                float t = (float)i / segmentsPerConnection;
                Vector3 point = Vector3.Lerp(start, end, t);
                
                // Parabolik sarkma ekle (ortada maksimum)
                float sagFactor = 4f * t * (1f - t); // 0'da 0, 0.5'te 1, 1'de 0
                point.y -= sagAmount * sagFactor;
                
                _ropePoints.Add(point);
            }
        }

        /// <summary>
        /// Halatı gizler
        /// </summary>
        public void HideRope()
        {
            _lineRenderer.positionCount = 0;
        }

        private void OnDisable()
        {
            HideRope();
        }
    }
}
