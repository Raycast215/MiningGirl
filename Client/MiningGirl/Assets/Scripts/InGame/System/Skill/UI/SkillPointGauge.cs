using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill.UI
{
    public class SkillPointGauge : GameInitializer
    {
       [SerializeField]
       private TMP_Text curSkillPointText;
       [SerializeField] 
       private TMP_Text maxSkillPointText;
       [SerializeField]
       private Image gaugeUI;

       private int _curSkillPoint;
       private int _maxSkillPoint;
       private RectTransform _rect;
       
       public void Init(int maxPoint)
       {
           _curSkillPoint = 0;
           _maxSkillPoint = maxPoint;
           _rect ??= GetComponentInParent<RectTransform>();
           _rect.anchoredPosition = new Vector2(0, -100.0f);
           
           Refresh();
       }

       public void Appear()
       {
           _rect.DOAnchorPosY(32.0f, 0.5f);
       }

       public void UpdateGaugeUI(int addSkillPoint)
       {
           _curSkillPoint = math.clamp(_curSkillPoint + addSkillPoint, 0, _maxSkillPoint);
           Refresh();
       }

       private void Refresh()
       {
           var ratio = Mathf.Clamp(_curSkillPoint / (float)_maxSkillPoint, 0, _maxSkillPoint);
           
           gaugeUI.DOFillAmount(ratio, 0.5f);

           curSkillPointText.text = $"{_curSkillPoint}";
           maxSkillPointText.text = $"{_maxSkillPoint}";
       }
    }
}