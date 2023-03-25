using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/ErasableBehavior")]
    //[DisallowMultipleComponent()]
    public class ErasableBehavior : MonoBehaviour, IErasable
    {
        private CancellationToken _cancellationTokentoken;
        private bool _isErasing = false;

        private void Awake()
        {
            _cancellationTokentoken = this.GetCancellationTokenOnDestroy();
        }

        public async void Erase()
        {
            if (_isErasing == false)
            {
                _isErasing = true;
                await EraseAsync(_cancellationTokentoken);
            }
        }

        private async UniTask EraseAsync(CancellationToken token)
        {
            await this.transform.DOShakeScale(.15f, 0.7f, 5, 50f, true).WithCancellation(token);
            token.ThrowIfCancellationRequested();
            await this.transform.DOScale(0f, UnityEngine.Random.Range(.1f, .2f)).WithCancellation(token);
            token.ThrowIfCancellationRequested();

            Destroy(this.gameObject);
        }
    }
}
