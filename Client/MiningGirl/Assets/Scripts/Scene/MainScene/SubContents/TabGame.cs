using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scene.MainScene.SubContents
{
    public class TabGame : GameInitializer, IPointerClickHandler
    {
        [SerializeField] 
        private Slider slider;
        [SerializeField] 
        private Image hitImage;
        
        private RectTransform _rect;
        private Tween _shakeTween;
        private Tween _sliderTween;
        private Vector2 _origin;
        private int _hp;
        private int _maxHp;
        private bool _isDead;
        
        private void Start()
        {
            _rect = GetComponent<RectTransform>();
            _origin = _rect.anchoredPosition;
        }

        public void Initialize()
        {
            _isDead = false;
            _maxHp = 30;
            _hp = _maxHp;
            slider.value = 1;
            hitImage.DOFade(0.0f, 0.0f);
        }

        public async void OnPointerClick(PointerEventData eventData)
        {
            if (Input.touchCount > 1)
                return;

            if (_isDead)
                return;
            
            _hp -= 1;
            _sliderTween = slider.DOValue(_hp / (float)_maxHp, 0.2f);
            SoundManager.Instance.PlaySfx("Hit1");
            
            if (_hp <= 0)
            {
                _hp = _maxHp;
                _isDead = true;
                
                await UniTask.WaitForSeconds(0.2f);
                
                slider.value = 1;
                _isDead = false;
                return;
            }
            
            _shakeTween?.Kill();
            _sliderTween = slider.DOValue(_hp / (float)_maxHp, 0.2f);
            _rect.anchoredPosition = _origin;
            _shakeTween = _rect.DOShakeAnchorPos(
                duration: 0.1f,
                strength: new Vector3(60f, 0f, 0f),  // X만
                vibrato: 10,
                randomness: 90,
                snapping: false,
                fadeOut: true
            ).SetRelative(true);
            
            hitImage.DOFade(0.5f, 0.0f);
            hitImage.DOFade(0.0f, 0.2f);
        }
    }
}
