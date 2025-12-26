using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace InGame.System.Result
{
    public class ResultFailed : GameInitializer
    {
        [Header("UI")]
        [SerializeField]
        private TMP_Text curMiningCountText;
        [SerializeField]
        private TMP_Text targetMiningCountText;
        [SerializeField]
        private TMP_Text messageText;
        
        [Header("Buttons")]
        [SerializeField]
        private Button retryButton;
        [SerializeField]
        private Button homeButton;

        private List<string> _messageList;
        private int _currentMiningCount;
        private int _targetCount;
        private IResultHandler _handler;

        public void Initialize(IResultHandler handler)
        {
            if (IsInitialized)
                return;
            
            _handler = handler;
            
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(_handler.OnRetry);
            
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(_handler.OnHome);
            
            _messageList = new List<string>()
            {
                "다시 도전해보자...",
                "아쉽지만, 다시 도전!",
                "조금만 더 하면 될 것 같아요!",
                "다시 도전해볼까요?",
                "아이 캔 두 디스 올데이.",
            };
            
            gameObject.SetActive(false);
            IsInitialized = true;
        }

        public void Set(int miningCount, int targetCount)
        {
            var random = Random.Range(0, _messageList.Count);
            
            messageText.text = _messageList[random];

            _currentMiningCount = miningCount;
            _targetCount = targetCount;
            
            curMiningCountText.text = "0";
            targetMiningCountText.text = $"{_targetCount}";
            gameObject.SetActive(true);
        }

#region AnimationEvent

        public async void NumberInterpolator()
        {
            var elapsed = 0f;
            var duration = 2.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var value = Mathf.RoundToInt(Mathf.Lerp(0, _currentMiningCount, elapsed / duration));
                curMiningCountText.text = $"{value}";
                await UniTask.Yield();
            }

            curMiningCountText.text = $"{_currentMiningCount}";
        }

#endregion
    }
}