using UnityEngine;

namespace Grass
{
    /// <summary>
    /// Attach this component to any object that should bend grass when moving through it.
    /// Typically added to the player character or other moving entities.
    /// </summary>
    public class GrassInteractor : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Radius of grass interaction effect. Grass within this radius will bend away.")]
        [SerializeField] private float interactionRadius = 1.5f;
        
        [Tooltip("Optional: offset the interaction center from this object's pivot.")]
        [SerializeField] private Vector3 offset = Vector3.zero;
        
        public float InteractionRadius => interactionRadius;
        
        /// <summary>
        /// World position of the interaction center (with offset applied)
        /// </summary>
        public Vector3 InteractionPosition => transform.position + transform.TransformDirection(offset);
        
        private void OnEnable()
        {
            // Create manager if it doesn't exist
            if (GrassInteractionManager.Instance == null)
            {
                var managerObj = new GameObject("GrassInteractionManager");
                managerObj.AddComponent<GrassInteractionManager>();
            }
            
            GrassInteractionManager.Instance.RegisterInteractor(this);
        }
        
        private void OnDisable()
        {
            if (GrassInteractionManager.Instance != null)
            {
                GrassInteractionManager.Instance.UnregisterInteractor(this);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(InteractionPosition, interactionRadius);
        }
    }
}
