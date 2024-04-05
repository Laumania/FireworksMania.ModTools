using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/MortarBehavior")]
    public class MortarBehavior : NetworkBehaviour, IAmGameObject, ISaveableComponent, IHaveBaseEntityDefinition, IIgnitable
    {
        [Header("General")]
        //[HideInInspector]
        [SerializeField]
        [Tooltip("This field should never been necessary to setup manually. It will be set automatically when this prefab is assigned to FireworksEntityDefinition")]
        private FireworkEntityDefinition _entityDefinition;

        [Header("Mortar Settings")]
        [SerializeField]
        private EntityDiameterDefinition _diameter;
        [SerializeField]
        [HideInInspector]
        [Tooltip("A Mortar need to have at least one MortarTube. This list is auto populated based on child gameobjects with a MortarTube component on it.")]
        private MortarTube[] _mortarTubes;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            if (_mortarTubes == null || _mortarTubes.Length == 0)
            {
                Debug.LogError($"No MortarTubes found on {this.gameObject.name} - this is not gonna work, just saying", this.gameObject);
                this.enabled = false;
                return;
            }

            _rigidbody = GetComponent<Rigidbody>();

            SetupMortarTubes();
        }

        public override void OnDestroy()
        {
            foreach (var mortarTube in _mortarTubes)
                mortarTube.OnShellLaunched -= MortarTube_OnShellLaunched;
            
            base.OnDestroy();
        }

        private void SetupMortarTubes()
        {
            foreach (var mortarTube in _mortarTubes)
                mortarTube.OnShellLaunched += MortarTube_OnShellLaunched;
        }

        private void MortarTube_OnShellLaunched(Transform mortarTubeTransform, ShellBehavior shellBehavior)
        {
            if (IsServer && shellBehavior.Recoil > 0f)
            {
                _rigidbody.AddForceAtPosition(mortarTubeTransform.up * -1f * shellBehavior.Recoil, mortarTubeTransform.position, ForceMode.Impulse);
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    if (_entityDefinition == null)
                    {
                        Debug.LogError($"Missing {nameof(FireworkEntityDefinition)} on '{this.gameObject.name}' - everything will go wrong this way!", this);
                        return;
                    }

                    _mortarTubes = this.GetComponentsInChildren<MortarTube>();

                    if (_mortarTubes == null || _mortarTubes.Length == 0)
                        Debug.LogError($"No MortarTubes found on {this.gameObject.name}", this);

                    if(_diameter == null)
                        Debug.LogError($"Missing {nameof(EntityDiameterDefinition)} on {this.gameObject.name}", this);

                    if (GetComponent<SaveableEntity>() != null)
                    {
                        GetComponent<SaveableEntity>().EntityDefinition = _entityDefinition;
                    }
                }
            };
        }
#endif

        public CustomEntityComponentData CaptureState()
        {
            throw new NotImplementedException();
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            throw new NotImplementedException();
        }

        public void Ignite(float ignitionForce)
        {
            //Note: Do nothing here
        }

        public void IgniteInstant()
        {
            //Note: Do nothing here
        }

        public string Name                                     => _entityDefinition.ItemName;
        public GameObject GameObject                           => this.gameObject;
        public string SaveableComponentTypeId                  => this.GetType().Name;
        public BaseEntityDefinition EntityDefinition
        {
            get => _entityDefinition;
            set => _entityDefinition = (FireworkEntityDefinition)value;
        }

        public Transform IgnitePositionTransform               => null;
        public bool Enabled                                    => false;
        public bool IsIgnited                                  => _mortarTubes.Any(x => x.IsIgnited);
        public EntityDiameterDefinition DiameterDefinition     => _diameter;
    }
}
