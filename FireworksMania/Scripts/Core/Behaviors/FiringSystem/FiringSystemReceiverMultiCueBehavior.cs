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
    public class FiringSystemReceiverMultiCueBehavior : NetworkBehaviour
    {
        private const string CueIndexName = "CueIndexName";

        [Header("Configuration")]
        [SerializeField]
        private FiringSystemElectricFuse[] _electricFuses;

        private int _channelIndex          = 1;

        private void Awake()
        {
            Preconditions.CheckNotNull(_electricFuses, this);
            Preconditions.CheckState(_electricFuses.Length > 0, $"At least one '{nameof(FiringSystemElectricFuse)}' is required on '{this.name}'");
            InitializeElectricFuses();
        }

        private void InitializeElectricFuses()
        {
            var saveableEntity = GetComponent<SaveableEntity>();
            for (int i = 0; i < _electricFuses.Length; i++)
            {
                _electricFuses[i].SaveableEntityOwner = saveableEntity;
                _electricFuses[i].Index = i;
                Preconditions.CheckNotNull(_electricFuses[i].SaveableEntityOwner, $"Missing '{nameof(SaveableEntity)}' which is a required component - make sure '{_electricFuses[i].name}' have one", this);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignal>(OnFireSignalReceived);
            }
        }


        private void OnFireSignalReceived(MessengerEventFiringSystemControllerSendSignal arg)
        {
            if(arg.ModuleIndex == _channelIndex)
            {
                if (arg.CueIndex <= _electricFuses.Length)
                    _electricFuses[arg.CueIndex-1].IgniteInstant();
            }
        }
        
        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
