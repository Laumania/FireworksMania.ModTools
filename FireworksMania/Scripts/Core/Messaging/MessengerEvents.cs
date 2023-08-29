using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Tools;
using UnityEngine;

namespace FireworksMania.Core.Messaging
{
    /// <summary>
    /// Plays the provided sound at a specific Vector3 position
    /// </summary>
    public class MessengerEventPlaySoundAtVector3
    {
        public MessengerEventPlaySoundAtVector3(string soundGroupName, Vector3 sourcePosition, bool delayBasedOnDistanceToListener = false)
        {
            SoundGroupName = soundGroupName;
            SourcePosition = sourcePosition;
            DelayBasedOnDistanceToListener = delayBasedOnDistanceToListener;
        }

        public string SoundGroupName { get; }
        public Vector3 SourcePosition { get; }
        public bool DelayBasedOnDistanceToListener { get; }
    }

    /// <summary>
    /// Plays the provided sound at a the position of the specified sourceTransform. Optional makes the sound follow the sourceTransform.
    /// </summary>
    public class MessengerEventPlaySound
    {
        /// <summary>
        /// Plays the provided sound at a the position of the specified sourceTransform. Optional makes the sound follow the sourceTransform.
        /// </summary>
        /// <param name="soundGroupName">Sound to play</param>
        /// <param name="sourceTransform">Transform from which position the sound should be played</param>
        /// <param name="delayBasedOnDistanceToListener">Determines if the sound should be played with realistic delay calculated from sources position to the player.</param>
        /// <param name="followTransform">Determines if the sound should follow the sourceTransform. Only use this if really needed as it have an performance impact.</param>
        public MessengerEventPlaySound(string soundGroupName, Transform sourceTransform, bool delayBasedOnDistanceToListener = false, bool followTransform = false)
        {
            SoundGroupName                 = soundGroupName;
            SourceTransform                = sourceTransform;
            DelayBasedOnDistanceToListener = delayBasedOnDistanceToListener;
            FollowTransform                = followTransform;
        }

        public string SoundGroupName               { get; }
        public Transform SourceTransform           { get; }
        public bool DelayBasedOnDistanceToListener { get; }
        public bool FollowTransform                { get; }
    }

    public class MessengerEventStopSound
    {
        public MessengerEventStopSound(string soundGroupName, Transform sourceTransform)
        {
            SoundGroupName = soundGroupName;
            SourceTransform = sourceTransform;
        }

        public string SoundGroupName { get; }
        public Transform SourceTransform { get; }
    }

    public class MessengerEventApplyExplosionForce
    {
        public MessengerEventApplyExplosionForce(Rigidbody rigidBody, float actualExplosionForce, Vector3 position, float range, float upwardsModifier, ForceMode forceMode)
        {
            RigidBody = rigidBody;
            ActualExplosionForce = actualExplosionForce;
            Position = position;
            Range = range;
            UpwardsModifier = upwardsModifier;
            ForceMode = forceMode;
        }

        public Rigidbody RigidBody { get; }
        public float ActualExplosionForce { get; }
        public Vector3 Position { get; }
        public float Range { get; }
        public float UpwardsModifier { get; }
        public ForceMode ForceMode { get; }
    }

    public class MessengerEventApplyIgnitableForce
    {
        public MessengerEventApplyIgnitableForce(IIgnitable ignitable, float ignitionForce)
        {
            Ignitable = ignitable;
            IgnitionForce = ignitionForce;
        }

        public IIgnitable Ignitable { get; }
        public float IgnitionForce { get; }
    }

    /// <summary>
    /// Broadcasted when BlueprintManager starts loading an blueprint
    /// </summary>
    public class MessengerEventBlueprintStartLoading
    {
    }

    /// <summary>
    /// Broadcasted when BlueprintManager completed loading an blueprint
    /// </summary>
    public class MessengerEventBlueprintCompletedLoading
    {
    }

    /// <summary>
    /// EXPERIMENTAL: Be aware that this might change in the future, so be prepared to update your mod :)
    /// Message can be broadcasted to change the "UI Mode" of the game.
    /// Be carefull when using this, as you potentially can lock up the game for the player if not putting back the ability to move after your done with your custom logic.
    /// </summary>
    public class MessengerEventChangeUIMode
    {
        /// <summary>
        /// Message can be broadcasted to change the "UI Mode" of the game.
        /// </summary>
        /// <param name="showCursor">Show the cursor or not.</param>
        /// <param name="canPlayerMove">Determine if the player should reacti to inputs and there by be able to move or not.</param>
        public MessengerEventChangeUIMode(bool showCursor, bool canPlayerMove)
        {
            ShowCursor = showCursor;
            CanPlayerMove = canPlayerMove;
        }

        public bool ShowCursor { get; }
        public bool CanPlayerMove { get; }
    }

    /// <summary>
    /// Broadcasted when FuseConnectTool's 'Enabled' state change
    /// </summary>
    public class MessengerEventFuseConnectionToolEnableChanged
    {
        public MessengerEventFuseConnectionToolEnableChanged(IFuseConnectionTool tool, bool enabled)
        {
            Tool    = tool;
            Enabled = enabled;
        }

        public IFuseConnectionTool Tool { get; }
        public bool Enabled { get; }
    }

    /// <summary>
    /// Broadcasted once scene have been loaded right before loading screen is removed
    /// </summary>
    public class MessengerEventLoadSceneCompleted
    {
        public MessengerEventLoadSceneCompleted(string sceneName)
        {
            SceneName = sceneName;
        }

        public string SceneName { get; }
    }

    /// <summary>
    /// Broadcasted when day changes to night and night changes to day
    /// </summary>
    public class MessengerEventDayNightChanged
    {
        public MessengerEventDayNightChanged(bool isDay)
        {
            IsDay = isDay;
        }

        public bool IsDay { get; }
    }

    public class MessengerEventApplyShakeEffect
    {
        public MessengerEventApplyShakeEffect(float effectRange, Vector3 effectPosition)
        {
            EffectRange    = effectRange;
            EffectPosition = effectPosition;
        }

        public float EffectRange      { get; }
        public Vector3 EffectPosition { get; }
    }
}
