using UnityEngine;
using System.Collections.Generic;

namespace Grass
{
    /// <summary>
    /// Manages grass interaction by sending interactor positions to the grass shader.
    /// This is a singleton that collects all GrassInteractor components and updates
    /// global shader properties each frame.
    /// </summary>
    public class GrassInteractionManager : MonoBehaviour
    {
        public static GrassInteractionManager Instance { get; private set; }
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        
        private static readonly int InteractorPositionsID = Shader.PropertyToID("_GrassInteractorPositions");
        private static readonly int InteractorCountID = Shader.PropertyToID("_GrassInteractorCount");
        
        private const int MaxInteractors = 10;
        
        private List<GrassInteractor> interactors = new List<GrassInteractor>();
        private Vector4[] interactorData = new Vector4[MaxInteractors];
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                // Reset shader properties
                Shader.SetGlobalInt(InteractorCountID, 0);
            }
        }
        
        private void LateUpdate()
        {
            UpdateShaderProperties();
        }
        
        private void UpdateShaderProperties()
        {
            int count = Mathf.Min(interactors.Count, MaxInteractors);
            
            for (int i = 0; i < MaxInteractors; i++)
            {
                if (i < count && interactors[i] != null && interactors[i].isActiveAndEnabled)
                {
                    Vector3 pos = interactors[i].transform.position;
                    float radius = interactors[i].InteractionRadius;
                    interactorData[i] = new Vector4(pos.x, pos.y, pos.z, radius);
                }
                else
                {
                    interactorData[i] = Vector4.zero;
                }
            }
            
            Shader.SetGlobalVectorArray(InteractorPositionsID, interactorData);
            Shader.SetGlobalInt(InteractorCountID, count);
        }
        
        public void RegisterInteractor(GrassInteractor interactor)
        {
            if (!interactors.Contains(interactor))
            {
                interactors.Add(interactor);
            }
        }
        
        public void UnregisterInteractor(GrassInteractor interactor)
        {
            interactors.Remove(interactor);
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            foreach (var interactor in interactors)
            {
                if (interactor != null && interactor.isActiveAndEnabled)
                {
                    Gizmos.DrawWireSphere(interactor.transform.position, interactor.InteractionRadius);
                }
            }
        }
    }
}
