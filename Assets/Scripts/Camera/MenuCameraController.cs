using UnityEngine;
using Unity.Cinemachine;

namespace Sisifos.Camera
{
    /// <summary>
    /// Menü kamerasını yönetir.
    /// Sabit bir açıdan sahneyi gösterir ve intro boyunca aktif kalır.
    /// </summary>
    public class MenuCameraController : MonoBehaviour
    {
        [Header("Cinemachine")]
        [Tooltip("Menü için kullanılacak Cinemachine Virtual Camera")]
        [SerializeField] private CinemachineCamera menuVirtualCamera;

        [Header("Priority Settings")]
        [Tooltip("Aktifken kamera priority değeri (yüksek = aktif)")]
        [SerializeField] private int activePriority = 20;
        [Tooltip("Deaktifken kamera priority değeri (düşük = deaktif)")]
        [SerializeField] private int inactivePriority = 0;

        private void Awake()
        {
            if (menuVirtualCamera == null)
            {
                menuVirtualCamera = GetComponent<CinemachineCamera>();
            }
        }

        private void Start()
        {
            // Başlangıçta menü kamerası aktif
            SetMenuCameraActive(true);
        }

        /// <summary>
        /// Menü kamerasını aktif/deaktif yapar.
        /// Cinemachine priority ile kamera geçişi sağlar.
        /// </summary>
        public void SetMenuCameraActive(bool active)
        {
            if (menuVirtualCamera == null)
            {
                Debug.LogError("[MenuCameraController] Virtual Camera atanmamış!");
                return;
            }

            menuVirtualCamera.Priority = active ? activePriority : inactivePriority;
        }

        /// <summary>
        /// Menü kamerasının aktif olup olmadığını döndürür.
        /// </summary>
        public bool IsActive => menuVirtualCamera != null && menuVirtualCamera.Priority == activePriority;
    }
}
