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

        private void Awake()
        {
            _velocityThresholdSqr = velocityThreshold * velocityThreshold;
            _playSoundEvent       = new MessengerEventPlaySoundStruct(_sound, this.transform);
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
            ImpactSoundRegistry.Unregister(this);
        }

        Vector3 IImpactSoundCarrier.Position => this.transform.position;

        /// <summary>
        /// Whether this object currently makes Unity marshal a managed Collision per contact pair.
        /// Driven by the impact-sound manager - see <see cref="ImpactCarrierSweep"/> and #2236.
        /// </summary>
        public bool IsCarryingCollisionMessage => _relay != null;

        public void SetCarryingCollisionMessage(bool carrying)
        {
            if (carrying == IsCarryingCollisionMessage)
                return;

            if (carrying)
            {
                //A GameObject may carry several of these - two colliders wanting two different sounds
                //is legal authoring and mod content does it - but only ONE relay, so they share it.
                //AddComponent returns null rather than a second one (#2241).
                var relay = this.gameObject.GetComponent<ImpactCollisionRelay>();

                if (relay == null)
                    relay = this.gameObject.AddComponent<ImpactCollisionRelay>();

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
