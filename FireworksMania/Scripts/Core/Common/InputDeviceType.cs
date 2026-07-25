namespace FireworksMania.Core.Common
{
    /// <summary>
    /// Represents the type of input device currently being used by the player.
    /// Used to adapt UI prompts and control hints across platforms.
    /// </summary>
    public enum InputDeviceType
    {
        KeyboardAndMouse = 0,
        Gamepad          = 1,
        Touch            = 2,
    }
}
