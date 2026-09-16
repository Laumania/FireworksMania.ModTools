using System;

namespace FireworksMania.Core.Common
{
    public interface IInputManager
    {
        /// <summary>
        /// The type of input device most recently used by the player.
        /// Changes automatically when the player switches between keyboard/mouse,
        /// gamepad, or touch input, allowing UI systems to show appropriate prompts.
        /// </summary>
        InputDeviceType ActiveInputDeviceType { get; }

        /// <summary>
        /// Fired whenever the active input device changes - either to a different KIND of device
        /// (so <see cref="ActiveInputDeviceType"/> changed with it), or to a different device of
        /// the same kind, such as an Xbox pad swapped for a DualSense.
        ///
        /// There deliberately is no narrower "the type changed" event. There used to be, and it
        /// was a trap: on a platform with no keyboard the active type is Gamepad from the first
        /// frame and therefore never CHANGES, so it never fired at all and anything relying on it
        /// kept whatever it resolved at startup (#2175, then #2498). Compare against
        /// <see cref="ActiveInputDeviceType"/> here if you only care about the kind.
        /// </summary>
        event Action OnActiveInputDeviceChanged;
    }
}

