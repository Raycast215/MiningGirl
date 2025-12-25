using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CoverUI
{
    public class CoverUI : GameInitializer
    {
        [SerializeField] 
        private Image backImage;
        [SerializeField] 
        private Image iconImage;
        [SerializeField] 
        private Image decoImage;
        [SerializeField]
        private TMP_Text loadingText;

        [Header("Option")]
        [SerializeField] 
        private float duration = 0.2f;

        private bool _isShowFinished;
        
        public void Initialize()
        {
            if (IsInitialized)
                return;
            
            gameObject.SetActive(true);

            _isShowFinished = false;
            
            backImage.DOFade(0.0f, 0.0f);
            iconImage.DOFade(0.0f, 0.0f);
            loadingText.DOFade(0.0f, 0.0f);
            decoImage.DOFade(0.0f, 0.0f);
            
            gameObject.SetActive(false);
            
            IsInitialized = true;
        }

        public async UniTaskVoid Show(Action callback = null)
        {
            await UniTask.WaitUntil(() => IsInitialized);
            
            gameObject.SetActive(true);
            
            iconImage.DOFade(1.0f, duration).SetDelay(duration);
            loadingText.DOFade(1.0f, duration).SetDelay(duration);
            decoImage.DOFade(1.0f, duration).SetDelay(duration);
            backImage.DOFade(1.0f, duration)
                .OnComplete(() =>
                {
                    callback?.Invoke();
                    _isShowFinished = true;
                });
        }

        public async UniTaskVoid Hide()
        {
            await UniTask.WaitUntil(() => IsInitialized);
            await UniTask.WaitUntil(() => _isShowFinished);
            
            iconImage.DOFade(0.0f, 0.2f);
            loadingText.DOFade(0.0f, 0.2f);
            decoImage.DOFade(0.0f, 0.2f);
            backImage.DOFade(0.0f, duration)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}