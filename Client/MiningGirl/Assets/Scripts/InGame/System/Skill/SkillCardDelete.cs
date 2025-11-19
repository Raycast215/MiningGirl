using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Skill
{
    public class SkillCardDelete : GameInitializer
    {
        public bool IsActivated { get; private set; }
        
        [SerializeField] 
        private Image gradientImage;
        [SerializeField] 
        private float durationTime = 0.2f;

        private bool _isShow;
        
        private void OnEnable()
        {
            IsActivated = true;
        }

        private void OnDisable()
        {
            IsActivated = false;
        }

        public void Show()
        {
            if (_isShow)
                return;

            _isShow = true;
            
            gameObject.SetActive(true);
            gradientImage.DOFade(0.0f, 0.0f);
            gradientImage.DOFade(1.0f, durationTime);
        }

        public void Hide()
        {
            if (!_isShow)
                return;

            _isShow = false;
            
            gradientImage.DOFade(0.0f, durationTime).OnComplete(() => gameObject.SetActive(false));
        }
    }
}