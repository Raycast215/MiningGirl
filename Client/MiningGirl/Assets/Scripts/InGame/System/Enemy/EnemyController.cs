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
      [SerializeField]
      private SpriteRenderer hitRenderer;

      [SerializeField] 
      private GameObject test;
      
      private float _health = 3;
      private CancellationTokenSource _cts;
      private IEnemyHandler _handler;
      private bool _isDead;
      
      public void Initialize(IEnemyHandler handler)
      {
         _isDead = false;
         _handler = handler;
         hitRenderer.DOFade(0.0f, 0.0f);
         test.SetActive(false);
      }
      
      public void SetPosition(Vector2 position)
      {
         var pos = new Vector3(position.x, position.y, 0);
            
         transform.position = pos;
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

      public void Damage(float damage)
      {
         if (_isDead)
            return;
         
         _health -= damage;

         if (_health <= 0)
         {
            test.SetActive(true);
            test.transform.SetParent(null);
            gameObject.SetActive(false);
            _handler.IncreaseOreCount(1);
            _isDead = true;
            return;
         }
       
         transform.DOShakePosition(
            duration: 0.1f,
            strength: new Vector3(0.2f, 0f, 0f),  // X만
            vibrato: 10,
            randomness: 90,
            snapping: false,
            fadeOut: true
         ).SetRelative(true);
         
         hitRenderer.DOFade(0.5f, 0.0f);
         hitRenderer.DOFade(0.0f, 0.2f);
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