using System;
using System.Collections;
using FireworksMania.Core.Common;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.FiringSystem
{
    [Obsolete("Replaced by UI version of firing system")]
    public class FiringSystemControllerBehavior : NetworkBehaviour
    {
        [SerializeField]
        private Canvas _firingSystemCanvas;
        [SerializeField]
        private UseableNetworkBehavior[] _buttons;

        private const int ChannelIndex = 1;
        private ICustomUIManager _playerUiManager;
        private IInputManager _inputManager;

        private void Awake()
        {
            Preconditions.CheckNotNull(_firingSystemCanvas, "Firing System Canvas cannot be null", this);
            _firingSystemCanvas.gameObject.SetActive(false);

            _playerUiManager = DependencyResolver.Instance.Get<ICustomUIManager>();
            Preconditions.CheckNotNull(_playerUiManager, $"Unable to find {nameof(ICustomUIManager)}", this);
            _playerUiManager.RegisterCanvas(_firingSystemCanvas, _firingSystemCanvas.transform.parent);

            _inputManager = DependencyResolver.Instance.Get<IInputManager>();
            Preconditions.CheckNotNull(_inputManager, $"Unable to find {nameof(IInputManager)}", this);

            Preconditions.CheckNotNull(_buttons, "Buttons array cannot be null or empty", this);
            if(_buttons.Length == 0)
                Debug.LogWarning("Buttons array is empty, no buttons will be registered for firing system.");
        }

        public override void OnDestroy()
        {
            if(_playerUiManager != null)
                _playerUiManager.UnregisterCanvas(_firingSystemCanvas);

            base.OnDestroy();
        }

        private void OnFiringSystemCueIndexFireStarted(byte cueIndex)
        {
            if(_buttons.Length >= cueIndex)
                _buttons[cueIndex-1].BeginUse();
        }

        private void OnFiringSystemCueIndexFireCanceled(byte cueIndex)
        {
            if (_buttons.Length >= cueIndex)
                _buttons[cueIndex-1].EndUse();
        }

        public void SendFireSignal(int cueIndex)
        {
            Messenger.Broadcast(new MessengerEventFiringSystemControllerSendSignal(ChannelIndex, cueIndex));
        }

        public void BeginUse()
        {
            //IsInUse = true;
            ShowFiringSystemUI();
        }

        public void EndUse()
        {
            //IsInUse = false;
        }

        public void ShowFiringSystemUI()
        {
            _playerUiManager.ShowCanvas(_firingSystemCanvas);
        }

        //Called from UI button
        public void HideFiringSystemUI()
        {
            _playerUiManager.HideCanvas(_firingSystemCanvas);
        }

        //public bool IsInUse      { get; private set; }
        //public bool ShowHighlight => true;
        //public bool ShowInteractionUI => true;
        //public string CustomText { get; }
        //public GameObject GameObject => this.gameObject;
    }
}
