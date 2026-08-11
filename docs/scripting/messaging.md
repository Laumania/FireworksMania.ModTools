# The Messenger Event Bus

`Messenger` is how a mod script talks to the game — playing a sound, showing a notification, reacting to nightfall — without holding a reference to any game system. This page is for anyone writing C# in a mod.

---

## What it is

**Namespace:** `FireworksMania.Core.Messaging`  
**Type:** `public static class Messenger`

A typed publish/subscribe bus. You broadcast a message *object*, and every listener registered for that message *type* is invoked. Nobody needs a reference to anybody.

Four properties define how it behaves:

| Property | What it means for you |
|---|---|
| **Typed** | The message type *is* the topic. There are no string keys. |
| **Synchronous** | `Broadcast` invokes every listener inline. When it returns, they have all run. |
| **Process-local** | A broadcast never leaves the machine it was called on. It is not replicated. |
| **Static** | The listener table is a static dictionary that outlives scenes and objects. Nothing cleans it up for you. |

`Messenger` lives in the auto-referenced `FireworksMania.Core` assembly, so a mod script needs nothing more than `using FireworksMania.Core.Messaging;`.

---

## The API

```csharp
public delegate void Callback<T>(T arg);
```

| Purpose | Actual member | Notes |
|---|---|---|
| Subscribe | `static void AddListener<T>(Callback<T> handler)` | No generic constraint — `T` can be any type |
| Unsubscribe | `static void RemoveListener<T>(Callback<T> handler)` | Safe to call for a handler that was never added |
| Publish | `static void Broadcast<T>(T arg1)` | One argument only; there is no multi-argument overload |
| Debug dump | `static void PrintEventTable()` | `Debug.Log`s every registered event type and delegate |
| Permanence | `static void MarkAsPermanent<T>()` | Protects `T` from `Cleanup()` |
| Bulk clear | `static void Cleanup()` | Removes every non-permanent event type from the table |

There is **no** `Subscribe`, `Publish`, `Send`, `Register` or `Unregister`. The three names you need are `AddListener`, `RemoveListener` and `Broadcast`.

!!! note "About `Cleanup()` and `MarkAsPermanent<T>()`"
    Nothing in the Mod Tools package calls either one, and the comment at the top of `Messenger.cs` claiming the table is cleaned automatically on level load does not match anything in that file.[^cleanup] Do not rely on automatic cleanup, and do not call `Cleanup()` from a mod — you would be tearing down every *other* mod's listeners too.

[^cleanup]: `Messenger.cs` contains no `sceneLoaded` hook and no `[RuntimeInitializeOnLoadMethod]`. Whether the shipped game calls `Cleanup()` on scene load cannot be verified from the Mod Tools package.

---

## Three hard rules

### 1. Every `AddListener` needs a matching `RemoveListener`

!!! danger "A leaked listener breaks every other listener, not just yours"
    The listener table is **static**, so a `MonoBehaviour` that subscribed and was then destroyed without unsubscribing is still in the invocation list — and the delegate's reference to it keeps the managed object alive.

    On the next broadcast, your dead handler runs, touches `this.transform`, and throws `MissingReferenceException`. Because `Broadcast` is a synchronous multicast invoke, **an exception in one listener stops every listener after it in the list from running at all.** One forgotten `RemoveListener` in your mod can silently break the game's own systems and other people's mods.

Pair your calls in the component lifecycle and keep handlers cheap and exception-free.

### 2. The event key is `typeof(T).FullName`

The dictionary key is the type's namespace-qualified name — not the `Type`, and not assembly-qualified. Two different mods that each declare `MyMessages.ScoreChanged` land on the same key, and whichever one subscribes second gets a `Messenger.ListenerException` — *"Attempting to add listener with inconsistent signature for event type…"* — because the two `Callback<T>` delegate types do not match.

Put your own message types in a namespace nobody else will use — your nickname plus your mod name is the usual convention.

### 3. Messenger never crosses the network

A broadcast is local to the process it was called in. If every player should hear the sound or see the notification, every player's machine has to broadcast it. The way the game does this is to replicate the *trigger* with an RPC and let each machine broadcast locally — see [Multiplayer & Netcode](networking.md).

---

## Messages you broadcast

These make the game do something. The handlers live in the shipped game, so the exact presentation — notification duration, shake curve, console command grammar — is not something the Mod Tools can tell you.[^handlers]

[^handlers]: A grep across the Mod Tools package finds subscribers for only three message types. Everything else in this table is consumed by the game itself, which is not part of this package, so this page describes what each message asks for rather than exactly how it looks or sounds.

