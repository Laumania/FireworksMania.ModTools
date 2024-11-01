using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/ToggleNetworkBehavior")]
    public class ToggleNetworkBehavior : NetworkBehaviour
    {
        [Header("General")]
        [SerializeField]
        private bool _initialToggleState = true;

        [Header("Events")]
        public UnityEvent OnToggleOn;
        public UnityEvent OnToggleOff;

        private NetworkVariable<bool> _isToggledOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(IsServer)
                _isToggledOn.Value = _initialToggleState;

            _isToggledOn.OnValueChanged += (prevValue, newValue) =>
            {
                OnToggleChanged();
            };

            OnToggleChanged();
        }

        public void Toggle()
        {
            if (_isToggledOn.Value)
                ToggleOff();
            else
                ToggleOn();
        }

        public void ToggleOn()
        {
            ToggleOnRpc();
        }

        public void ToggleOff()
        {
            ToggleOffRpc();
        }

        private void OnToggleChanged()
        {
            if (_isToggledOn.Value == true)
                OnToggleOn?.Invoke();
            else
                OnToggleOff?.Invoke();
        }

        [Rpc(SendTo.Server)]
        private void ToggleOnRpc()
        {
            _isToggledOn.Value = true;
        }

        [Rpc(SendTo.Server)]
        private void ToggleOffRpc()
        {
            _isToggledOn.Value = false;
        }
    }
}
