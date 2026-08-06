using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core.Common;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/ErasableBehavior")]
    [DisallowMultipleComponent()]
    public class ErasableBehavior : NetworkBehaviour, IErasable
    {
        private CancellationToken _cancellationTokentoken;
        private bool _isErasing = false;

        private void Awake()
        {
            _cancellationTokentoken = this.GetCancellationTokenOnDestroy();
        }

        /// <summary>
        /// Server only - it tells every peer to play the erase animation, and the object despawns itself
        /// once that is done. Don't despawn it from the outside, that would cut the animation short.
        /// </summary>
        public void Erase()
        {
            if (_isErasing)
                return;

            if (IsSpawned == false)
            {
                //Not spawned means there is nobody to tell about it, so just erase it right here
                StartErasing();
                return;
            }

            if (IsServer == false)
            {
                Debug.LogError($"'{nameof(Erase)}' can only be called on the server - '{this.gameObject.name}' was not erased", this);
                return;
            }

            EraseOnAllPeersRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void EraseOnAllPeersRpc()
        {
            StartErasing();
        }

        private void StartErasing()
        {
            if (_isErasing)
                return;

            _isErasing = true;
            EraseAsync(_cancellationTokentoken).SuppressCancellationThrow().Forget();
        }

        private async UniTask EraseAsync(CancellationToken token)
        {
            //The network id is used to vary the animation a bit, as it's the one thing all peers agrees on
            var variation = IsSpawned ? (NetworkObjectId % 100) / 99f : 0.5f;

            await DestroyAnimation.PlayAsync(this.transform, variation, token);
            token.ThrowIfCancellationRequested();

            //Clients only play the animation, the server is the one actually removing the object for everybody
            if (IsSpawned && IsServer == false)
                return;

            await DestroyAnimation.WaitForClientsAsync(NetworkManager, token);
            token.ThrowIfCancellationRequested();

            this.gameObject.DestroyOrDespawn();
        }
    }
}
