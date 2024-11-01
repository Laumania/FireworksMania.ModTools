using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Behaviors
{
    public interface IUseable
    {
        void BeginUse();
        void EndUse();

        bool IsInUse { get; }
        bool ShowHighlight { get; }
        bool ShowInteractionUI { get; }
        string CustomText { get; }
        GameObject GameObject { get; }
    }

    [AddComponentMenu("Fireworks Mania/Behaviors/Other/UseableBehavior")]
    public class UseableBehavior : MonoBehaviour, IUseable
    {
        [Header("General")]
        [SerializeField]
        [Tooltip("If filled out, will be shown under the 'Use' tooltip UI in the game.")]
        private string _customText;
        [SerializeField]
        [Tooltip("Indicates if this object should be highlighted or not, when in view of the player.")]
        private bool _showHighlight = true;
        [SerializeField]
        [Tooltip("Indicates if the interaction UI should be shown when the player looks at it.")]
        private bool _showInteractionUI = true;

        [Header("Events")]
        public UnityEvent OnBeginUse;
        public UnityEvent OnEndUse;

        public void BeginUse()
        {
            IsInUse = true;
            if (OnBeginUse != null)
            {
                OnBeginUse.Invoke();
            }
        }

        public void EndUse()
        {
            IsInUse = false;
            if (OnEndUse != null)
            {
                OnEndUse.Invoke();
            }
        }

        public bool IsInUse { get; private set; }
        public GameObject GameObject => this.gameObject;
        public bool ShowHighlight => _showHighlight;
        public bool ShowInteractionUI => _showInteractionUI;
        public string CustomText => _customText;
    }
}
