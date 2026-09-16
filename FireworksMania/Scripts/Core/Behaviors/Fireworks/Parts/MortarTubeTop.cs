using FireworksMania.Core.Definitions.EntityDefinitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public class MortarTubeTop : MonoBehaviour
    {
        public event Action<Collider> OnTriggerEnterAction;

        private SphereCollider _sphereCollider;
        private MortarTube     _parentMortarTube;

        //Registry of live tube tops, so tools can enumerate placement targets without physics
        //queries (overlap buffers silently truncate in busy scenes - #1105 ghost previews)
        private static readonly List<MortarTubeTop> _activeMortarTubeTops = new List<MortarTubeTop>();
        public static IReadOnlyList<MortarTubeTop> ActiveMortarTubeTops => _activeMortarTubeTops;

        private void Awake()
        {
            _sphereCollider   = GetComponent<SphereCollider>();
            _parentMortarTube = GetComponentInParent<MortarTube>();
        }

        //Registration is activation-scoped, not lifetime-scoped: a deactivated tube is not a placement
        //target, and a scene-placed tube is deactivated on despawn rather than destroyed - so registering
        //in Awake left it advertising a tube the player cannot load
        private void OnEnable()
        {
            _activeMortarTubeTops.Add(this);
        }

        private void OnDisable()
        {
            _activeMortarTubeTops.Remove(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterAction?.Invoke(other);
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    var foundColliders = GetComponents<Collider>();

                    if (foundColliders == null || foundColliders.Any() == false)
                    {
                        Debug.LogError($"{nameof(MortarTubeTop)} (on {this.gameObject.name}) requieres at least one collider to be able to know when a shell is inserted into the MortarTube", this.gameObject);
                        return;
                    }

                    var foundTriggerCollider = foundColliders.FirstOrDefault(x => x.isTrigger);
                    if (foundTriggerCollider == null)
                    {
                        Debug.LogError($"{nameof(MortarTubeTop)} (on {this.gameObject.name}) requieres at least one collider that is marked as a trigger to be able to know when a shell is inserted into the MortarTube", this.gameObject);
                        return;
                    }
                }
            };
        }
#endif

        internal float DetectionRadius => _sphereCollider != null ? _sphereCollider.radius : 0.5f;

        /// <summary>
        /// The tube this top belongs to, resolved once. Tools ask every tube top in range whether its
        /// tube can take the held shell, every ghost refresh and again per aim test - a hierarchy walk
        /// each time adds up in exactly the crowded scenes the ghost preview exists for.
        /// </summary>
        public MortarTube ParentMortarTube => _parentMortarTube;
    }
}
