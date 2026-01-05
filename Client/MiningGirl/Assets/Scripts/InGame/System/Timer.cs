using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.System
{
    public class Timer : GameInitializer, IDisposable
    {
        private event Action OnFinished;
        
        public int Min { get; set; }
        public int Sec { get; set; }
        
        [SerializeField]
        private TMP_Text timerText;
        
        private float _time;
        private CancellationTokenSource _cts;
        private RectTransform _rect;
        
        public void Init(float time, Action onFinished)
        {
            OnFinished = null;
            OnFinished += onFinished;
            
            Dispose();
            _time = time;
            _cts = new CancellationTokenSource();
            _rect ??= GetComponent<RectTransform>();
            _rect.anchoredPosition = new Vector2(0.0f, 200.0f);
        }

        public void Appear()
        {
            _rect.DOAnchorPosY(0.0f, 0.5f);
        }

        public void StopProcess()
        {
            Dispose();
        }
        
        public async UniTaskVoid Execute()
        {
            UpdateTime();
            await UniTask.WaitForSeconds(1.0f, cancellationToken: _cts.Token);
            
            try
            {
                while (_time > 0.0f)
                {
                    _time -= Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: _cts.Token);
                    UpdateTime();
                }
            }
            catch (Exception _)
            {
                // return;
            }

            if (_cts == null || _cts.IsCancellationRequested)
                return;
            
            OnFinished?.Invoke();
        }

        private void UpdateTime()
        {
            var minutes = Mathf.FloorToInt(_time / 60.0f);
            var seconds = Mathf.FloorToInt(_time % 60.0f);

            Min = math.clamp(minutes, 0, 99);
            Sec = math.clamp(seconds, 0, 99);
            
            timerText.text = $"{Min:00}:{Sec:00}";
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

#region Iterface

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

#endregion
    }
}