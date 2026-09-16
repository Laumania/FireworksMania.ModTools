using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/PlaySoundOnImpactBehavior")]
    public class PlaySoundOnImpactBehavior : NetworkBehaviour, IImpactSoundCarrier
    {
        [SerializeField]
        [FormerlySerializedAs("ImpactSound")]
        [GameSound]
        private string _sound;

        private float velocityThreshold          = .5f;
        private const double PLAY_SOUND_COOLDOWN = 0.3f; // Cooldown to prevent playing the sound too often
        private double _lastImpactTime           = 0f;
        private float  _velocityThresholdSqr;

        private MessengerEventPlaySoundStruct _playSoundEvent;
        private ImpactCollisionRelay          _relay;
        private Transform                     _impactSource;

        private void Awake()
        {
            _velocityThresholdSqr = velocityThreshold * velocityThreshold;
            _playSoundEvent       = new MessengerEventPlaySoundStruct(_sound, ImpactSource);
        }

        private void OnEnable()
        {
            ImpactSoundRegistry.Register(this);

            //Without a manager in the scene nobody would ever hand out relays, and impact sounds
            //would silently stop working (mod maps, test scenes, a map without MapEssentials).
            if (ImpactSoundRegistry.IsManaged == false)
                SetCarryingCollisionMessage(true);
        }

        private void OnDisable()
        {
            //The relay is deliberately left in place. An inactive GameObject generates no contact
            //events, so it costs nothing while disabled, and destroying it here would race the
            //pooled-debris path (Destroy is deferred to end of frame, so a same-frame re-activate
            //would try to add a second relay while the first is still pending destruction).
            //A relay on ANOTHER GameObject is the exception (see SetImpactSource): that GameObject
            //stays active when this one goes, so the relay would be left behind serving a target
            //that is gone - on the character's bone, after every ragdoll knockdown (#2853).
            if (_relay != null && _relay.gameObject != this.gameObject)
                SetCarryingCollisionMessage(false);

            ImpactSoundRegistry.Unregister(this);
        }

        Vector3 IImpactSoundCarrier.Position => ImpactSource.position;

        /// <summary>
        /// The transform whose collisions this behaviour listens to, and where its sound plays. Its own transform,
        /// unless <see cref="SetImpactSource"/> pointed it somewhere else - and again once that has been destroyed.
        /// </summary>
        public Transform ImpactSource => _impactSource != null ? _impactSource : this.transform;

        /// <summary>
        /// Listens for impacts on <paramref name="source"/> and plays the sound there, instead of on this GameObject.
        /// For physics that only exists at runtime and so has nowhere to author this behaviour: the player ragdoll's
        /// bodies are built onto whatever character is in use, so their impact sounds sit on the ragdoll's pose
        /// slots and are pointed at the bones (#2853). Null goes back to this GameObject.
        /// </summary>
        public void SetImpactSource(Transform source)
        {
            if (ReferenceEquals(source, this.transform))
                source = null;

            //ReferenceEquals rather than ==: a source destroyed since it was set compares equal to null, and going
            //back to this GameObject must still rebuild the sound event away from the dead transform.
            if (ReferenceEquals(source, _impactSource))
                return;

            //A relay only ever hears the GameObject it sits on, so a relay the sweep already granted is given
            //back where it is and asked for again where the behaviour is going.
            var wasCarrying = IsCarryingCollisionMessage;

            if (wasCarrying)
                SetCarryingCollisionMessage(false);

            _impactSource   = source;
            _playSoundEvent = new MessengerEventPlaySoundStruct(_sound, ImpactSource);

            if (wasCarrying)
                SetCarryingCollisionMessage(true);
        }

        /// <summary>
        /// Whether this object currently makes Unity marshal a managed Collision per contact pair.
        /// Driven by the game's impact-sound manager - see its <c>ImpactCarrierSweep</c> and #2236.
        /// </summary>
        public bool IsCarryingCollisionMessage => _relay != null;

        public void SetCarryingCollisionMessage(bool carrying)
        {
            if (carrying == IsCarryingCollisionMessage)
                return;

            if (carrying)
            {
                //On the impact source, which is this GameObject unless SetImpactSource said otherwise (#2853).
                var source = ImpactSource.gameObject;

                //A GameObject may carry several of these - two colliders wanting two different sounds
                //is legal authoring and mod content does it - but only ONE relay, so they share it.
                //AddComponent returns null rather than a second one (#2241).
                var relay = source.GetComponent<ImpactCollisionRelay>();

                if (relay == null)
                    relay = source.AddComponent<ImpactCollisionRelay>();

                //Still null means a relay released earlier this frame is pending destruction and is
                //blocking the add. Nothing to do but stay uncarried - the sweep comes back around.
                if (relay == null)
                    return;

                relay.AddTarget(this);
                _relay = relay;
                return;
            }

            //Destroy, not disable: a disabled receiver still gets the message and still costs the
            //full marshaling (measured, see #2236). Only removing the component clears Unity's
            //collision-message mask for this GameObject - but not while another behaviour on the
            //same GameObject is still being served by it.
            if (_relay.RemoveTarget(this) == false)
                Destroy(_relay);

            _relay = null;
        }

        /// <summary>
        /// Called by <see cref="ImpactCollisionRelay"/> with the squared impulse of the collision.
        /// </summary>
        internal void HandleImpact(float sqrImpulse)
        {
            if (sqrImpulse > _velocityThresholdSqr)
                PlaySingleImpactSound();
        }

        public void PlaySingleImpactSound()
        {
            // Impacts are only detected where this object's physics is actually simulated
            // (non-authority rigidbodies are kinematic, see ClientNetworkRigidbody), so the
            // machine that detects the impact replicates it to everyone else.
            if (TryPlaySoundWithCooldown() && IsSpawned)
                PlayImpactSoundOnOthersRpc();
        }

        [Rpc(SendTo.NotMe)]
        private void PlayImpactSoundOnOthersRpc()
        {
            // The cooldown also runs on the receiving side, deduplicating the sound when
            // two machines each simulate one side of the same collision and both send.
            TryPlaySoundWithCooldown();
        }

        private bool TryPlaySoundWithCooldown()
        {
            var now = Time.timeAsDouble;
            if (now - _lastImpactTime < PLAY_SOUND_COOLDOWN)
                return false;

            _lastImpactTime = now;
            Messenger.Broadcast(_playSoundEvent);
            return true;
        }
    }
}
