using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill.UI
{
    public interface ISkillPointGaugeUIHandler
    {
        int GetSkillPoint();
        void UpdateGaugeUI(int addSkillPoint);
    }
    
    public class SkillPointGaugeUI : GameInitializer, ISkillPointGaugeUIHandler
    { 
        [SerializeField]
        private TMP_Text curSkillPointText;
        [SerializeField] 
        private TMP_Text maxSkillPointText;
        [SerializeField]
        private Image gaugeUI;
        
        [Header("Option")]
        [SerializeField] 
        private float startPosY = -120.0f;
        [SerializeField] 
        private float endPosY = 32.0f;
        [SerializeField]
        private float duration = 0.5f;
        
        private int _maxSkillPoint;
        private RectTransform _rect;
        private int _skillPoint;

        public void Init(int maxPoint)
        {
            _skillPoint = 0;
            _maxSkillPoint = maxPoint;
            _rect ??= GetComponentInParent<RectTransform>();
            _rect.anchoredPosition = new Vector2(0, startPosY);
            
            Refresh();
        }

        public void Appear()
        {
            _rect.DOAnchorPosY(endPosY, duration);
        }
        
        private void Refresh()
        {
            var ratio = Mathf.Clamp(_skillPoint / (float)_maxSkillPoint, 0, _maxSkillPoint);
            
            gaugeUI.DOFillAmount(ratio, duration);

            curSkillPointText.text = $"{_skillPoint}";
            maxSkillPointText.text = $"{_maxSkillPoint}";
        }

#region ISkillPointGaugeUIHandler

        public int GetSkillPoint()
        {
            return _skillPoint;
        }

        public void UpdateGaugeUI(int addSkillPoint)
        {
            _skillPoint = math.clamp(_skillPoint + addSkillPoint, 0, _maxSkillPoint);
            Refresh();
        }

#endregion
    }
}