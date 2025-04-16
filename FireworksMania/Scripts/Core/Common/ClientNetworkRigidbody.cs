using FireworksMania.Core.Utilities;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Client Network Rigidbody")]
    [UMod.Shared.ModDontCompile]
    [DisallowMultipleComponent]
    public class ClientNetworkRigidbody : NetworkRigidbody
    {
        private NetworkTransform _networkTransform;
        private RigidbodyInterpolation _originalInterpolation;

        // Used to cache the authority state of this Rigidbody during the last frame
        private bool _isAuthority;

        protected override void Awake()
        {
            base.Awake();

            AutoUpdateKinematicState      = false;
            _networkTransform             = GetComponent<NetworkTransform>();
            _originalInterpolation        = this.Rigidbody.interpolation;
            Rigidbody.interpolation       = _isAuthority ? _originalInterpolation : (_networkTransform.Interpolate ? RigidbodyInterpolation.None : _originalInterpolation);
            Rigidbody.isKinematic         = true;
        }


        /// <summary>
        /// For owner authoritative (i.e. ClientNetworkTransform)
        /// we adjust our authority when we gain ownership
        /// </summary>
        public override void OnGainedOwnership()
        {
            UpdateOwnershipAuthority();
            base.OnGainedOwnership();
        }

        /// <summary>
        /// For owner authoritative(i.e. ClientNetworkTransform)
        /// we adjust our authority when we have lost ownership
        /// </summary>
        public override void OnLostOwnership()
        {
            UpdateOwnershipAuthority();
            base.OnLostOwnership();
        }

        /// <summary>
        /// Sets the authority differently depending upon
        /// whether it is server or owner authoritative
        /// </summary>
        private void UpdateOwnershipAuthority()
        {
            if (_networkTransform.IsServerAuthoritative())
            {
                _isAuthority = NetworkManager.IsServer;
            }
            else
            {
                _isAuthority = IsOwner;
            }

            // If you have authority then you are not kinematic
            this.Rigidbody.isKinematic = _isAuthority ? Rigidbody.isKinematic : true;

            // Set interpolation of the Rigidbody based on authority
            // With authority: let local transform handle interpolation
            // Without authority: let the NetworkTransform handle interpolation
            this.Rigidbody.interpolation = _isAuthority ? _originalInterpolation : RigidbodyInterpolation.None;
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            UpdateOwnershipAuthority();
            base.OnNetworkSpawn();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            this.Rigidbody.interpolation = _originalInterpolation;
            // Turn off physics for the rigid body until spawned, otherwise
            // non-owners can run fixed updates before the first full
            // NetworkTransform update and physics will be applied (i.e. gravity, etc)
            this.Rigidbody.isKinematic = true;

            base.OnNetworkDespawn();
        }
    }
}
