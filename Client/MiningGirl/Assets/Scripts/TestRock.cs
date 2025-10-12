using DG.Tweening;
using UnityEngine;

public class TestRock : GameInitializer, IHit
{
   private RectTransform _rect;
   
   public void Damage()
   {
      _rect ??= GetComponent<RectTransform>();
      _rect.DOShakePosition(0.3f, 10.0f);
   }

   public Vector2 GetPosition()
   {
      _rect ??= GetComponent<RectTransform>();
      return _rect.anchoredPosition;
   }
}