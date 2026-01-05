using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Grass
{
    /// <summary>
    /// Simple grass spawner - spawns grass in defined rectangular areas on terrain
    /// </summary>
    public class GrassSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The terrain to spawn grass on")]
        [SerializeField] private Terrain terrain;
        
        [Tooltip("Grass prefab to spawn")]
        [SerializeField] private GameObject grassPrefab;
        
        [Header("Spawn Area")]
        [Tooltip("Center of spawn area (X, Z in world coordinates relative to terrain)")]
        [SerializeField] private Vector2 spawnCenter = new Vector2(25, 25);
        
        [Tooltip("Size of spawn area (Width, Length)")]
        [SerializeField] private Vector2 spawnSize = new Vector2(10, 10);
        
        [Header("Spawn Settings")]
        [Tooltip("Number of grass instances to spawn")]
        [SerializeField] private int grassCount = 100;
        
        [Tooltip("Random scale variation (min, max)")]
        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        
        [Header("Runtime")]
        [SerializeField] private Transform grassParent;
        
        private List<GameObject> spawnedGrass = new List<GameObject>();
        
        public void SpawnGrass()
        {
            ClearGrass();
            
            if (terrain == null)
            {
                Debug.LogError("GrassSpawner: Terrain is not assigned!");
                return;
            }
            
            if (grassPrefab == null)
            {
                Debug.LogError("GrassSpawner: Grass Prefab is not assigned!");
                return;
            }
            
            if (grassParent == null)
            {
                var parentObj = new GameObject("SpawnedGrass");
                parentObj.transform.SetParent(transform);
                grassParent = parentObj.transform;
            }
            
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;
            
            // Calculate spawn bounds
            float minX = spawnCenter.x - spawnSize.x / 2f;
            float maxX = spawnCenter.x + spawnSize.x / 2f;
            float minZ = spawnCenter.y - spawnSize.y / 2f;
            float maxZ = spawnCenter.y + spawnSize.y / 2f;
            
            Debug.Log($"GrassSpawner: Spawning {grassCount} grass in area ({minX},{minZ}) to ({maxX},{maxZ})");
            
            for (int i = 0; i < grassCount; i++)
            {
                // Random position in spawn area
                float x = Random.Range(minX, maxX);
                float z = Random.Range(minZ, maxZ);
                
                // Clamp to terrain bounds
                x = Mathf.Clamp(x, 0, terrainData.size.x);
                z = Mathf.Clamp(z, 0, terrainData.size.z);
                
                // Get terrain height
                float normalizedX = x / terrainData.size.x;
                float normalizedZ = z / terrainData.size.z;
                float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                
                // World position
                Vector3 worldPos = new Vector3(
                    terrainPos.x + x,
                    terrainPos.y + height,
                    terrainPos.z + z
                );
                
                // Spawn grass
                GameObject grass = Instantiate(grassPrefab, worldPos, Quaternion.identity, grassParent);
                grass.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                grass.transform.localScale = Vector3.one * scale;
                
                spawnedGrass.Add(grass);
            }
            
            Debug.Log($"GrassSpawner: Successfully spawned {spawnedGrass.Count} grass instances!");
        }
        
        public void ClearGrass()
        {
            foreach (var grass in spawnedGrass)
            {
                if (grass != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(grass);
                    else
#endif
                        Destroy(grass);
                }
            }
            spawnedGrass.Clear();
            
            if (grassParent != null)
            {
                while (grassParent.childCount > 0)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(grassParent.GetChild(0).gameObject);
                    else
#endif
                        Destroy(grassParent.GetChild(0).gameObject);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (terrain == null) return;
            
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Vector3 terrainPos = terrain.transform.position;
            
            Vector3 center = new Vector3(
                terrainPos.x + spawnCenter.x,
                terrainPos.y + 1,
                terrainPos.z + spawnCenter.y
            );
            
            Vector3 size = new Vector3(spawnSize.x, 0.5f, spawnSize.y);
            Gizmos.DrawCube(center, size);
            Gizmos.DrawWireCube(center, size);
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(GrassSpawner))]
    public class GrassSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GrassSpawner spawner = (GrassSpawner)target;
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Spawn Grass", GUILayout.Height(35)))
            {
                spawner.SpawnGrass();
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Grass", GUILayout.Height(35)))
            {
                spawner.ClearGrass();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
