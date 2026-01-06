using UnityEngine;

namespace Sisifos.Audio
{
    /// <summary>
    /// Kutuların yerde sürüklenirken çıkardığı ses efekti.
    /// DraggableBox olan objelere eklenmelidir.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody))]
    public class BoxDragAudio : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Sürüklenme sesi (loop olmalı)")]
        [SerializeField] private AudioClip dragLoopSound;
        
        [Tooltip("Yere çarpma/tokuşma sesleri")]
        [SerializeField] private AudioClip[] impactSounds;
        
        [Header("Settings")]
        [SerializeField] private float maxVolume = 0.5f;
        [SerializeField] private float minSpeedForSound = 0.5f;
        [SerializeField] private float maxSpeedForFullVolume = 5f;
        
        [Tooltip("Rastgele pitch aralığı (impact için)")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        
        [Tooltip("Impact sesi için minimum çarpma hızı")]
        [SerializeField] private float minImpactVelocity = 2f;
        
        // Components
        private AudioSource _audioSource;
        private Rigidbody _rigidbody;
        
        // State
        private bool _isGrounded;
        private float _lastImpactTime;
        private const float IMPACT_COOLDOWN = 0.2f;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _rigidbody = GetComponent<Rigidbody>();
            
            // AudioSource ayarları
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.volume = 0f;
            
            if (dragLoopSound != null)
            {
                _audioSource.clip = dragLoopSound;
            }
        }

        private void Update()
        {
            UpdateDragSound();
        }

        private void UpdateDragSound()
        {
            if (dragLoopSound == null) return;
            
            // Yatay hız (XZ düzleminde)
            Vector3 horizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;
            
            // Yerdeyse ve hareket ediyorsa ses çal
            if (_isGrounded && speed > minSpeedForSound)
            {
                // Volume hıza göre
                float speedRatio = Mathf.Clamp01((speed - minSpeedForSound) / (maxSpeedForFullVolume - minSpeedForSound));
                float targetVolume = speedRatio * maxVolume;
                
                // Smooth volume geçişi
                _audioSource.volume = Mathf.Lerp(_audioSource.volume, targetVolume, Time.deltaTime * 10f);
                
                if (!_audioSource.isPlaying)
                {
                    _audioSource.Play();
                }
            }
            else
            {
                // Yavaşça kapat
                _audioSource.volume = Mathf.Lerp(_audioSource.volume, 0f, Time.deltaTime * 5f);
                
                if (_audioSource.volume < 0.01f && _audioSource.isPlaying)
                {
                    _audioSource.Stop();
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Yere çarptığında impact sesi
            if (impactSounds != null && impactSounds.Length > 0)
            {
                float impactVelocity = collision.relativeVelocity.magnitude;
                
                if (impactVelocity > minImpactVelocity && Time.time - _lastImpactTime > IMPACT_COOLDOWN)
                {
                    PlayImpactSound(impactVelocity);
                    _lastImpactTime = Time.time;
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            // Zemin teması kontrolü - collision normal yukarı bakıyorsa yer
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    _isGrounded = true;
                    return;
                }
            }
            _isGrounded = false;
        }

        private void OnCollisionExit(Collision collision)
        {
            _isGrounded = false;
        }

        private void PlayImpactSound(float velocity)
        {
            AudioClip clip = impactSounds[Random.Range(0, impactSounds.Length)];
            
            // Hıza göre volume
            float volumeRatio = Mathf.Clamp01(velocity / 10f);
            float volume = volumeRatio * maxVolume;
            
            // Rastgele pitch
            _audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            _audioSource.PlayOneShot(clip, volume);
            _audioSource.pitch = 1f; // Reset for loop sound
        }
    }
}
