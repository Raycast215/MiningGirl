using DG.Tweening;
using InGame.System.Skill.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InGame.System.Skill
{
    public class SkillCardDrag : GameInitializer, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField]
        private RectTransform rectTransform;
        [SerializeField]
        private CanvasGroup canvasGroup;

        private Canvas _canvas;
        private Vector2 dragOffset;   
        private Vector2 _startPos;
        private bool _isSelected;

        private ISkillCardHandler _handler;
        
        public void Init(ISkillCardHandler handler)
        {
            _handler = handler;
            _canvas = _handler.GetUICanvas();
            _isSelected = false;
        }

        public void Reset()
        {
            _isSelected = false;
        }
        
#region Interface

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvas == null) 
                return;

            if (!_isSelected)
                return;
            
            _startPos = rectTransform.anchoredPosition;
            
            // 다른 UI로의 Raycast 막고 싶으면
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            // 👉 기준은 항상 Canvas의 RectTransform
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint
            );

            rectTransform.SetAsLastSibling();
            
            // 현재 이미지 anchoredPosition과 터치 위치의 차이 저장
            dragOffset = rectTransform.anchoredPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null) 
                return;

            if (!_isSelected)
                return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint
            );
            
            rectTransform.anchoredPosition = localPoint + dragOffset;

            // 카드 제거 위치 
            if (rectTransform.anchoredPosition.y - _startPos.y < -20.0f)
                _handler.GetSkillCardDelete().Show();
            else
                _handler.GetSkillCardDelete().Hide();

            // 스킬 발동 조건!
            if (rectTransform.anchoredPosition.y - _startPos.y > 700.0f)
            {
                Debug.Log($"스킬 발동 가능!");
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            if (!_isSelected)
                return;

            // 카드 제거 위치 
            if (rectTransform.anchoredPosition.y - _startPos.y < -20.0f)
            {
                Debug.Log($"스킬 제거!");
                _handler.DeleteSkillCard();
            }
            
            // 스킬 발동 조건!
            if (rectTransform.anchoredPosition.y - _startPos.y > 700.0f)
            {
                Debug.Log($"스킬 발동!");
                
                // 스킬 사용처리.
                _handler.ExecuteSkillEffect();
                
                // 임시
                rectTransform.DOAnchorPos(_startPos, 0.2f);
                _handler.GetSkillCardDelete().Hide();
            }
            else
            {
                rectTransform.DOAnchorPos(_startPos, 0.2f);
                _handler.GetSkillCardDelete().Hide();
                _handler.OnDeselectCard();
                Reset();
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_canvas == null) 
                return;

            if (_isSelected)
            {
                Reset();
                _handler.OnDeselectCard();
                _handler.GetSkillCardDelete().Hide();
                return;
            }
            
            _handler.OnSelectCard();
            _isSelected = true;
        }

#endregion
    }
}