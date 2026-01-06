using UnityEngine;
using System.Collections.Generic;

namespace Sisifos.Audio
{
    /// <summary>
    /// Zemin tiplerini dokulara veya tag'lere göre eşleştirir.
    /// </summary>
    [CreateAssetMenu(fileName = "Surface Collection", menuName = "Sisifos/Audio/Surface Collection")]
    public class SurfaceCollection : ScriptableObject
    {
        [Header("Default Surface")]
        [Tooltip("Tanımlanamayan zeminler için varsayılan ses")]
        public SurfaceDefinition defaultSurface;

        [Header("Texture Operations (Terrain)")]
        [Tooltip("Terrain layer (texture) ismine göre zemin tanımı")]
        public List<TextureEntry> textureSurfaces = new List<TextureEntry>();

        [Header("Tag Operations (Meshes)")]
        [Tooltip("Obje tag'ine göre zemin tanımı (örn. 'Wood', 'Metal')")]
        public List<TagEntry> tagSurfaces = new List<TagEntry>();

        [System.Serializable]
        public struct TextureEntry
        {
            public string textureName;
            public SurfaceDefinition surface;
        }

        [System.Serializable]
        public struct TagEntry
        {
            public string tag;
            public SurfaceDefinition surface;
        }

        /// <summary>
        /// Terrain texture ismine göre SurfaceDefinition döndürür.
        /// </summary>
        public SurfaceDefinition GetSurfaceByTexture(string textureName)
        {
            foreach (var entry in textureSurfaces)
            {
                if (textureName.Contains(entry.textureName))
                    return entry.surface;
            }
            return defaultSurface;
        }

        /// <summary>
        /// Tag ismine göre SurfaceDefinition döndürür.
        /// </summary>
        public SurfaceDefinition GetSurfaceByTag(string tag)
        {
            foreach (var entry in tagSurfaces)
            {
                if (entry.tag.Equals(tag))
                    return entry.surface;
            }
            return defaultSurface; // Tag bulunamazsa default dönmez, null dönebilir veya default
        }
    }
}
