using System;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public class MortarTubeEntryAndLaunchPosition : MonoBehaviour
    {
        public event Action<Collider> OnTriggerEnterAction;

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterAction?.Invoke(other);
        }
    }
}
