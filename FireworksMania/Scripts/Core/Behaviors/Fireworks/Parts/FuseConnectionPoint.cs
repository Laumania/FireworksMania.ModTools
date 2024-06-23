using DG.Tweening;
using FireworksMania.Core.Messaging;
using FireworksMania.Core.Utilities;
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

        //[SerializeField]
        private Fuse _fuse;

        private readonly Vector3 _punchScaleFactor = new Vector3(3f, 3f, 3f);

        private void Awake()
        {
            Preconditions.CheckNotNull(_activeIndicator);
            HideActiveIndicator();
        }

        private void Start()
        {
            Preconditions.CheckNotNull(_fuse);
            
            _fuse.OnFuseIgnited += HideActiveIndicator;

            if(_activeIndicator != null)
                Messenger.AddListener<MessengerEventFuseConnectionToolEnableChanged>(FuseConnectionPoint_FuseConnectionToolEnableChanged);

            ForceRefresh();
        }

        public void ForceRefresh()
        {
            if (IsFuseConnectionToolEnabled && _fuse.IsUsed == false)
                ShowActiveIndicator();
            else
                HideActiveIndicator();
        }
        
        private void FuseConnectionPoint_FuseConnectionToolEnableChanged(MessengerEventFuseConnectionToolEnableChanged arg)
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

        public void Setup(Fuse fuse)
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
                this.transform.DOPunchScale(_punchScaleFactor, 0.2f);
            }
            else
                this.transform.DOScale(1f, 0.2f);
        }

        private void OnDestroy()
        {
            Messenger.RemoveListener<MessengerEventFuseConnectionToolEnableChanged>(FuseConnectionPoint_FuseConnectionToolEnableChanged);

            if (_fuse != null)
                _fuse.OnFuseIgnited -= HideActiveIndicator;
        }

        public Fuse Fuse           => _fuse;
        public Vector3 Position    => this.transform.position;
        public Transform Transform => this.transform;
    }
}