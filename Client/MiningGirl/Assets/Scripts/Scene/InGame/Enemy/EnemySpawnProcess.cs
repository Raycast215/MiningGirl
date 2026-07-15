using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Enemy
{
    public class EnemySpawnProcess : GameInitializer, IDisposable
    {
        private event Func<string, UniTask<IEntity>> OnGet;

        private CancellationTokenSource _cts;
        private float _time;
        private bool _isRunning;
        private float _timeScale;
        
        public void Init(Func<string, UniTask<IEntity>> onGet)
        {
            if (IsInitialized)
                return;
            
            OnGet += onGet;
            
            IsInitialized = true;
        }

        public async UniTask Execute()
        {
            if (_isRunning)
                return;
            
            Dispose();
            
            _time = 10.0f;
            _cts = new CancellationTokenSource();
            
            try
            {
                await Resume();
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Pause()
        {
            _isRunning = false;
            Dispose();
        }

        public async UniTask Resume()
        {
            if (_isRunning)
                return;
            
            _isRunning = true;
            _cts ??= new CancellationTokenSource();
            
            while (!_cts.IsCancellationRequested)
            {
                _time -= Time.deltaTime * _timeScale;
               
                if (_time < 0.0f)
                {
                    _time = 10.0f;
                    Debug.Log("Time!!");
                }
                    
                await UniTask.Yield(cancellationToken: _cts.Token);
            }
        }

        public void SetTimeScale(float scale = 1.0f)
        {
            _timeScale = scale;
        }

        public void Clear()
        {
            _time = 0.0f;
            _isRunning = false;
            Dispose();
        }

#region IDisposable
        
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

#endregion
    }
}