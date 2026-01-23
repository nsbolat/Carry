using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Sisifos.GameFlow
{
    /// <summary>
    /// Oyun sonu Timeline Signal Asset.
    /// Timeline'da marker olarak kullanılır ve GameEndSignalReceiver'ı tetikler.
    /// </summary>
    [System.Serializable]
    public class GameEndSignal : Marker, INotification
    {
        [Tooltip("Bu signal ne yapacak?")]
        public GameEndSignalAction action = GameEndSignalAction.DisableInput;
        
        public PropertyName id => new PropertyName("GameEndSignal");
    }
}
