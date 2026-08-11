using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CoverUI
{
    public class CoverUI : GameMonoInitializer
    {
        [SerializeField] 
        private Image backImage;
        [SerializeField] 
        private Image iconImage;
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
            
            gameObject.SetActive(false);
            
            IsInitialized = true;
        }

        public async UniTaskVoid Show(Action callback = null)
        {
            await UniTask.WaitUntil(() => IsInitialized);
            
            gameObject.SetActive(true);

            // 이전에 진행 중이던 페이드 트윈이 남아 있으면 정리합니다.
            // (남은 트윈이 뒤늦게 알파를 튕겨 올려 텍스트가 '툭' 나타나는 현상 방지)
            iconImage.DOKill();
            loadingText.DOKill();
            backImage.DOKill();

            iconImage.DOFade(1.0f, duration).SetDelay(duration);
            loadingText.DOFade(1.0f, duration).SetDelay(duration);
            backImage.DOFade(1.0f, duration)
                .OnComplete(() =>
                {
                    callback?.Invoke();
                    _isShowFinished = true;
                });
        }

        public async UniTaskVoid Hide(Action callback = null)
        {
            await UniTask.WaitUntil(() => IsInitialized);
            await UniTask.WaitUntil(() => _isShowFinished);
            
            // 페이드 아웃 전에 진행 중이던 페이드 인 트윈을 반드시 정리합니다.
            // (아직 페이드 인 중인 텍스트에 페이드 아웃을 겹쳐 걸면 알파가 충돌해
            //  텍스트가 '툭' 나타났다가 사라지는 현상이 생깁니다.)
            iconImage.DOKill();
            loadingText.DOKill();
            backImage.DOKill();

            iconImage.DOFade(0.0f, duration);
            loadingText.DOFade(0.0f, duration);
            backImage.DOFade(0.0f, duration)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    callback?.Invoke();
                });
        }
    }
}