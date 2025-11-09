using DG.Tweening;
using UnityEngine;

namespace InGame.System.Enemy
{
   public class EnemyController : GameInitializer, IHit
   {
      private RectTransform _rect;

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
         _rect.DOShakePosition(0.3f, 10.0f);
      }
      
      public Vector2 GetPosition()
      {
         return _rect.anchoredPosition;
      }

#endregion
   }
}