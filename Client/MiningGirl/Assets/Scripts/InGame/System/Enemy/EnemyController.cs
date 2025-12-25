using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using InGame.System.Loader;
using UnityEngine;

namespace InGame.System.Enemy
{
   public class EnemyController : GameInitializer, IHit, IDisposable
   {
      private int _health = 3;
      private float _delay;
      private CancellationTokenSource _cts;
      private IEnemyHandler _handler;
      private bool _isDead;
      
      public void Initialize(IEnemyHandler handler)
      {
         _isDead = false;
         _handler = handler;
      }
      
      public void SetPosition(Vector2 position)
      {
         var pos = new Vector3(position.x, position.y, 0);
            
         transform.position = pos;
      }

      public void SetDelay(float delay)
      {
         _delay = delay;
      }

      public async void Drop()
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
      
#region IHit

      public void Damage()
      {
         if (_isDead)
            return;
         
         _health -= 1;

         if (_health <= 0)
         {
            gameObject.SetActive(false);
            _handler.IncreaseOreCount(1);
            _isDead = true;
            return;
         }
         
         transform.DOShakePosition(0.1f, 0.2f);
      }
      
      public Vector3 GetPosition()
      {
         return transform.position;
      }

      public bool GetActiveState()
      {
         return gameObject.activeSelf;
      }

      public Transform GetTransform()
      {
         return transform;
      }

#endregion
   }
}