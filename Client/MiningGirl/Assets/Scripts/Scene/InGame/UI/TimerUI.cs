using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace MainGame.UI
{
    public class TimerUI : GameMonoInitializer, IDisposable
    {
        private event Action OnFinished;
        
        public int Min { get; set; }
        public int Sec { get; set; }
        
        [SerializeField]
        private TMP_Text timerText;
        
        private float _time;
        private bool _isPaused;

        // 스테이지 전체 시간(진행도 계산용)
        private float _totalTime;

        // 0(시작) ~ 1(종료)까지의 진행도
        public float Progress => _totalTime <= 0f ? 0f : Mathf.Clamp01(1f - _time / _totalTime);
        private CancellationTokenSource _cts;
        private RectTransform _rect;
        
        public void Init(float time, Action onFinished)
        {
            OnFinished = null;
            OnFinished += onFinished;
            
            _rect ??= GetComponent<RectTransform>();

            SetTime(time);
        }

        public void SetTime(float time)
        {
            Dispose();
            
            _time = time;
            _totalTime = time;
            _cts = new CancellationTokenSource();
            
            UpdateTime();
        }

        // 팝업 등으로 게임을 잠시 멈출 때 사용합니다(진행 시간은 보존됩니다).
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        public void StopProcess()
        {
            Dispose();
        }
        
        public async UniTaskVoid Execute()
        {
            UpdateTime();
            await UniTask.WaitForSeconds(0.2f, cancellationToken: _cts.Token);
            
            try
            {
                while (_time > 0.0f)
                {
                    if (!_isPaused)
                        _time -= Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: _cts.Token);
                    UpdateTime();
                }
            }
            catch (Exception _)
            {
                return;
            }

            if (_cts == null || _cts.IsCancellationRequested)
                return;

            StopProcess();
            OnFinished?.Invoke();
        }

        private void UpdateTime()
        {
            var minutes = Mathf.FloorToInt(_time / 60.0f);
            var seconds = Mathf.FloorToInt(_time % 60.0f);

            Min = math.clamp(minutes, 0, 99);
            Sec = math.clamp(seconds, 0, 99);
            
            timerText.text = $"{Min:00} : {Sec:00}";
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