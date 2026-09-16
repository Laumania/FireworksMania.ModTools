using FireworksMania.Core.Behaviors;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Tools;
using System;
using UnityEngine;

namespace FireworksMania.Core.Messaging
{
    /// <summary>
    /// Plays the provided sound at a specific Vector3 position
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventPlaySoundAtVector3Struct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventPlaySoundAtVector3
    {
        [Obsolete("This message is deprecated, please use MessengerEventPlaySoundAtVector3Struct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventPlaySoundAtVector3(string soundGroupName, Vector3 sourcePosition, bool delayBasedOnDistanceToListener = false)
        {
            SoundGroupName                 = soundGroupName;
            SourcePosition                 = sourcePosition;
            DelayBasedOnDistanceToListener = delayBasedOnDistanceToListener;
        }

        public string SoundGroupName               { get; }
        public Vector3 SourcePosition              { get; }
        public bool DelayBasedOnDistanceToListener { get; }
    }

    /// <summary>
    /// Plays the provided sound at a specific Vector3 position
    /// </summary>
    public struct MessengerEventPlaySoundAtVector3Struct
    {
        public MessengerEventPlaySoundAtVector3Struct(string soundGroupName, Vector3 sourcePosition, bool delayBasedOnDistanceToListener = false)
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
    [Obsolete("This message is deprecated, please use MessengerEventPlaySoundStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventPlaySound
    {
        /// <summary>
        /// Plays the provided sound at a the position of the specified sourceTransform. Optional makes the sound follow the sourceTransform.
        /// </summary>
        /// <param name="soundGroupName">Sound to play</param>
        /// <param name="sourceTransform">Transform from which position the sound should be played</param>
        /// <param name="delayBasedOnDistanceToListener">Determines if the sound should be played with realistic delay calculated from sources position to the player.</param>
        /// <param name="followTransform">Determines if the sound should follow the sourceTransform. Only use this if really needed as it have an performance impact.</param>
        [Obsolete("This message is deprecated, please use MessengerEventPlaySoundStruct instead to avoid unnecessary heap allocations.", true)]
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

    /// <summary>
    /// Plays the provided sound at a the position of the specified sourceTransform. Optional makes the sound follow the sourceTransform.
    /// </summary>
    public struct MessengerEventPlaySoundStruct
    {
        /// <summary>
        /// Plays the provided sound at a the position of the specified sourceTransform. Optional makes the sound follow the sourceTransform.
        /// </summary>
        /// <param name="soundGroupName">Sound to play</param>
        /// <param name="sourceTransform">Transform from which position the sound should be played</param>
        /// <param name="delayBasedOnDistanceToListener">Determines if the sound should be played with realistic delay calculated from sources position to the player.</param>
        /// <param name="followTransform">Determines if the sound should follow the sourceTransform. Only use this if really needed as it have an performance impact.</param>
        public MessengerEventPlaySoundStruct(string soundGroupName, Transform sourceTransform, bool delayBasedOnDistanceToListener = false, bool followTransform = false)
        {
            SoundGroupName = soundGroupName;
            SourceTransform = sourceTransform;
            DelayBasedOnDistanceToListener = delayBasedOnDistanceToListener;
            FollowTransform = followTransform;
        }

        public string SoundGroupName { get; }
        public Transform SourceTransform { get; }
        public bool DelayBasedOnDistanceToListener { get; }
        public bool FollowTransform { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventStopSoundStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventStopSound
    {
        [Obsolete("This message is deprecated, please use MessengerEventStopSoundStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventStopSound(string soundGroupName, Transform sourceTransform)
        {
            SoundGroupName  = soundGroupName;
            SourceTransform = sourceTransform;
        }

        public string SoundGroupName    { get; }
        public Transform SourceTransform { get; }
    }

    public struct MessengerEventStopSoundStruct
    {
        public MessengerEventStopSoundStruct(string soundGroupName, Transform sourceTransform)
        {
            SoundGroupName  = soundGroupName;
            SourceTransform = sourceTransform;
        }

        public string SoundGroupName    { get; }
        public Transform SourceTransform { get; }
    }

    public struct MessengerEventStopAllSoundsOfTransformStruct
    {
        /// <summary>
        /// Stops every sound playing from the specified sourceTransform, no matter which sound group it
        /// belongs to. Useful when an object leaves the world and whatever it was playing should go with it,
        /// as a sound following a transform lives on the audio system and not on the object itself.
        /// </summary>
        /// <param name="sourceTransform">Transform to stop all sounds for</param>
        public MessengerEventStopAllSoundsOfTransformStruct(Transform sourceTransform)
        {
            SourceTransform = sourceTransform;
        }

        public Transform SourceTransform { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventApplyExplosionForceStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventApplyExplosionForce
    {
        [Obsolete("This message is deprecated, please use MessengerEventApplyExplosionForceStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventApplyExplosionForce(Rigidbody rigidBody, float actualExplosionForce, Vector3 position, float range, float upwardsModifier, ForceMode forceMode)
        {
            RigidBody            = rigidBody;
            ActualExplosionForce = actualExplosionForce;
            Position             = position;
            Range                = range;
            UpwardsModifier      = upwardsModifier;
            ForceMode            = forceMode;
        }

        public Rigidbody RigidBody           { get; }
        public float ActualExplosionForce    { get; }
        public Vector3 Position              { get; }
        public float Range                   { get; }
        public float UpwardsModifier         { get; }
        public ForceMode ForceMode           { get; }
    }

    public struct MessengerEventApplyExplosionForceStruct
    {
        public MessengerEventApplyExplosionForceStruct(Rigidbody rigidBody, float actualExplosionForce, Vector3 position, float range, float upwardsModifier, ForceMode forceMode)
        {
            RigidBody            = rigidBody;
            ActualExplosionForce = actualExplosionForce;
            Position             = position;
            Range                = range;
            UpwardsModifier      = upwardsModifier;
            ForceMode            = forceMode;
        }

        public Rigidbody RigidBody           { get; }
        public float ActualExplosionForce    { get; }
        public Vector3 Position              { get; }
        public float Range                   { get; }
        public float UpwardsModifier         { get; }
        public ForceMode ForceMode           { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventApplyIgnitableForceStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventApplyIgnitableForce
    {
        [Obsolete("This message is deprecated, please use MessengerEventApplyIgnitableForceStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventApplyIgnitableForce(IIgnitable ignitable, float ignitionForce)
        {
            Ignitable     = ignitable;
            IgnitionForce = ignitionForce;
        }

        public IIgnitable Ignitable   { get; }
        public float IgnitionForce    { get; }
    }

    public struct MessengerEventApplyIgnitableForceStruct
    {
        public MessengerEventApplyIgnitableForceStruct(IIgnitable ignitable, float ignitionForce)
            : this(ignitable, ignitionForce, FireworksMania.Core.Behaviors.ExplosionDamageSource.NoCauser)
        {
        }

        public MessengerEventApplyIgnitableForceStruct(IIgnitable ignitable, float ignitionForce, ulong causerClientId)
        {
            Ignitable      = ignitable;
            IgnitionForce  = ignitionForce;
            CauserClientId = causerClientId;
        }

        public IIgnitable Ignitable   { get; }
        public float IgnitionForce    { get; }

        /// <summary>The player whose exploding firework pushes this ignition, or NoCauser.</summary>
        public ulong CauserClientId   { get; }
    }

    /// <summary>
    /// Broadcasted when BlueprintManager starts loading an blueprint
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventBlueprintStartLoadingStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventBlueprintStartLoading
    {
        [Obsolete("This message is deprecated, please use MessengerEventBlueprintStartLoadingStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventBlueprintStartLoading()
        {
            
        }
    }

    /// <summary>
    /// Broadcasted when BlueprintManager starts loading an blueprint
    /// </summary>
    public struct MessengerEventBlueprintStartLoadingStruct
    {
    }

    /// <summary>
    /// Broadcasted when BlueprintManager completed loading an blueprint
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventBlueprintCompletedLoadingStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventBlueprintCompletedLoading
    {
        [Obsolete("This message is deprecated, please use MessengerEventBlueprintCompletedLoadingStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventBlueprintCompletedLoading()
        {
            
        }
    }

    /// <summary>
    /// Broadcasted when BlueprintManager completed loading an blueprint
    /// </summary>
    public struct MessengerEventBlueprintCompletedLoadingStruct
    {
    }

    /// <summary>
    /// EXPERIMENTAL: Be aware that this might change in the future, so be prepared to update your mod :)
    /// Message can be broadcasted to change the "UI Mode" of the game.
    /// Be carefull when using this, as you potentially can lock up the game for the player if not putting back the ability to move after your done with your custom logic.
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventChangeUIModeStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventChangeUIMode
    {
        /// <summary>
        /// Message can be broadcasted to change the "UI Mode" of the game.
        /// </summary>
        /// <param name="showCursor">Show the cursor or not.</param>
        /// <param name="canPlayerMove">Determine if the player should reacti to inputs and there by be able to move or not.</param>
        [Obsolete("This message is deprecated, please use MessengerEventChangeUIModeStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventChangeUIMode(bool showCursor, bool canPlayerMove)
        {
            ShowCursor    = showCursor;
            CanPlayerMove = canPlayerMove;
        }

        public bool ShowCursor    { get; }
        public bool CanPlayerMove { get; }
    }

    /// <summary>
    /// EXPERIMENTAL: Be aware that this might change in the future, so be prepared to update your mod :)
    /// Message can be broadcasted to change the "UI Mode" of the game.
    /// Be carefull when using this, as you potentially can lock up the game for the player if not putting back the ability to move after your done with your custom logic.
    /// </summary>
    public struct MessengerEventChangeUIModeStruct
    {
        /// <summary>
        /// Message can be broadcasted to change the "UI Mode" of the game.
        /// </summary>
        /// <param name="showCursor">Show the cursor or not.</param>
        /// <param name="canPlayerMove">Determine if the player should reacti to inputs and there by be able to move or not.</param>
        public MessengerEventChangeUIModeStruct(bool showCursor, bool canPlayerMove)
        {
            ShowCursor    = showCursor;
            CanPlayerMove = canPlayerMove;
        }

        public bool ShowCursor    { get; }
        public bool CanPlayerMove { get; }
    }

    /// <summary>
    /// Broadcasted when FuseConnectTool's 'Enabled' state change
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventFuseConnectionToolEnableChangedStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventFuseConnectionToolEnableChanged
    {
        [Obsolete("This message is deprecated, please use MessengerEventFuseConnectionToolEnableChangedStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventFuseConnectionToolEnableChanged(IFuseConnectionTool tool, bool enabled)
        {
            Tool    = tool;
            Enabled = enabled;
        }

        public IFuseConnectionTool Tool { get; }
        public bool Enabled             { get; }
    }

    /// <summary>
    /// Broadcasted when FuseConnectTool's 'Enabled' state change
    /// </summary>
    public struct MessengerEventFuseConnectionToolEnableChangedStruct
    {
        public MessengerEventFuseConnectionToolEnableChangedStruct(IFuseConnectionTool tool, bool enabled)
        {
            Tool    = tool;
            Enabled = enabled;
        }

        public IFuseConnectionTool Tool { get; }
        public bool Enabled             { get; }
    }

    /// <summary>
    /// Broadcasted once scene have been loaded right before loading screen is removed
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventLoadSceneCompletedStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventLoadSceneCompleted
    {
        [Obsolete("This message is deprecated, please use MessengerEventLoadSceneCompletedStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventLoadSceneCompleted(string sceneName)
        {
            SceneName = sceneName;
        }

        public string SceneName { get; }
    }

    /// <summary>
    /// Broadcasted once scene have been loaded right before loading screen is removed
    /// </summary>
    public struct MessengerEventLoadSceneCompletedStruct
    {
        public MessengerEventLoadSceneCompletedStruct(string sceneName)
        {
            SceneName = sceneName;
        }

        public string SceneName { get; }
    }

    /// <summary>
    /// Broadcasted when day changes to night and night changes to day
    /// </summary>
    [Obsolete("This message is deprecated, please use MessengerEventDayNightChangedStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventDayNightChanged
    {
        [Obsolete("This message is deprecated, please use MessengerEventDayNightChangedStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventDayNightChanged(bool isDay)
        {
            IsDay = isDay;
        }

        public bool IsDay { get; }
    }

    /// <summary>
    /// Broadcasted when day changes to night and night changes to day
    /// </summary>
    public struct MessengerEventDayNightChangedStruct
    {
        public MessengerEventDayNightChangedStruct(bool isDay)
        {
            IsDay = isDay;
        }

        public bool IsDay { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventApplyShakeEffectStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventApplyShakeEffect
    {
        [Obsolete("This message is deprecated, please use MessengerEventApplyShakeEffectStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventApplyShakeEffect(float effectRange, Vector3 effectPosition)
        {
            EffectRange    = effectRange;
            EffectPosition = effectPosition;
        }

        public float EffectRange      { get; }
        public Vector3 EffectPosition { get; }
    }

    public struct MessengerEventApplyShakeEffectStruct
    {
        public MessengerEventApplyShakeEffectStruct(float effectRange, Vector3 effectPosition)
        {
            EffectRange    = effectRange;
            EffectPosition = effectPosition;
        }

        public float EffectRange      { get; }
        public Vector3 EffectPosition { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventExecuteConsoleCommandStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventExecuteConsoleCommand
    {
        [Obsolete("This message is deprecated, please use MessengerEventExecuteConsoleCommandStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventExecuteConsoleCommand(string command)
        {
            Command = command;
        }

        public string Command { get; private set; }
    }

    public struct MessengerEventExecuteConsoleCommandStruct
    {
        public MessengerEventExecuteConsoleCommandStruct(string command)
        {
            Command = command;
        }

        public string Command { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventShowNotificationStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventShowNotification
    {
        [Obsolete("This message is deprecated, please use MessengerEventShowNotificationStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventShowNotification(string title, string message)
        {
            Title   = title;
            Message = message;
        }

        public string Title   { get; }
        public string Message { get; }
    }

    public struct MessengerEventShowNotificationStruct
    {
        public MessengerEventShowNotificationStruct(string title, string message) : this(title, message, null)
        {
        }

        public MessengerEventShowNotificationStruct(string title, string message, Sprite icon)
        {
            Title   = title;
            Message = message;
            Icon    = icon;
        }

        public string Title   { get; }
        public string Message { get; }
        public Sprite Icon    { get; }
    }
    
    public struct MessengerEventFiringSystemControllerSendSignalStruct
    {
        public MessengerEventFiringSystemControllerSendSignalStruct(int moduleIndex, int cueIndex)
        {
            ModuleIndex = moduleIndex;
            CueIndex    = cueIndex;
        }

        public int ModuleIndex { get; }
        public int CueIndex    { get; }
    }

    [Obsolete("This message is deprecated, please use MessengerEventRegisterNetworkObjectInVisibilityManagerStruct instead to avoid unnecessary heap allocations.")]
    public class MessengerEventRegisterNetworkObjectInVisibilityManager
    {
        [Obsolete("This message is deprecated, please use MessengerEventRegisterNetworkObjectInVisibilityManagerStruct instead to avoid unnecessary heap allocations.", true)]
        public MessengerEventRegisterNetworkObjectInVisibilityManager(Unity.Netcode.NetworkObject networkObject)
        {
            NetworkObject = networkObject;
        }

        public Unity.Netcode.NetworkObject NetworkObject { get; }
    }

    public struct MessengerEventRegisterNetworkObjectInVisibilityManagerStruct
    {
        public MessengerEventRegisterNetworkObjectInVisibilityManagerStruct(Unity.Netcode.NetworkObject networkObject)
        {
            NetworkObject = networkObject;
        }

        public Unity.Netcode.NetworkObject NetworkObject { get; }
    }

    public struct MessengerEventFireworkParticleSystemsRegisteringStruct
    {
        public MessengerEventFireworkParticleSystemsRegisteringStruct(GameObject rootGameObject, ParticleSystem[] particleSystems)
        {
            RootGameObject  = rootGameObject;
            ParticleSystems = particleSystems;
        }

        public GameObject       RootGameObject  { get; }
        public ParticleSystem[] ParticleSystems { get; }
    }

    public struct MessengerEventFireworkParticleSystemsUnregisteringStruct
    {
        public MessengerEventFireworkParticleSystemsUnregisteringStruct(GameObject rootGameObject)
        {
            RootGameObject = rootGameObject;
        }

        public GameObject RootGameObject { get; }
    }

    public struct MessengerEventEntityRespawnedOutsideSpawnPathStruct
    {
        /// <summary>
        /// Asks whoever owns the spawned entities to take this one back, because it was just given a fresh
        /// <c>Spawn()</c> that did not go through the normal spawn path.
        ///
        /// Needed because a despawn clears the parent Netcode has recorded, so the next <c>Spawn()</c>
        /// applies "no parent" and drops the object at the scene root. Everything that works off the
        /// entity hierarchy then skips it - the host's Clear All, blueprint save, blueprint load's
        /// duplicate removal and the fuse-connection scan all only walk <c>FireworksManager</c>'s own
        /// children (#2452). Re-parenting is only legal in the moment right after the spawn, which is
        /// why this is a message rather than something a later pass could tidy up.
        ///
        /// A message rather than a direct call because the re-spawning code lives in
        /// <c>FireworksMania.Core</c>, which cannot reference the assembly the entity hierarchy lives
        /// in - the same direction, and the same reason, as
        /// <see cref="MessengerEventFireworkParticleSystemsRegisteringStruct"/> and
        /// <see cref="MessengerEventPlaySoundAtVector3Struct"/>.
        /// </summary>
        public MessengerEventEntityRespawnedOutsideSpawnPathStruct(GameObject entityGameObject)
        {
            EntityGameObject = entityGameObject;
        }

        public GameObject EntityGameObject { get; }
    }

    /// <summary>
    /// A mortar tube has swallowed a shell: the shell GameObject is about to be destroyed and the tube
    /// takes over playing its effects. Broadcast on the server only, right before the shell goes.
    ///
    /// The host's spawn limit listens so the shell keeps costing whoever spawned it a slot for as long
    /// as the tube holds it - without this the shell simply vanished from the count, and a player could
    /// free a slot per tube by loading a rack of mortars (#2630).
    /// </summary>
    public struct MessengerEventShellSwallowedByMortarTubeStruct
    {
        public MessengerEventShellSwallowedByMortarTubeStruct(GameObject shellGameObject, MortarTube mortarTube)
        {
            ShellGameObject = shellGameObject;
            MortarTube      = mortarTube;
        }

        public GameObject ShellGameObject { get; }
        public MortarTube MortarTube      { get; }
    }

    /// <summary>
    /// A shell a mortar tube swallowed has gone spent - it has been fired and its effect has stopped
    /// throwing out new particles. Broadcast once per fired shell, so a tube reloaded while the previous
    /// shell is still in the air sends one of these for each of them, in the order they were fired.
    /// </summary>
    public struct MessengerEventMortarTubeShellSpentStruct
    {
        public MessengerEventMortarTubeShellSpentStruct(MortarTube mortarTube)
        {
            MortarTube = mortarTube;
        }

        public MortarTube MortarTube { get; }
    }

    public struct MessengerEventFireworkEffectStartedStruct
    {
        public MessengerEventFireworkEffectStartedStruct(GameObject rootGameObject)
        {
            RootGameObject = rootGameObject;
        }

        public GameObject RootGameObject { get; }
    }

    public struct MessengerEventDestructibleDestroyedStruct
    {
        public MessengerEventDestructibleDestroyedStruct(DestructibleKind kind)
            : this(kind, FireworksMania.Core.Behaviors.ExplosionDamageSource.NoCauser)
        {
        }

        public MessengerEventDestructibleDestroyedStruct(DestructibleKind kind, ulong causerClientId)
        {
            Kind           = kind;
            CauserClientId = causerClientId;
        }

        public DestructibleKind Kind { get; }

        /// <summary>
        /// The client whose firework caused this, or NoCauser. The damage path is server-only, so on
        /// the host this is frequently somebody else's id - FireworksManager relays each client's share.
        /// </summary>
        public ulong CauserClientId { get; }
    }
}
