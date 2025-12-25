using Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InGame.System.Skill.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class SkillCardDrag : GameInitializer, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform Rect { get; set; }
        private CanvasGroup CanvasGroup  { get; set; }
        private SkillCardLogic Logic { get; set; }
        private ISkillCardUIHandler UIHandler { get; set; }
        
        private Vector2 _startPos;
        private Vector2 _dragOffset;
        
        private void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        public void Init(SkillDataRowTable data, ISkillCardUIHandler skillCardUIHandler)
        {
            Logic = new SkillCardLogic(data, skillCardUIHandler);
            UIHandler = skillCardUIHandler;
            Rect = skillCardUIHandler.GetContentsRectTransform();
        }

        private Canvas GetCanvas()
        {
            return UIHandler.GetSkillUIControllerHandler().GetUICanvas();
        }

        private bool GetSelectState()
        {
            return Logic.IsSelected;
        }

#region Event Interfaces

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Input.touchCount > 1)
                return;
            
            if (GetCanvas() == null)
                return;

            // // 선택된 상태가 아니면 드래그 불가
            // if (!GetSelectState())
            //     return;

            _startPos = Rect.anchoredPosition;

            if (CanvasGroup != null)
                CanvasGroup.blocksRaycasts = false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GetCanvas().transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint
            );

            Rect.SetAsLastSibling();
            Logic.StartDrag();
            _dragOffset = Rect.anchoredPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (GetCanvas() == null || !GetSelectState())
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GetCanvas().transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint
            );

            Logic.Drag(Rect.anchoredPosition.y - _startPos.y);
            UIHandler.MoveContents(localPoint + _dragOffset, false);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // if (GetCanvas() == null || !GetSelectState())
            //     return;

            if (CanvasGroup != null)
                CanvasGroup.blocksRaycasts = true;
            
            Logic.EndDrag(Rect.anchoredPosition.y - _startPos.y, () => UIHandler.MoveContents(_startPos, true));
        }

#endregion
    }
}