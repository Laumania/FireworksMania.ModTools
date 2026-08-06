using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// Global per-frame budget for how many destroyed-debris swaps are allowed to run.
    /// A big explosion can destroy many <see cref="DestructibleBehavior"/> at once and each swap is
    /// expensive (pool get/instantiate, network spawn, RPCs), so the swaps are spread over multiple
    /// frames instead of all running in the detonation frame (https://github.com/Laumania/FireworksMania/issues/2220).
    /// </summary>
    public static class DestructionSpawnBudget
    {
        public const int MaxSpawnsPerFrame = 2;

        /// <summary>
        /// Frames taking at least this long are considered too slow to take on extra work.
        /// </summary>
        public const float SlowFrameSeconds = 0.1f;

        /// <summary>
        /// True while the current frame is already too slow to take on extra work. Shared by the
        /// destruction stagger (DestructibleBehavior) and the ignition queue (FireworksManager).
        /// Deliberately measured on UNSCALED delta time: Unity clamps Time.deltaTime to
        /// 'Maximum Allowed Timestep' (ProjectSettings/TimeManager.asset, lowered to 0.05 by issue #2218),
        /// so a Time.deltaTime gate at 0.1 could never fire again - it would read at most the clamp.
        /// Time.unscaledDeltaTime is not clamped and keeps measuring the real length of the hitch.
        /// </summary>
        public static bool IsCurrentFrameSlow => Time.unscaledDeltaTime >= SlowFrameSeconds;

        private static int _lastSeenFrameCount = -1;
        private static int _spawnsInFrame      = 0;

        public static bool TryConsume(int frameCount)
        {
            if (frameCount != _lastSeenFrameCount)
            {
                _lastSeenFrameCount = frameCount;
                _spawnsInFrame      = 0;
            }

            if (_spawnsInFrame >= MaxSpawnsPerFrame)
                return false;

            _spawnsInFrame++;
            return true;
        }

        public static void Reset()
        {
            _lastSeenFrameCount = -1;
            _spawnsInFrame      = 0;
        }
    }
}
