using System;
using System.Collections.Generic;
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Definitions.EntityDefinitions;
using FireworksMania.Core.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace FireworksMania.Core.Persistence
{
    [AddComponentMenu("Fireworks Mania/Persistence/SaveableEntity")]
    public class SaveableEntity : MonoBehaviour, IHaveBaseEntityDefinition
    {
        [SerializeField]
        [Tooltip("Reference to the EntityDefinition for this prefab")]
        private BaseEntityDefinition _entityDefinition;

        [Tooltip("Enabling this will include position, rotation and scale as part of the saved data. Disable to lower blueprint file size, if you do not need these data.")]
        [SerializeField]
        private bool _saveTransformData = true;

        private const string TransformComponentTypeIdKey = "SaveableTransformComponent";
        private const string RigidbodyComponentTypeIdKey = "SaveableRigidbodyComponent";

        public string EntityInstanceId { get; private set; } = Guid.NewGuid().ToString();

        private void Awake()
        {
            TrySetEntityDefinitionFromParent();
        }

        private void TrySetEntityDefinitionFromParent()
        {
            if (_entityDefinition != null)
                return;

            var entityDefinitionFromParent = GetComponent<IHaveBaseEntityDefinition>()?.EntityDefinition;
            if (entityDefinitionFromParent != null && _entityDefinition != entityDefinitionFromParent)
                this.EntityDefinition = entityDefinitionFromParent;

            if (_entityDefinition == null)
                Debug.LogError($"'{nameof(BaseEntityDefinition)}' is missing on component '{this.GetType().Name}' on '{this.gameObject.name}', please fix else save/load won't work", this.gameObject);
        }

        public SaveableEntityData CaptureState()
        {
            if(IsValidForSaving == false)
            {
                Debug.LogError($"Entity '{this.EntityInstanceId}' shouldn't be trying to CaptureState as it's not marked as valid for saving!");
            }

            var result               = new SaveableEntityData();
            var customComponentData  = new Dictionary<string, CustomEntityComponentData>();

            if(_saveTransformData) 
                CaptureTransformState(customComponentData);

            CaptureRigidbodyState(customComponentData);

            foreach (var saveable in GetAllSaveableComponents())
            {
                try
                {
                    var capturedState = saveable.CaptureState();
                    if (capturedState.CustomData != null && capturedState.CustomData.Count > 0)
                    {
                        customComponentData[saveable.SaveableComponentTypeId] = saveable.CaptureState();
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    throw new CaptureStateException($"Unable to CaptureState for '{EntityInstanceId}' on '{saveable.SaveableComponentTypeId}', due to '{ex.Message}'");
                }
            }

            result.EntityInstanceId     = this.EntityInstanceId;
            result.EntityDefinitionId   = this.EntityDefinition.Id;
            result.CustomComponentData  = customComponentData;

            return result;
        }

        public void RestoreState(SaveableEntityData stateToRestore)
        {
            EntityInstanceId = stateToRestore.EntityInstanceId;
            
            foreach (var saveable in GetAllSaveableComponents())
            {
                try
                {
                    string typeName = saveable.SaveableComponentTypeId;

                    if (stateToRestore.CustomComponentData.TryGetValue(typeName, out CustomEntityComponentData value))
                        saveable.RestoreState(value);
                    //else
                    //    Debug.LogWarning($"No {nameof(CustomEntityComponentData)} found on '{EntityInstanceId}' for {nameof(ISaveableComponent)} '{typeName}'", this);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    throw new RestoreStateException($"Unable to RestoreState for '{EntityInstanceId}' on '{saveable.SaveableComponentTypeId}', due to '{ex.Message}'");
                }
            }

            if (_saveTransformData)
                RestoreTransformState(stateToRestore);

            RestoreRigidbodyState(stateToRestore);
        }

        private void CaptureTransformState(Dictionary<string, CustomEntityComponentData> customComponentData)
        {
            var customData = new CustomEntityComponentData();

            customData.Add<SerializableVector3>(nameof(transform.position), new SerializableVector3
            {
                X = this.transform.position.x,
                Y = this.transform.position.y,
                Z = this.transform.position.z
            });

            customData.Add<SerializableRotation>(nameof(transform.rotation), new SerializableRotation()
            {
                X = this.transform.rotation.x,
                Y = this.transform.rotation.y,
                Z = this.transform.rotation.z,
                W = this.transform.rotation.w
            });

            customData.Add<SerializableVector3>(nameof(transform.localScale), new SerializableVector3()
            {
                X = this.transform.localScale.x,
                Y = this.transform.localScale.y,
                Z = this.transform.localScale.y
            });

            customComponentData[TransformComponentTypeIdKey] = customData;
        }

        private void CaptureRigidbodyState(Dictionary<string, CustomEntityComponentData> customComponentData)
        {
            var rigidbody = GetComponent<Rigidbody>();

            if (rigidbody.OrNull() != null)
            {
                var customData = new CustomEntityComponentData();
                customData.Add<bool>(nameof(rigidbody.isKinematic), rigidbody.isKinematic);
                customComponentData[RigidbodyComponentTypeIdKey] = customData;
            }
        }

        private void RestoreTransformState(SaveableEntityData stateToRestore)
        {
            if (stateToRestore.CustomComponentData.TryGetValue(TransformComponentTypeIdKey, out CustomEntityComponentData customComponentData))
            {
                var position = customComponentData.Get<SerializableVector3>(nameof(transform.position));
                var rotation = customComponentData.Get<SerializableRotation>(nameof(transform.rotation));
                var scale    = customComponentData.Get<SerializableVector3>(nameof(transform.localScale));

                this.transform.position   = new Vector3(position.X, position.Y, position.Z);
                this.transform.rotation   = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
                this.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z);
            }
        }

        private void RestoreRigidbodyState(SaveableEntityData stateToRestore)
        {
            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody.OrNull() != null)
            {
                if (stateToRestore.CustomComponentData.TryGetValue(RigidbodyComponentTypeIdKey, out CustomEntityComponentData customComponentData))
                {
                    rigidbody.isKinematic = customComponentData.Get<bool>(nameof(rigidbody.isKinematic));
                }
            }
        }

        private IEnumerable<ISaveableComponent> GetAllSaveableComponents()
        {
            return GetComponents<ISaveableComponent>();
        }

        public void SetIsValidForSaving(bool isValid)
        {
            IsValidForSaving = isValid;
        }

        public BaseEntityDefinition EntityDefinition
        {
            get => _entityDefinition;
            set => _entityDefinition = value;
        }

        public bool IsValidForSaving { get; private set; } = true;
    }


    public class CaptureStateException : Exception
    {
        public CaptureStateException(string message) : base(message) { }
    }

    public class RestoreStateException : Exception
    {
        public RestoreStateException(string message) : base(message) { }
    }


    [Serializable]
    public class SaveableBlueprintMetaData
    {
        public string Author;
        public string GameVersion;
        public string Map;
        public string Description;
        public DateTime ModifiedUtc;
    }


    [Serializable]
    public class SaveableBlueprintData : SaveableBlueprintMetaData
    {
        [JsonProperty(Order = 100)]
        public IEnumerable<SaveableEntityData> Entities;
    }

    [Serializable]
    public struct SaveableEntityData
    {
        public string EntityInstanceId;
        public string EntityDefinitionId;
        public Dictionary<string, CustomEntityComponentData> CustomComponentData;
    }

    [Serializable]
    public struct CustomEntityComponentData
    {
        public Dictionary<string, object> CustomData;

        private void InitializeCustomDataIfNotAlready()
        {
            if (CustomData == null)
                CustomData = new Dictionary<string, object>();
        }

        public void Add<T>(string key, T data)
        {
            InitializeCustomDataIfNotAlready();

            CustomData.Add(key, data);
        }

        public T Get<T>(string key)
        {
            InitializeCustomDataIfNotAlready();

            object foundData = null;

            if (CustomData.TryGetValue(key, out foundData))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(foundData.ToString());
                }
                catch//(Exception ex) 
                { 
                    //Debug.LogWarning($"Failed json convert '{foundData.ToString()}', error = '{ex.Message}'"); 
                }

                try 
                {
                    return (T)foundData;
                }
                catch// (Exception ex) 
                { 
                    //Debug.LogWarning($"Failed native convert '{foundData.ToString()}', error = '{ex.Message}'"); 
                }
            }

            return default(T);
        }
    }

    [Serializable]
    public struct SerializableVector3
    {
        public float X;
        public float Y;
        public float Z;
    }

    [Serializable]
    public struct SerializableRotation
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
    }
}