| Message | Constructor |
|---|---|
| `MessengerEventPlaySound` | `(string soundGroupName, Transform sourceTransform, bool delayBasedOnDistanceToListener = false, bool followTransform = false)` |
| `MessengerEventPlaySoundAtVector3` | `(string soundGroupName, Vector3 sourcePosition, bool delayBasedOnDistanceToListener = false)` |
| `MessengerEventStopSound` | `(string soundGroupName, Transform sourceTransform)` |
| `MessengerEventShowNotification` | `(string title, string message)` |
| `MessengerEventApplyShakeEffect` | `(float effectRange, Vector3 effectPosition)` |
| `MessengerEventApplyExplosionForce` | `(Rigidbody rigidBody, float actualExplosionForce, Vector3 position, float range, float upwardsModifier, ForceMode forceMode)` |
| `MessengerEventApplyIgnitableForce` | `(IIgnitable ignitable, float ignitionForce)` |
| `MessengerEventChangeUIMode` | `(bool showCursor, bool canPlayerMove)` |
| `MessengerEventExecuteConsoleCommand` | `(string command)` |

Notes on the awkward ones:

- **`soundGroupName`** is a string naming a sound group, not an `AudioClip`. Declare the field as `[SerializeField, GameSound] private string _sound;` (`using FireworksMania.Core.Attributes;`) so you pick from a validated list instead of typing a magic string. See [Icons & Sounds](../guides/icons-and-sounds.md).
- **`followTransform`** has a real performance cost and the source says to enable it only when the sound genuinely needs to travel with a moving object. A short one-shot does not. Pair a `followTransform: true` sound with a matching `MessengerEventStopSound` when you are done with it — that is what `FountainBehavior`, `Thruster` and `SmokeBombBehavior` do.
- **`ApplyExplosionForce` / `ApplyIgnitableForce`** are rarely what you want from a mod — the shipped `ExplosionPhysicsForceEffect` already does this properly for fireworks. `IIgnitable` lives in `FireworksMania.Core.Behaviors.Fireworks.Parts`, so referencing it needs a second `using`.
- **`ChangeUIMode` is marked EXPERIMENTAL in its own source**, with the warning that you *"potentially can lock up the game for the player if not putting back the ability to move after your done with your custom logic."* If you broadcast it, make absolutely sure you restore movement.

The codebase consistently uses named arguments for the optional bools, because they are trivially easy to swap:

```csharp
Messenger.Broadcast(new MessengerEventPlaySoundAtVector3(
    _sound, this.transform.position, delayBasedOnDistanceToListener: true));
```

### Rarely, and only in specific situations

| Message | Constructor, and when it applies |
|---|---|
| `MessengerEventFiringSystemControllerSendSignal` | `(int moduleIndex, int cueIndex)` — fires the matching cue. Every listener for this message in the shipped code is server-gated, so broadcasting it on a client does nothing. |

---

## Messages you subscribe to

These are game events you can react to.

| Message | Fired when | Members |
|---|---|---|
| `MessengerEventDayNightChanged` | The world flips day → night or night → day | `bool IsDay` |
| `MessengerEventLoadSceneCompleted` | A scene finished loading, just before the loading screen goes away | `string SceneName` |
| `MessengerEventBlueprintStartLoading` | Blueprint loading starts | *(no members)* |
| `MessengerEventBlueprintCompletedLoading` | Blueprint loading finished | *(no members)* |
| `MessengerEventFuseConnectionToolEnableChanged` | The fuse-connection tool is enabled or disabled | `IFuseConnectionTool Tool` (in `FireworksMania.Core.Tools`), `bool Enabled` |

The two blueprint messages are the pair to use if your component needs to know that a bulk load is in progress — for example to hold off expensive work until everything has been placed.

!!! tip "The empty ones still need `new`"
    `MessengerEventBlueprintStartLoading` has no constructor and no members. To broadcast one you would write `new MessengerEventBlueprintStartLoading()`; to subscribe, the handler still takes it as its parameter.

---

## Subscribing properly

Subscribe once, unsubscribe once, in matching lifecycle methods. Use `Awake`/`OnDestroy` when the component should listen for its whole lifetime, or `OnEnable`/`OnDisable` when it should genuinely stop reacting while disabled. Both are correct; mixing them is not.

=== "Awake / OnDestroy"

    ```csharp
    using FireworksMania.Core.Messaging;
    using UnityEngine;

    namespace YourNick.YourMod
    {
        public class NightLight : MonoBehaviour
        {
            [SerializeField]
            private Light _light;

            private void Awake()
            {
                Messenger.AddListener<MessengerEventDayNightChanged>(OnDayNightChanged);
            }

            private void OnDestroy()
            {
                // Safe even if Awake never ran - removing an unknown handler is a no-op.
                Messenger.RemoveListener<MessengerEventDayNightChanged>(OnDayNightChanged);
            }

            private void OnDayNightChanged(MessengerEventDayNightChanged args)
            {
                if (_light != null)
                    _light.enabled = args.IsDay == false;   // light on at night
            }
        }
    }
    ```

