using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI.Level
{
    public class LevelGaugeUI : GameInitializer
    {
        [SerializeField] 
        private Image sliderUI;
        [SerializeField]
        private TMP_Text levelText;

        private Tween _sliderTween;

        public void SetLevel(int level)
        {
            levelText.text = $"Lv.{level}";
        }
        
        public void SetValue(float value, Action callback = null)
        {
            if (value == 0)
            {
                sliderUI.fillAmount = value;
                callback?.Invoke();
                return;
            }

            if (_sliderTween != null)
            {
                _sliderTween.Pause();
                _sliderTween.Kill();
                _sliderTween = null;
            }
            
            _sliderTween = sliderUI.DOFillAmount(value, 0.2f).OnComplete(() => callback?.Invoke());
        }
    }
}