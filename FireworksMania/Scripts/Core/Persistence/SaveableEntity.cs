using System;
using System.Collections.Generic;
using FireworksMania.Core.Definitions.EntityDefinitions;
using Newtonsoft.Json;
using UnityEngine;

namespace FireworksMania.Core.Persistence
{
    [AddComponentMenu("Fireworks Mania/Persistence/SaveableEntity")]
    public class SaveableEntity : MonoBehaviour, IHaveBaseEntityDefinition
    {
        [SerializeField]
        [Tooltip("If not set specific in editor, will try and find via IHaveBaseEntityDefinition interface component")]
        [HideInInspector]
        private BaseEntityDefinition _entityDefinition;

        public string EntityInstanceId { get; private set; } = Guid.NewGuid().ToString();

        private void Awake()
        {
            TrySetEntityDefinitionFromParent();
        }

        private void TrySetEntityDefinitionFromParent()
        {
            var entityDefinitionFromParent = GetComponent<IHaveBaseEntityDefinition>()?.EntityDefinition;
            if (entityDefinitionFromParent != null && _entityDefinition != entityDefinitionFromParent)
                this.EntityDefinition = entityDefinitionFromParent;

            if (_entityDefinition == null)
            {
                Debug.LogError($"'{nameof(BaseEntityDefinition)}' is missing on component '{this.GetType().Name}' on '{this.gameObject.name}', please fix else save/load won't work", this.gameObject);
            }
        }

        public SaveableEntityData CaptureState()
        {
            if(IsValidForSaving == false)
            {
                Debug.LogError($"Entity '{this.EntityInstanceId}' shouldn't be trying to CaptureState as it's not marked as valid for saving!");
            }

            var result               = new SaveableEntityData();
            var customComponentData  = new Dictionary<string, CustomEntityComponentData>();
            foreach (var saveable in GetAllSaveableComponents())
            {
                try
                {
                    customComponentData[saveable.SaveableComponentTypeId] = saveable.CaptureState();
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
                    else
                        Debug.LogWarning($"No {nameof(CustomEntityComponentData)} found on '{EntityInstanceId}' for {nameof(ISaveableComponent)} '{typeName}'", this);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    throw new RestoreStateException($"Unable to RestoreState for '{EntityInstanceId}' on '{saveable.SaveableComponentTypeId}', due to '{ex.Message}'");
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