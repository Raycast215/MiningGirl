using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NotImplementedException = System.NotImplementedException;

namespace InGame.System.Skill.UI
{
    public interface ISkillCardHandler
    {
        public Canvas GetUICanvas();
        public void OnSelectCard();
        public void OnDeselectCard();
        public void ExecuteSkillEffect();
        public void DeleteSkillCard();
        public SkillCardDelete GetSkillCardDelete();
    }
    
    public class SkillCardElementUI : GameInitializer, ISkillCardHandler
    {
        public int Index { get; private set; }
      
        [SerializeField] 
        private Image skillIconImage;
        [SerializeField] 
        private TMP_Text skillCostText;
        [SerializeField] 
        private TMP_Text skillNameText;
        [SerializeField] 
        private RectTransform contents;
        [SerializeField]
        private SkillCardDrag dragHandler;

        private int _cost;
        private bool _isSelected;
        private ISkillControllerHandler _handler;

        public void Init(int index, ISkillControllerHandler handler)
        {
            Index = index;
            _handler = handler;
            _cost = 3;
            contents.transform.localScale = Vector3.one;
            contents.anchoredPosition = new Vector2(0, -800);
            dragHandler.Init(this);
        }

        public void Appear()
        {
            contents.DOAnchorPos(new Vector2(0, 0), 0.2f);
        }

#region ISkillCardHandler

        public Canvas GetUICanvas()
        {
            return _handler.GetUICanvas();
        }

        public void OnSelectCard()
        {
            _handler.OnSkillCardTouch(Index);
            contents.DOScale(1.2f, 0.2f);
        }

        public void OnDeselectCard()
        {
            contents.transform.localScale = Vector3.one;
            dragHandler.Reset();
        }

        public void ExecuteSkillEffect()
        {
            _handler.HideInfoUI();
            
            if (_handler.GetSkillPoint() >= _cost)
            {
                _handler.ExecuteSkillEffect(-_cost);
                return;
            }
            
            Debug.Log("스킬 포인트 부족...");
        }

        public void DeleteSkillCard()
        {
            _handler.HideInfoUI();
            
            if (_handler.GetSkillPoint() > 0)
            {
                _handler.ExecuteSkillEffect(-1);
                return;
            }
            
            Debug.Log("스킬 포인트 부족...");
        }

        public SkillCardDelete GetSkillCardDelete()
        {
            return _handler.GetSkillCardDelete();
        }

#endregion
    }
}