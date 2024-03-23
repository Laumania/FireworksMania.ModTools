using DG.Tweening;
using FireworksMania.Core.Messaging;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Fireworks/Parts/FuseConnectionPoint")]
    public class FuseConnectionPoint : MonoBehaviour, IFuseConnectionPoint
    {
        [SerializeField]
        private GameObject _activeIndicator;

        //[SerializeField]
        private Fuse _fuse;

        private readonly Vector3 _punchScaleFactor = new Vector3(3f, 3f, 3f);

        private void Awake()
        {
            if (_activeIndicator == null)
            {
                Debug.LogError($"Missing active indicator on '{this.name}'", this);
                return;
            }
            
            HideActiveIndicator();
        }

        private void Start()
        {
            if (_fuse == null)
            {
                Debug.LogError($"Missing Fuse on '{this.name}'", this);
                return;
            }
            _fuse.OnFuseIgnited += HideActiveIndicator;

            if(_activeIndicator != null)
                Messenger.AddListener<MessengerEventFuseConnectionToolEnableChanged>(FuseConnectionPoint_FuseConnectionToolEnableChanged);
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