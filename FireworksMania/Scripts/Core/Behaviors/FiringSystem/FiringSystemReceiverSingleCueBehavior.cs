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
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignal>(OnFireSignalReceived);
                
                if (_restoredCueIndex.HasValue)
                {
                    _cueIndexVariable.Value = _restoredCueIndex.Value;
                    _restoredCueIndex = null;
                }
            }

            _cueIndexVariable.OnValueChanged += (value, newValue) =>
            {
                UpdateCueIndexText();
            };

            UpdateCueIndexText();
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

        private void OnFireSignalReceived(MessengerEventFiringSystemControllerSendSignal arg)
        {
            if(arg.ModuleIndex == _channelIndex && arg.CueIndex == _cueIndexVariable.Value)
            {
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
