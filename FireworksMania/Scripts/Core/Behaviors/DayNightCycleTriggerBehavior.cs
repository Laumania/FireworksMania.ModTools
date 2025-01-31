using System;
using FireworksMania.Core.Common;
using FireworksMania.Core.Messaging;
using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/DayNightCycleTriggerBehavior")]
    public class DayNightCycleTriggerBehavior : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Set the randomness in seconds from the event occurs to the actions are triggered")]
        private float _randomDelayInSeconds = 0f;

        [Header("Events")]
        public UnityEvent OnDayActions;
        public UnityEvent OnNightActions;


        private bool _lastIsDay = false;

        private void Awake()
        {
            Messenger.AddListener<MessengerEventDayNightChanged>(OnDayNightChanged);
            Initialize();
        }

        private void Initialize()
        {
            var enviroSkyManager = DependencyResolver.Instance?.Get<IEnviroSkyManager>();
            if (enviroSkyManager != null)
            {
                _lastIsDay = !enviroSkyManager.IsNight;
                InternalHandlingChanges();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    if (OnDayActions != null)
                    {
                        for (int i = 0; i < OnDayActions.GetPersistentEventCount(); i++)
                        {
                            if (String.IsNullOrEmpty(OnDayActions.GetPersistentMethodName(i)))
                                Debug.LogError($"Event '{nameof(OnDayActions)}' on '{this.gameObject.name}' missing method", this);

                            if (OnDayActions.GetPersistentTarget(i) == null)
                                Debug.LogError($"Event '{nameof(OnDayActions)}' on '{this.gameObject.name}' missing target", this);
                        }
                    }

                    if (OnNightActions != null)
                    {
                        for (int i = 0; i < OnNightActions.GetPersistentEventCount(); i++)
                        {
                            if (String.IsNullOrEmpty(OnNightActions.GetPersistentMethodName(i)))
                                Debug.LogError($"Event '{nameof(OnNightActions)}' on '{this.gameObject.name}' missing method", this);

                            if (OnNightActions.GetPersistentTarget(i) == null)
                                Debug.LogError($"Event '{nameof(OnNightActions)}' on '{this.gameObject.name}' missing target", this);
                        }
                    }
                }
            };
        }
#endif

        private void OnDestroy()
        {
            Messenger.RemoveListener<MessengerEventDayNightChanged>(OnDayNightChanged);
        }

        private void OnDayNightChanged(MessengerEventDayNightChanged args)
        {
            _lastIsDay = args.IsDay;

            if (_randomDelayInSeconds > 0f)
                Invoke("InternalHandlingChanges", UnityEngine.Random.Range(0f, _randomDelayInSeconds));
            else
                InternalHandlingChanges();
        }

        private void InternalHandlingChanges()
        {
            if (_lastIsDay)
                OnDayActions?.Invoke();
            else
                OnNightActions?.Invoke();
        }
    }
}
