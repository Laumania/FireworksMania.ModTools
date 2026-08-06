using Cysharp.Threading.Tasks;
using DG.Tweening;
using FireworksMania.Core.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Common
{
    /// <summary>
    /// The little shake and shrink played whenever something leaves the world - a burned out firework
    /// despawning or an object removed with the Eraser Tool. It lives here so host and clients runs the
    /// exact same animation code, and so both places stays in sync if the animation is ever tweaked.
    /// </summary>
    public static class DestroyAnimation
    {
        private const float ShakeDuration        = 0.15f;
        private const float MinScaleDownDuration = 0.05f;
        private const float MaxScaleDownDuration = 0.1f;

        /// <summary>
        /// How long the server holds the despawn back so clients running a bit behind gets to finish their
        /// own animation. The object is scaled down to nothing at that point, so the extra lifetime isn't
        /// visible anywhere.
        /// </summary>
        private const float ClientGraceDuration  = 0.25f;

        private static readonly List<Collider> _colliderBuffer = new List<Collider>();
        private static readonly List<Renderer> _rendererBuffer = new List<Renderer>();
        private static readonly List<Light>    _lightBuffer    = new List<Light>();

        /// <summary>
        /// Plays the animation on whatever peer calls it. <paramref name="variation"/> is a 0-1 value used
        /// to vary the scale down duration a bit, so a whole chain of fireworks doesn't disappear in
        /// lockstep. Feed it something that is identical on every peer - a replicated seed or a network id -
        /// so all peers ends the animation at the same time.
        /// </summary>
        public static async UniTask PlayAsync(Transform transform, float variation, CancellationToken token)
        {
            StopTakingPartInThePhysicsSimulation(transform.gameObject);

            var scaleDownDuration = Mathf.Lerp(MinScaleDownDuration, MaxScaleDownDuration, Mathf.Clamp01(variation));

            await transform.DOShakeScale(ShakeDuration, 0.5f, 5, 50f, true).SetLink(transform.gameObject).WithCancellation(token);
            ThrowIfCancelledOrDestroyed(transform, token);

            await transform.DOScale(0f, scaleDownDuration).SetLink(transform.gameObject).WithCancellation(token);
            ThrowIfCancelledOrDestroyed(transform, token);

            StopBeingSeenAndHeard(transform.gameObject);
        }

        /// <summary>
        /// The object can be destroyed while a tween is awaited - typically a client hitching past the
        /// server's grace period, so the despawn arrives mid animation. SetLink then kills the tween, but a
        /// killed tween resumes the await as if it completed normally (only the token is checked on tween
        /// *updates*, and a killed tween never updates again), so the code after the await would happily
        /// touch the destroyed object (#2267). Treat it as the cancellation it really is.
        /// </summary>
        private static void ThrowIfCancelledOrDestroyed(Transform transform, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (transform == null)
                throw new OperationCanceledException();
        }

        /// <summary>
        /// Scaling to nothing only takes care of the meshes. A light keeps shining just as brightly from a
        /// point in thin air, and a sound following the object keeps playing - sounds live on the audio
        /// system rather than on the object itself, so they don't go away by hiding anything. Switching it
        /// all off here means the object is properly gone the moment the animation ends, and that it can
        /// hang around waiting for the clients without anybody noticing.
        /// </summary>
        private static void StopBeingSeenAndHeard(GameObject gameObject)
        {
            gameObject.GetComponentsInChildren(true, _rendererBuffer); //List overloads to avoid allocating on every single destroy
            foreach (var renderer in _rendererBuffer)
                renderer.enabled = false;

            _rendererBuffer.Clear();

            gameObject.GetComponentsInChildren(true, _lightBuffer);
            foreach (var light in _lightBuffer)
                light.enabled = false;

            _lightBuffer.Clear();

            Messenger.Broadcast(new MessengerEventStopAllSoundsOfTransformStruct(gameObject.transform));
        }

        /// <summary>
        /// Whatever is being removed is on its way out, so it shouldn't keep bumping into things while it
        /// shrinks. Without this, objects resting on top of it sinks into it as it gets smaller, a shrinking
        /// object stuck inside something else gets shot away as the physics pushes them apart, and the
        /// player can still push around something that is about to be gone. It saves the shrinking colliders
        /// from being updated every frame as well, though that part is small.
        /// </summary>
        private static void StopTakingPartInThePhysicsSimulation(GameObject gameObject)
        {
            gameObject.GetComponentsInChildren(true, _colliderBuffer); //List overload to avoid allocating on every single destroy
            foreach (var collider in _colliderBuffer)
                collider.enabled = false;

            _colliderBuffer.Clear();

            var rigidbody = gameObject.GetComponent<Rigidbody>();
            if (rigidbody != null && rigidbody.isKinematic == false)
                rigidbody.isKinematic = true;
        }

        /// <summary>
        /// Call this on the server after <see cref="PlayAsync"/> and before despawning, so a client that
        /// hitched mid animation doesn't get it cut short. Playing on your own skips the wait entirely.
        /// </summary>
        public static async UniTask WaitForClientsAsync(NetworkManager networkManager, CancellationToken token)
        {
            if (networkManager == null || networkManager.ConnectedClientsIds.Count <= 1)
                return;

            await UniTask.Delay(TimeSpan.FromSeconds(ClientGraceDuration), cancellationToken: token);
        }
    }
}
