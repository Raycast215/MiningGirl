using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.System.Skill
{
    public class SkillController : GameInitializer, IDisposable
    {
        [SerializeField]
        private SkillPointGauge skillPointGauge;

        private bool _isGameStated;
        private CancellationTokenSource _cts;
        
        public void Init()
        {
            IsInitialized = false;
            _isGameStated = false;
            
            skillPointGauge.Init(10);
            
            IsInitialized = true;
        }

        public void Appear()
        {
            skillPointGauge.Appear();
        }

        public async UniTaskVoid ExecuteSkillPointGauge()
        {
            _isGameStated = true;
            Dispose();
            _cts ??= new CancellationTokenSource();
            
            try
            {
                while (_isGameStated)
                {
                    await UniTask.WaitForSeconds(1.0f, cancellationToken: _cts.Token);
                
                    skillPointGauge.UpdateGaugeUI(1);
                }
            }
            catch (OperationCanceledException)
            {
                _isGameStated = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isGameStated = false;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

#region IDisposable

        public void Dispose()
        {
            if (_cts == null)
                return;
            
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

#endregion
    }
}