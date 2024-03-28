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
    }
}
