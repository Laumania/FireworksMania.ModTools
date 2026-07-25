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
        /// Fired whenever <see cref="ActiveInputDeviceType"/> changes, e.g. when a
        /// player picks up a gamepad mid-session.
        /// </summary>
        event Action<InputDeviceType> OnActiveInputDeviceTypeChanged;
    }
}

