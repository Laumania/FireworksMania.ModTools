using UnityEngine;
using UnityEngine.Events;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/ToggleBehavior")]
    public class ToggleBehavior : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private bool _initialToggleState = true;

        [Header("Events")]
        public UnityEvent OnToggleOn;
        public UnityEvent OnToggleOff;

        private bool _isToggledOn;

        void Start()
        {
            _isToggledOn = _initialToggleState;
        }

        public void Toggle()
        {
            if (_isToggledOn)
                ToggleOff();
            else
                ToggleOn();
        }

        public void ToggleOn()
        {
            OnToggleOn?.Invoke();
            _isToggledOn = true;
        }

        public void ToggleOff()
        {
            OnToggleOff?.Invoke();
            _isToggledOn = false;
        }
    }
}
