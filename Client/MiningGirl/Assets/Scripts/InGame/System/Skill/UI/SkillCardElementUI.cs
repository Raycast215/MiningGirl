using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill.UI
{
    public class SkillCardElementUI : GameInitializer
    {
        private event Action<int> OnTouched;
        private event Action<SkillCardElementUI> OnSkillUsed;
        
        public int Index { get; private set; }
        public int SkillCost { get; private set; }
        
        [SerializeField] 
        private Image skillIconImage;
        [SerializeField] 
        private TMP_Text skillCostText;
        [SerializeField] 
        private TMP_Text skillNameText;
        [SerializeField] 
        private RectTransform contents;
        [SerializeField] 
        private Button touchButton;

        private bool _isSelected;
        
        private void Awake()
        {
            touchButton.onClick.RemoveAllListeners();
            touchButton.onClick.AddListener(OnTouchButton);
        }

        public void Init(int index, Action<int> onTouched, Action<SkillCardElementUI> onSkillUsed)
        {
            OnTouched = null;
            OnTouched += onTouched;

            OnSkillUsed = null;
            OnSkillUsed += onSkillUsed;
            
            Index = index;
            SkillCost = 3;
            contents.transform.localScale = Vector3.one;
        }

        public void UnTouch()
        {
            _isSelected = false;
            contents.transform.localScale = Vector3.one;
        }
        
        private void OnTouchButton()
        {
            if (_isSelected)
            {
                _isSelected = false;
                contents.DOScale(1.0f, 0.2f);
                OnSkillUsed?.Invoke(this);
                return;
            }
            
            _isSelected = true;
            OnTouched?.Invoke(Index);
            contents.DOScale(1.2f, 0.2f);
        }
    }
}