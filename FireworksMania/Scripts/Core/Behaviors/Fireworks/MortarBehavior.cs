using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Interactions;
using FireworksMania.Core.Persistence;
using System.Collections.Generic;
using System.Linq;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/MortarBehavior")]
    [SelectionBase]
    public class MortarBehavior : NetworkBehaviour, ISaveableComponent, IHaveBaseEntityDefinition, IIgnitable, IHaveEntityDiameterDefinition
    {
        [Header("General")]
        [SerializeField]
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
            Preconditions.CheckNotNull(_mortarTubes);
            PopulateMortarTubeList();
            Preconditions.CheckState(_mortarTubes.Length != 0, $"'{nameof(_mortarTubes)}' cannot be empty");

            _rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            SetupMortarTubes();
            base.OnNetworkSpawn();
        }

        public override void OnDestroy()
        {
            foreach (var mortarTube in _mortarTubes)
                mortarTube.OnShellLaunched -= MortarTube_OnShellLaunched;
            
            base.OnDestroy();
        }

        private void PopulateMortarTubeList()
        {
            _mortarTubes = this.GetComponentsInChildren<MortarTube>();

            if (_mortarTubes == null || _mortarTubes.Length == 0)
                Debug.LogError($"No MortarTubes found on {this.gameObject.name}", this);
        }

        private void SetupMortarTubes()
        {
            for (int i = 0; i < _mortarTubes.Length; i++)
            {
                var mortarTube = _mortarTubes[i];

                mortarTube.OnShellLaunched += MortarTube_OnShellLaunched;
                mortarTube.ParentEntityDefinition = this._entityDefinition;
                mortarTube.GetFuse().Index = i;
            }
        }

        private void MortarTube_OnShellLaunched(Transform mortarTubeTransform, ShellBehavior shellBehavior)
        {
            if (IsServer && shellBehavior.Recoil > 0f)
            {
                _rigidbody.AddForceAtPosition(mortarTubeTransform.up * -1f * shellBehavior.Recoil * 0.5f, mortarTubeTransform.position, ForceMode.Impulse);
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

                    PopulateMortarTubeList();

                    if (_diameter == null)
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
            var customData       = new CustomEntityComponentData();
            var shellEntityIds   = new List<string>();

            foreach (var mortarTube in _mortarTubes)
            {
                shellEntityIds.Add(mortarTube.TubeState.ShellEntityId.ToString());
            }

            customData.Add<List<string>>("ShellEntityIds", shellEntityIds);
            return customData;
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            var shellEntityIds = customComponentData.Get<List<string>>("ShellEntityIds");

            for (int i = 0; i < _mortarTubes.Length; i++)
            {
                var mortarTube = _mortarTubes[i];
                mortarTube.RestoreTubeState(shellEntityIds[i]);
            }
        }

        public void Ignite(float ignitionForce)
        {
            GetNextIgnitable()?.Ignite(ignitionForce);
        }

        public void IgniteInstant()
        {
            GetNextIgnitable()?.IgniteInstant();
        }

        private IIgnitable GetNextIgnitable()
        {
            var isPickedUp = this.gameObject.GetComponent<IsPickedUp>().OrNull();

            if (isPickedUp == null)
                return null;

            foreach (var mortarTube in _mortarTubes)
            {
                if(mortarTube.Enabled == true && mortarTube.IsIgnited == false)
                    return mortarTube;
            }

            return null;
        }

        public string Name                                     => _entityDefinition.ItemName;
        public GameObject GameObject                           => this.gameObject;
        public string SaveableComponentTypeId                  => this.GetType().Name;
        public BaseEntityDefinition EntityDefinition
        {
            get => _entityDefinition;
            set => _entityDefinition = (FireworkEntityDefinition)value;
        }

        public Transform IgnitePositionTransform               => GetNextIgnitable()?.IgnitePositionTransform;
        public bool Enabled                                    => _mortarTubes.Any(x => x.Enabled);
        public bool IsIgnited                                  => _mortarTubes.Any(x => x.IsIgnited);
        public EntityDiameterDefinition DiameterDefinition     => _diameter;
    }
}
