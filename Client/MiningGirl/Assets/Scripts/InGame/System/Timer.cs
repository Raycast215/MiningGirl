using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        
        public void Init(float time, Action onFinished)
        {
            OnFinished = null;
            OnFinished += onFinished;
            
            Dispose();
            _time = time;
            _cts = new CancellationTokenSource();
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