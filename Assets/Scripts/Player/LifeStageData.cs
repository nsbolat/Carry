using UnityEngine;

namespace Sisifos.Player
{
    /// <summary>
    /// Yaşam dönemlerini tanımlayan ScriptableObject.
    /// Her dönem için karakter modeli, animasyonlar ve gameplay parametreleri içerir.
    /// </summary>
    [CreateAssetMenu(fileName = "LifeStage", menuName = "Sisifos/Life Stage Data")]
    public class LifeStageData : ScriptableObject
    {
        [Header("Stage Info")]
        [Tooltip("Dönem adı (UI için)")]
        public string stageName = "Unnamed Stage";
        
        [Tooltip("Dönem indexi (0=Çocukluk, 1=Ergenlik, 2=Yetişkinlik, 3=Yaşlılık, 4=Ölüm)")]
        [Range(0, 4)]
        public int stageIndex;

        [Header("Character Model")]
        [Tooltip("Bu dönem için karakter model GameObject'i (prefab içindeki child)")]
        public GameObject characterModel;
        
        [Tooltip("Animator Controller (opsiyonel - model üzerindeki kullanılır)")]
        public RuntimeAnimatorController animatorController;

        [Header("Movement Modifiers")]
        [Tooltip("Hareket hızı çarpanı (1 = normal)")]
        [Range(0.5f, 2f)]
        public float moveSpeedMultiplier = 1f;
        
        [Tooltip("Zıplama kuvveti çarpanı (1 = normal)")]
        [Range(0.5f, 2f)]
        public float jumpForceMultiplier = 1f;
        
        [Tooltip("Hızlanma çarpanı (1 = normal)")]
        [Range(0.5f, 2f)]
        public float accelerationMultiplier = 1f;

        [Header("Visual Effects")]
        [Tooltip("Karakter renk tonu (Material'e uygulanabilir)")]
        public Color characterTint = Color.white;
        
        [Tooltip("Bu dönem için özel particle efekti (opsiyonel)")]
        public ParticleSystem stageParticleEffect;
    }
}
