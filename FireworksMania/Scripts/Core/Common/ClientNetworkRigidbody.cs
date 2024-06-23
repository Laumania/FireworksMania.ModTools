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
        /// <summary>
        /// Determines if we are server (true) or owner (false) authoritative
        /// </summary>
        private bool _isServerAuthoritative;

        private Rigidbody _rigidbody;
        private NetworkTransform _networkTransform;
        private RigidbodyInterpolation _originalInterpolation;
        private bool _originalIsKinematic;

        // Used to cache the authority state of this Rigidbody during the last frame
        private bool _isAuthority;

        private void Awake()
        {
            _networkTransform      = GetComponent<NetworkTransform>();
            _isServerAuthoritative = _networkTransform.IsServerAuthoritative();

            SetupRigidBody();
        }

        /// <summary>
        /// If the current <see cref="NetworkTransform"/> has authority,
        /// then use the <see cref="RigidBody"/> interpolation strategy,
        /// if the <see cref="NetworkTransform"/> is handling interpolation,
        /// set interpolation to none on the <see cref="RigidBody"/>
        /// <br/>
        /// Turn off physics for the rigid body until spawned, otherwise
        /// clients can run fixed update before the first
        /// full <see cref="NetworkTransform"/> update
        /// </summary>
        private void SetupRigidBody()
        {
            _rigidbody             = GetComponent<Rigidbody>();
            _originalInterpolation = _rigidbody.interpolation;
            _originalIsKinematic   = _rigidbody.isKinematic;

            _rigidbody.interpolation = _isAuthority ? _originalInterpolation : (_networkTransform.Interpolate ? RigidbodyInterpolation.None : _originalInterpolation);
            _rigidbody.isKinematic = true;
        }

        /// <summary>
        /// For owner authoritative (i.e. ClientNetworkTransform)
        /// we adjust our authority when we gain ownership
        /// </summary>
        public override void OnGainedOwnership()
        {
            UpdateOwnershipAuthority();
        }

        /// <summary>
        /// For owner authoritative(i.e. ClientNetworkTransform)
        /// we adjust our authority when we have lost ownership
        /// </summary>
        public override void OnLostOwnership()
        {
            UpdateOwnershipAuthority();
        }

        /// <summary>
        /// Sets the authority differently depending upon
        /// whether it is server or owner authoritative
        /// </summary>
        private void UpdateOwnershipAuthority()
        {
            if (_isServerAuthoritative)
            {
                _isAuthority = NetworkManager.IsServer;
            }
            else
            {
                _isAuthority = IsOwner;
            }

            // If you have authority then you are not kinematic
            _rigidbody.isKinematic = _isAuthority ? _rigidbody.isKinematic : true;

            // Set interpolation of the Rigidbody based on authority
            // With authority: let local transform handle interpolation
            // Without authority: let the NetworkTransform handle interpolation
            _rigidbody.interpolation = _isAuthority ? _originalInterpolation : RigidbodyInterpolation.None;
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            UpdateOwnershipAuthority();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            _rigidbody.interpolation = _originalInterpolation;
            // Turn off physics for the rigid body until spawned, otherwise
            // non-owners can run fixed updates before the first full
            // NetworkTransform update and physics will be applied (i.e. gravity, etc)
            _rigidbody.isKinematic = true;
        }
    }
}
