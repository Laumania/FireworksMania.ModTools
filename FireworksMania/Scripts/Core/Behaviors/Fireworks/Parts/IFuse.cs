using System;
using FireworksMania.Core.Netcode;
using FireworksMania.Core.Persistence;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IFuse
    {
        event Action OnFuseIgnited;

        bool IsUsed                                 { get; }
        bool IsIgnited                              { get; }

        void IgniteInstant();
        void IgniteWithoutFuseTime();

        /// <summary>
        /// Index of this particular fuse if its on an SaveableEntity that contains multiple fuses. Defaults to 0.
        /// </summary>
        int Index             { get; set; }
        float FuseTime        { get; set; }
        ParticleSystem Effect { get; }
        string IgniteSound    { get; }

        Transform Transform                         { get; }
        FuseNetworkIdentifier FuseNetworkIdentifier { get; }
        IFuseConnectionPoint ConnectionPoint        { get; }
        SaveableEntity SaveableEntityOwner          { get; }
    }
}
