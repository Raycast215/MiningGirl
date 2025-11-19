using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill.UI
{
    public class SkillPointGauge : GameInitializer
    { 
        public int SkillPoint { get; private set; }
        
        [SerializeField]
        private TMP_Text curSkillPointText;
        [SerializeField] 
        private TMP_Text maxSkillPointText;
        [SerializeField]
        private Image gaugeUI;

        private int _maxSkillPoint;
        private RectTransform _rect;

        public void Init(int maxPoint)
        {
            SkillPoint = 0;
            _maxSkillPoint = maxPoint;
            _rect ??= GetComponentInParent<RectTransform>();
            _rect.anchoredPosition = new Vector2(0, -120.0f);
            
            Refresh();
        }

        public void Appear()
        {
            _rect.DOAnchorPosY(32.0f, 0.5f);
        }

        public void UpdateGaugeUI(int addSkillPoint)
        {
            SkillPoint = math.clamp(SkillPoint + addSkillPoint, 0, _maxSkillPoint);
            Refresh();
        }

        private void Refresh()
        {
            var ratio = Mathf.Clamp(SkillPoint / (float)_maxSkillPoint, 0, _maxSkillPoint);
            
            gaugeUI.DOFillAmount(ratio, 0.5f);

            curSkillPointText.text = $"{SkillPoint}";
            maxSkillPointText.text = $"{_maxSkillPoint}";
        }
    }
}