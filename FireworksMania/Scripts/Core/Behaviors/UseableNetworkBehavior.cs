using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/UseableNetworkBehavior")]
    public class UseableNetworkBehavior : NetworkBehaviour, IUseable
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

        private readonly NetworkVariable<bool> _isInUse = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isInUse.OnValueChanged += (prevValue, newValue) =>
            {
                OnIsUseChanged(newValue);
            };

            OnIsUseChanged(_isInUse.Value);
        }

        private void OnIsUseChanged(bool isInUse)
        {
            if (isInUse)
                OnBeginUse?.Invoke();
            else
                OnEndUse?.Invoke();
        }

        public void BeginUse()
        {
            BeginUseRpc();
        }

        [Rpc(SendTo.Server)]
        private void BeginUseRpc()
        {
            _isInUse.Value = true;
        }

        public void EndUse()
        {
            EndUseRpc();
        }

        [Rpc(SendTo.Server)]
        private void EndUseRpc()
        {
            _isInUse.Value = false;
        }

        public bool IsInUse => _isInUse.Value;

        public GameObject GameObject  => this.gameObject;
        public bool ShowHighlight     => _showHighlight;
        public bool ShowInteractionUI => _showInteractionUI;
        public string CustomText      => _customText;
    }
}
