using System;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Netcode;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.FiringSystem
{
    //Note: The Electric fuse isn't actually the fuse...it's more a connection point or socket... the ElectrictFuseConnection is what will end up being the actual connection
    public class FiringSystemElectricFuse : NetworkBehaviour, IFuse, IHaveFuseConnectionPoint, IHaveFuse, IIgnitionCauserCarrier
    {
        public event Action OnFuseIgnited;

        [SerializeField]
        private FuseConnectionPoint _fuseConnectionPoint;

        private IgnitionCauser _ignitionCauser;

        private void Awake()
        {
            Preconditions.CheckNotNull(_fuseConnectionPoint, this);
            _fuseConnectionPoint.Setup(this);
        }

        public void IgniteInstant()
        {
            OnFuseIgnited?.Invoke();
        }

        public IFuse GetFuse()
        {
            return this;
        }

        public void IgniteWithoutFuseTime()
        {
            OnFuseIgnited?.Invoke();
        }

        //A chain burning INTO a receiver's wire (rather than lit directly at the box) reaches here
        //instead of IgniteInstant - without this override the causer it carries was silently dropped
        //and the IFuse default just called the no-arg overload above
        public void IgniteWithoutFuseTime(ulong causerClientId)
        {
            TrySetIgnitionCauser(causerClientId);
            IgniteWithoutFuseTime();
        }

        public bool IsUsed    { get; } = false;
        public bool IsIgnited { get; } = false;
        public Transform Transform => this.transform;
        public FuseNetworkIdentifier FuseNetworkIdentifier => new()
        {
            FuseNetworkObjectId   = this.NetworkObjectId,
            FuseNetworkBehaviorId = this.NetworkBehaviourId,
            FuseIndex             = this.Index
        };
        public IFuseConnectionPoint ConnectionPoint => _fuseConnectionPoint;
        public SaveableEntity SaveableEntityOwner { get; set; }

        public int Index      { get; set; } = 0;
        public float FuseTime { get; set; } = 0f;
        public ParticleSystem Effect => throw new NotImplementedException();
        public string IgniteSound => throw new NotImplementedException();

        public ulong IgnitionCauserClientId                 => _ignitionCauser.Value;
        public void TrySetIgnitionCauser(ulong causerClientId) => _ignitionCauser.TrySet(causerClientId);
        public void ResetIgnitionCauser()                      => _ignitionCauser.Reset();
    }
}
