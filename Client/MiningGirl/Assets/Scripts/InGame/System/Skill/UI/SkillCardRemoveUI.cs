using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill.UI
{
    public interface ISkillCardRemoveUIHandler
    {
        void ShowCardRemoveUI();
        void HideCardRemoveUI();
    }
    
    public class SkillCardRemoveUI : GameInitializer, ISkillCardRemoveUIHandler
    {
        [SerializeField] 
        private Image gradientImage;
        
        [Header("Option")]
        [SerializeField] 
        private float durationTime = 0.2f;
        [SerializeField]
        private float showDelay = 0.1f;

        private bool _isShow;
        
#region ISkillCardRemoveUIHandler

        public void ShowCardRemoveUI()
        {
            if (_isShow)
                return;

            _isShow = true;
            
            gameObject.SetActive(true);
            gradientImage.DOFade(0.0f, 0.0f);
            gradientImage.DOFade(1.0f, durationTime).SetDelay(showDelay);
        }

        public void HideCardRemoveUI()
        {
            if (!_isShow)
                return;

            _isShow = false;
            
            gradientImage
                .DOFade(0.0f, durationTime)
                .OnComplete(() => gameObject.SetActive(false));
        }
        
#endregion
    }
}