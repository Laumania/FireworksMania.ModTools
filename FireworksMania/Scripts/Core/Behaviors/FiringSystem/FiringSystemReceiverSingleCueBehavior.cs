using FireworksMania.Core.Behaviors.Fireworks;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.FiringSystem
{
    [Obsolete("Replaced by UI version of firing system")]
    public class FiringSystemReceiverSingleCueBehavior : NetworkBehaviour, IHaveFuseConnectionPoint, IHaveFuse, ISaveableComponent
    {
        internal const string CueIndexName = "CueIndexName";

        [Header("Configuration")]
        [SerializeField]
        private FiringSystemElectricFuse _electricFuse;

        [Header("UI")]
        [SerializeField]
        private TMP_Text _cueIndexText;

        private int? _restoredCueIndex     = 1;
        private int _channelIndex          = 1;
        private const int MaxCueIndex      = 9;

        private NetworkVariable<int> _cueIndexVariable = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake()
        {
            Preconditions.CheckNotNull(_electricFuse, this);
            Preconditions.CheckNotNull(_cueIndexText, this);
            UpdateCueIndexText();
            InitializeSaveableEntity();
        }

        private void InitializeSaveableEntity()
        {
            _electricFuse.SaveableEntityOwner = GetComponent<SaveableEntity>();
            Preconditions.CheckNotNull(_electricFuse.SaveableEntityOwner, $"Missing '{nameof(SaveableEntity)}' which is a required component - make sure '{this.name}' have one", this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFireSignalReceived);
                
                if (_restoredCueIndex.HasValue)
                {
                    _cueIndexVariable.Value = _restoredCueIndex.Value;
                    _restoredCueIndex = null;
                }
            }

            // Named handler, paired with the OnNetworkDespawn removal: an anonymous lambda cannot be
            // unsubscribed, so every respawn of a scene object added another one
            _cueIndexVariable.OnValueChanged += OnCueIndexChanged;

            UpdateCueIndexText();
        }

        public override void OnNetworkDespawn()
        {
            _cueIndexVariable.OnValueChanged -= OnCueIndexChanged;

            // Despawn, not just destroy: a scene object is despawned and can be spawned again, and the
            // listener is re-added by OnNetworkSpawn. The OnDestroy removal below stays as the backstop
            // for instances that are destroyed without ever being despawned (#2306).
            Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFireSignalReceived);

            base.OnNetworkDespawn();
        }

        private void OnCueIndexChanged(int previousValue, int newValue)
        {
            UpdateCueIndexText();
        }

        public override void OnDestroy()
        {
            // Unconditional: removing a never-added listener is a no-op, and every despawned
            // receiver otherwise left its listener in the static Messenger table (#2306).
            Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFireSignalReceived);

            base.OnDestroy();
        }

        private void UpdateCueIndexText()
        {
            _cueIndexText.text = _cueIndexVariable.Value.ToString("00");
        }

        public void IncreaseCueIndex()
        {
            if (IsServer)
                _cueIndexVariable.Value = Mathf.Clamp(_cueIndexVariable.Value + 1, 1, MaxCueIndex);
        }

        public void DecreaseCueIndex()
        {
            if (IsServer)
                _cueIndexVariable.Value = Mathf.Clamp(_cueIndexVariable.Value - 1, 1, MaxCueIndex);
        }

        private void OnFireSignalReceived(MessengerEventFiringSystemControllerSendSignalStruct arg)
        {
            if(arg.ModuleIndex == _channelIndex && arg.CueIndex == _cueIndexVariable.Value)
            {
                //A cue firing is an unambiguous fresh origin (same rule as a mortar reload), so a
                //causer left on the socket by an earlier burned-in chain must not outrank the runner
                _electricFuse.ResetIgnitionCauser();
                _electricFuse.TrySetIgnitionCauser(NetworkManager.LocalClientId);
                _electricFuse.IgniteInstant();
            }
        }

        public IFuse GetFuse()
        {
            return _electricFuse;
        }

        public CustomEntityComponentData CaptureState()
        {
            var componentData = new CustomEntityComponentData();
            componentData.Add<int>(FiringSystemReceiverSingleCueBehavior.CueIndexName, _cueIndexVariable.Value);
            return componentData;
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            _restoredCueIndex = customComponentData.Get<int>(FiringSystemReceiverSingleCueBehavior.CueIndexName);
        }

        public IFuseConnectionPoint ConnectionPoint => _electricFuse.ConnectionPoint;
        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
