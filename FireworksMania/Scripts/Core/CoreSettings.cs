
namespace FireworksMania.Core
{
    /// <summary>
    /// Various game settings relevant for 'Core' classes. Initialized and updated automatic by main game.
    /// </summary>
    public static class CoreSettings
    {
        public static bool AutoDespawnFireworks         { get; set; }
        public static bool EnableExplosionPhysicsForces { get; set; }
        public static bool EnableIgnitionForces         { get; set; }
        public static bool EnableCameraShake            { get; set; }
        public static bool EnableDestruction            { get; set; }

        /// <summary>
        /// This is a temp fix for modders to know if a game is in single or multiplayer mode. Please be aware that this might change in the future.
        /// </summary>
        public static bool IsMultiplayer
        {
            get;
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
            set;
#endif
        }
    }
}
