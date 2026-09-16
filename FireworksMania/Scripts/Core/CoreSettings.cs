
using UnityEngine;

namespace FireworksMania.Core
{
    /// <summary>
    /// Various game settings relevant for 'Core' classes. Initialized and updated automatic by main game.
    /// </summary>
    public static class CoreSettings
    {
        //These are written when a game starts, so between games they hold whatever the last one used -
        //and with Domain Reload disabled that now reaches across play sessions too, leaving the main
        //menu of a fresh session running on the previous game's host configuration (#2612).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            AutoDespawnFireworks         = false;
            EnableExplosionPhysicsForces = false;
            EnableIgnitionForces         = false;
            EnableCameraShake            = false;
            EnableDestruction            = false;
            EnableFlyMode                = false;
#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS
            IsMultiplayer                = false;
#endif
        }

        public static bool AutoDespawnFireworks         { get; set; }
        public static bool EnableExplosionPhysicsForces { get; set; }
        public static bool EnableIgnitionForces         { get; set; }
        public static bool EnableCameraShake            { get; set; }
        public static bool EnableDestruction            { get; set; }
        public static bool EnableFlyMode                { get; set; }

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
