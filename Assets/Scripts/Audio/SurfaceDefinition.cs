using UnityEngine;
using System.Collections.Generic;

namespace Sisifos.Audio
{
    /// <summary>
    /// Bir zemin tipi için ses ayarlarını tutar (örn. Çim, Toprak, Kaya).
    /// </summary>
    [CreateAssetMenu(fileName = "New Surface", menuName = "Sisifos/Audio/Surface Definition")]
    public class SurfaceDefinition : ScriptableObject
    {
        [Header("Audio Clips")]
        [Tooltip("Bu zeminde çalınacak rastgele sesler")]
        public List<AudioClip> footstepSounds = new List<AudioClip>();

        [Header("Settings")]
        [Tooltip("Ses seviyesi çarpanı")]
        [Range(0f, 1f)]
        public float volumeMultiplier = 1f;
    }
}
