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
    public class FiringSystemElectricFuse : NetworkBehaviour, IFuse, IHaveFuseConnectionPoint, IHaveFuse
    {
        public event Action OnFuseIgnited;

        [SerializeField]
        private FuseConnectionPoint _fuseConnectionPoint;

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
    }
}
