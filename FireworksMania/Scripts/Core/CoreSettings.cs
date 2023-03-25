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
    }
}
