using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace InGame.System.Tile
{
    public class Tile : MonoBehaviour, IDisposable
    {
        private float _delay;
        private CancellationTokenSource _cts;
        
        public void SetPosition(Vector3 position)
        {
            var pos = new Vector3(position.x, position.y, 0);
            
            transform.position = pos;
        }

        public void SetDelay(float delay)
        {
            _delay = delay;
        }
        
        public async UniTaskVoid Drop()
        {
            Dispose();
            _cts ??= new CancellationTokenSource();

            try
            {
                var pos = transform.position;
                var to = new Vector3(pos.x, pos.y, 0);
                
                transform.position = new Vector3(pos.x, pos.y + 100, 0);
                
                await UniTask.WaitForSeconds(_delay, cancellationToken: _cts.Token);
                
                gameObject.SetActive(true);
                transform.DOLocalMove(to, 0.5f);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("타일 드랍 취소");
            }
            catch (Exception e)
            {
               Debug.LogException(e);
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

#region IDisposable

        public void Dispose()
        {
            if (_cts == null)
                return;
            
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

#endregion
    }
}