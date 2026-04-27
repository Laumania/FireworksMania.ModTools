using DG.Tweening;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
using System;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/FuseConnectionPoint")]
    public class FuseConnectionPoint : MonoBehaviour, IFuseConnectionPoint
    {
        //Hacky way to show fuse connection points if FuseConnectionTool is in hand at spawn time - for shells in mortars
        public static bool IsFuseConnectionToolEnabled = false;

        [SerializeField]
        private GameObject _activeIndicator;

        private IFuse _fuse;

        private readonly Vector3 _punchScaleFactor = new Vector3(3f, 3f, 3f);

        private void Awake()
        {
            Preconditions.CheckNotNull(_activeIndicator, this);
            HideActiveIndicator();
        }

        private void Start()
        {
            Preconditions.CheckNotNull(_fuse, $"{nameof(FuseConnectionPoint)} is missing {nameof(_fuse)}", this);
            
            _fuse.OnFuseIgnited += ForceRefresh;

            if(_activeIndicator != null)
            {
                Messenger.AddListener<MessengerEventFuseConnectionToolEnableChangedStruct>(FuseConnectionPoint_FuseConnectionToolEnableChanged);
                Messenger.AddListener<MessengerEventFuseConnectionToolEnableChanged>(FuseConnectionPoint_FuseConnectionToolEnableChanged);
            }

            ForceRefresh();
        }

        public void ForceRefresh()
        {
            if (IsFuseConnectionToolEnabled && _fuse.IsUsed == false && _fuse.IsIgnited == false)
                ShowActiveIndicator();
            else
                HideActiveIndicator();
        }

        [Obsolete("Obsolete event handler for MessengerEventFuseConnectionToolEnableChanged. Use MessengerEventFuseConnectionToolEnableChangedStruct instead.")]
        private void FuseConnectionPoint_FuseConnectionToolEnableChanged(MessengerEventFuseConnectionToolEnableChanged arg)
        {
            FuseConnectionPoint_FuseConnectionToolEnableChanged(new MessengerEventFuseConnectionToolEnableChangedStruct(arg.Tool, arg.Enabled));
        }

        private void FuseConnectionPoint_FuseConnectionToolEnableChanged(MessengerEventFuseConnectionToolEnableChangedStruct arg)
        {
            if (arg.Enabled)
                ShowActiveIndicator();
            else
                HideActiveIndicator();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (_activeIndicator == null)
            {
                Debug.LogError($"Missing active indicator on '{this.name}'", this);
                return;
            }
        }

        public void Setup(IFuse fuse)
        {
            _fuse = fuse;
        }

        private void ShowActiveIndicator()
        {
            if(_fuse.IsUsed == false || _fuse.IsIgnited)
                _activeIndicator?.SetActive(true);
        }

        private void HideActiveIndicator()
        {
            _activeIndicator?.SetActive(false);
        }

        public void SetAsActiveSource(bool active)
        {
            if(active)
            {
                this.transform.DOPunchScale(_punchScaleFactor, 0.2f).SetLink(this.gameObject);
            }
            else
                this.transform.DOScale(1f, 0.2f).SetLink(this.gameObject);
        }

        private void OnDestroy()
        {
            Messenger.RemoveListener<MessengerEventFuseConnectionToolEnableChangedStruct>(FuseConnectionPoint_FuseConnectionToolEnableChanged);
            Messenger.RemoveListener<MessengerEventFuseConnectionToolEnableChanged>(FuseConnectionPoint_FuseConnectionToolEnableChanged);

            if (_fuse != null)
                _fuse.OnFuseIgnited -= ForceRefresh;
        }

        public IFuse Fuse           => _fuse;
        public Transform Transform => this.transform;
    }
}