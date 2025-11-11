using DG.Tweening;
using UnityEngine;

namespace InGame.System.Enemy
{
   public class EnemyController : GameInitializer, IHit
   {
      private RectTransform _rect;
      private int _health = 3;
      
      private void Awake()
      {
         _rect ??= GetComponent<RectTransform>();
      }

      public void SetPosition(Vector2 position)
      {
         _rect.anchoredPosition = position;
      }
      
#region Interface

      public void Damage()
      {
         _health -= 1;
         _rect.DOShakePosition(0.3f, 10.0f);

         if (_health <= 0)
         {
            gameObject.SetActive(false);
         }
      }
      
      public Vector3 GetPosition()
      {
         return transform.position;
      }

      public Vector2 GetAnchoredPosition()
      {
         return _rect.localPosition;
      }

      public bool GetActiveState()
      {
         return gameObject.activeSelf;
      }

      #endregion
   }
}