=== "OnEnable / OnDisable"

    ```csharp
    using FireworksMania.Core.Messaging;
    using UnityEngine;

    namespace YourNick.YourMod
    {
        public class NightLight : MonoBehaviour
        {
            [SerializeField]
            private Light _light;

            private void OnEnable()
            {
                Messenger.AddListener<MessengerEventDayNightChanged>(OnDayNightChanged);
            }

            private void OnDisable()
            {
                Messenger.RemoveListener<MessengerEventDayNightChanged>(OnDayNightChanged);
            }

            private void OnDayNightChanged(MessengerEventDayNightChanged args)
            {
                if (_light != null)
                    _light.enabled = args.IsDay == false;
            }
        }
    }
    ```

A few behaviours you can rely on:

- **Subscribing the same instance method twice is not a duplicate.** `AddListener` skips it and logs `Messenger listener '<eventId>' with Target: '…' and Method: '…' already registred. Skipping this one`. It is still sloppy — it means your lifecycle pairing is wrong somewhere.
- **`RemoveListener` for something never added is a no-op.** You can unsubscribe unconditionally even when the subscription was conditional.
- **Broadcasting with zero listeners is a silent no-op.** Nothing throws.
- **Subscribing or unsubscribing from inside a handler is safe.** The invocation list is captured before the broadcast runs, so the change takes effect from the next broadcast.

!!! warning "Networked components have two lifecycles"
    On a `NetworkBehaviour`, subscribing in `OnNetworkSpawn` means unsubscribing in `OnNetworkDespawn`. If the subscription was made under `if (IsServer)`, mirror that guard on the removal — the shipped `BaseFireworkBehavior` and `MortarTube` both do. Removing a handler that was never added is a safe no-op either way, so the guard is about keeping the pair readable rather than about avoiding a crash.

---

## Reusing a message instance

The built-in messages are plain classes with get-only properties, so `new MessengerEventPlaySound(...)` allocates on every broadcast — and they never change after construction. A message you send over and over can therefore be built once and re-broadcast.

Every shipped component constructs its message inline, which is the right call for something that fires occasionally.

```csharp
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace YourNick.YourMod
{
    public class Announcer : MonoBehaviour
    {
        [SerializeField]
        [GameSound]
        private string _sound;

        [SerializeField]
        private string _title = "My Mod";

        [SerializeField]
        private string _message = "Something happened!";

        private MessengerEventPlaySound _playSoundEvent;
        private MessengerEventShowNotification _notificationEvent;

        private void Awake()
        {
            // Build them once; re-broadcasting the same instance allocates nothing.
            _playSoundEvent    = new MessengerEventPlaySound(_sound, this.transform);
            _notificationEvent = new MessengerEventShowNotification(_title, _message);
        }

        // Hook this up to a UnityEvent, or call it from your own code.
        public void Announce()
        {
            Messenger.Broadcast(_playSoundEvent);
            Messenger.Broadcast(_notificationEvent);
        }
    }
}
```

For a one-off message, building it inline is perfectly fine — cache only what you send often.

---

## Your own message types

`AddListener<T>` has no generic constraint, so any type works as a message. That makes `Messenger` a usable internal bus between components of your own mod that have no reference to each other.

```csharp
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace YourNick.YourMod
{
    // The event key is typeof(T).FullName, so this namespace is what stops the message
    // colliding with somebody else's type also called ScoreChangedMessage.
    public readonly struct ScoreChangedMessage
    {
        public ScoreChangedMessage(int score) => Score = score;

        public int Score { get; }
    }

    public class ScoreBoard : MonoBehaviour
    {
        private void Awake() =>
            Messenger.AddListener<ScoreChangedMessage>(OnScoreChanged);

        private void OnDestroy() =>
            Messenger.RemoveListener<ScoreChangedMessage>(OnScoreChanged);

        private void OnScoreChanged(ScoreChangedMessage message) =>
            Debug.Log($"Score is now {message.Score}");
    }
}
```

A `readonly struct` is a good default for your own messages — no allocation per broadcast, and nothing downstream can mutate it. Never use a bare primitive like `float` as a message type, because the key would be `System.Single` and every mod in the game would share it.

---

## Where to go next

| You want to | Page |
|---|---|
| Make an event reach every player | [Multiplayer & Netcode](networking.md) |
| Know where your listener should be registered | [Entry Points & Lifecycle](entry-points.md) |
| Get your first script compiling into a mod | [Setting Up Scripts in a Mod](setup.md) |
| Find the right sound name for `soundGroupName` | [Icons & Sounds](../guides/icons-and-sounds.md) |
| See which shipped components already broadcast these | [Behaviors](../script-reference/behaviors.md) |
