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
                Messenger.AddListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFireSignalReceived);
            }
        }

        public override void OnDestroy()
        {
            // Unconditional: removing a never-added listener is a no-op, and every despawned
            // receiver otherwise left its listener in the static Messenger table (#2306).
            Messenger.RemoveListener<MessengerEventFiringSystemControllerSendSignalStruct>(OnFireSignalReceived);

            base.OnDestroy();
        }


        private void OnFireSignalReceived(MessengerEventFiringSystemControllerSendSignalStruct arg)
        {
            if(arg.ModuleIndex == _channelIndex)
            {
                if (arg.CueIndex <= _electricFuses.Length)
                {
                    //A cue firing is an unambiguous fresh origin (same rule as a mortar reload), so a
                    //causer left on the socket by an earlier burned-in chain must not outrank the runner
                    _electricFuses[arg.CueIndex-1].ResetIgnitionCauser();
                    _electricFuses[arg.CueIndex-1].TrySetIgnitionCauser(NetworkManager.LocalClientId);
                    _electricFuses[arg.CueIndex-1].IgniteInstant();
                }
            }
        }
        
        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
