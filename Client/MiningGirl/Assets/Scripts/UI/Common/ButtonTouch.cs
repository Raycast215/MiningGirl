using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Common
{
    public class ButtonTouch : GameInitializer, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        [SerializeField] 
        private Transform iconTransform;

        [Header("Option")]
        [SerializeField]
        private float animationDuration = 0.1f;
        [SerializeField] 
        private float toScale = 0.9f;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            iconTransform.DOScale(toScale, animationDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            iconTransform.DOScale(1.0f, animationDuration);
        }
    }
}