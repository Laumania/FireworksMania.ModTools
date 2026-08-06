using System.Collections.Generic;
using System.Linq;
using FireworksMania.Core.Behaviors.Fireworks.Parts;
using FireworksMania.Core.Common;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Persistence;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/FireworkMountBehavior")]
    [SelectionBase]
    public class FireworkMountBehavior : NetworkBehaviour, ISaveableComponent, ISaveablePostActivatedComponent, IHaveBaseEntityDefinition, IIgnitable, IFireworkEntityHolder
    {
        [Header("General")]
        [SerializeField]
        private FireworkEntityDefinition _entityDefinition;

        [Header("Rack Settings")]
        [SerializeField]
        [HideInInspector]
        [Tooltip("A Single Shot Rack needs at least one FireworkMountPoint. This list is auto populated based on child gameobjects with a FireworkMountPoint component on it.")]
        private FireworkMountPoint[] _mountPoints;

        private List<FireworkMountPointSaveData> _restoredTubeSaveData;

        private void Awake()
        {
            PopulateMountPointList();
            Preconditions.CheckState(_mountPoints.Length != 0, $"'{nameof(_mountPoints)}' cannot be empty");
        }

        public override void OnDestroy()
        {
            //If the rack is erased/destroyed with fireworks still seated, set them free so they
            //drop to the ground instead of hanging in the air pinned to nothing
            if (IsServer)
            {
                foreach (var mountPoint in _mountPoints)
                {
                    if (mountPoint.OrNull() != null)
                        mountPoint.ReleaseMountedEntity();
                }
            }

            base.OnDestroy();
        }

        private void PopulateMountPointList()
        {
            _mountPoints = this.GetComponentsInChildren<FireworkMountPoint>();

            if (_mountPoints == null || _mountPoints.Length == 0)
                Debug.LogError($"No FireworkMountPoints found on {this.gameObject.name}", this);
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

                    PopulateMountPointList();

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
            var customData        = new CustomEntityComponentData();
            var rackTubesSaveData = new List<FireworkMountPointSaveData>();

            foreach (var mountPoint in _mountPoints)
                rackTubesSaveData.Add(mountPoint.CaptureMountPointSaveData());

            customData.Add("MountPoints", rackTubesSaveData);
            return customData;
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            //The seated entities don't exist yet at this point - stash the ids and resolve them in
            //PostActivate once every entity of the blueprint has been spawned
            _restoredTubeSaveData = customComponentData.Get<List<FireworkMountPointSaveData>>("MountPoints");
        }

        public void PostActivate(IDictionary<string, SaveableEntity> entityDictionary)
        {
            if (_restoredTubeSaveData == null)
                return;

            for (int i = 0; i < _mountPoints.Length && i < _restoredTubeSaveData.Count; i++)
            {
                var seatedEntityInstanceId = _restoredTubeSaveData[i].MountedEntityInstanceId;
                if (string.IsNullOrEmpty(seatedEntityInstanceId))
                    continue;

                if (entityDictionary.TryGetValue(seatedEntityInstanceId, out var savedEntity) == false)
                {
                    Debug.LogWarning($"Unable to find saved entity '{seatedEntityInstanceId}' for tube {i} on '{this.gameObject.name}' - leaving that tube empty", this);
                    continue;
                }

                _mountPoints[i].RestoreMountedEntity(savedEntity);
            }

            _restoredTubeSaveData = null;
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
            foreach (var mountPoint in _mountPoints)
            {
                var seatedFirework = mountPoint.MountedFirework;
                if (seatedFirework != null && seatedFirework.Enabled && seatedFirework.IsIgnited == false)
                    return seatedFirework;
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
        public bool Enabled                                    => _mountPoints.Any(x => x.MountedFirework != null && x.MountedFirework.Enabled);
        public bool IsIgnited                                  => _mountPoints.Any(x => x.MountedFirework != null && x.MountedFirework.IsIgnited);
    }
}
