using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    [System.Obsolete("As we now use CinemachineShake and MessengerEventApplyShakeEffect instead")]
    public interface IShakeable
    {
        Vector3 Position { get; }
        void InduceStress(float stress);
    }
}
