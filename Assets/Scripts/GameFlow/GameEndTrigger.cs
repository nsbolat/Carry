using UnityEngine;
using UnityEngine.Playables;

namespace Sisifos.GameFlow
{
    /// <summary>
    /// Oyun sonu trigger zone'u - Oyuncu bu bölgeye girince Timeline başlar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GameEndTrigger : MonoBehaviour
    {
        [Header("Timeline")]
        [Tooltip("Oyun sonu Timeline'ı")]
        public PlayableDirector endGameTimeline;
        
        [Header("Settings")]
        [Tooltip("Tetikleme için gerekli tag (genellikle 'Player')")]
        public string playerTag = "Player";
        
        [Tooltip("Sadece bir kez tetiklensin mi?")]
        public bool triggerOnce = true;
        
        [Header("Optional")]
        [Tooltip("GameEndController referansı (otomatik bulunabilir)")]
        public GameEndController gameEndController;
        
        private bool _triggered = false;
        
        private void Awake()
        {
            // Collider'ı trigger yap
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            
            // GameEndController'ı bul
            if (gameEndController == null)
            {
                gameEndController = FindFirstObjectByType<GameEndController>();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (_triggered && triggerOnce) return;
            
            if (other.CompareTag(playerTag))
            {
                TriggerGameEnd();
            }
        }
        
        /// <summary>
        /// Oyun sonunu tetikler
        /// </summary>
        public void TriggerGameEnd()
        {
            if (_triggered && triggerOnce) return;
            _triggered = true;
            
            Debug.Log("[GameEndTrigger] Oyun sonu tetiklendi!");
            
            // GameEndController'a bildir
            if (gameEndController != null)
            {
                gameEndController.StartGameEnd();
            }
            
            // Timeline'ı başlat
            if (endGameTimeline != null)
            {
                endGameTimeline.Play();
            }
            else
            {
                Debug.LogWarning("[GameEndTrigger] Timeline atanmamış!");
            }
        }
        
        private void OnDrawGizmos()
        {
            // Trigger alanını görselleştir
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            
            var col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
