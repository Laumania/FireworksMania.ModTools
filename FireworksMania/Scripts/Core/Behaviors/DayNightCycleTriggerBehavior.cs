using FireworksMania.Core.Messaging;
using System.Collections;
using System.Collections.Generic;
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
        }

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
