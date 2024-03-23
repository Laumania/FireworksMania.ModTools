using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public class ShellFuse : MonoBehaviour
    {
        [SerializeField]
        private Transform _ignitePosition;

        public Transform IgnitePosition => _ignitePosition;
    }
}
