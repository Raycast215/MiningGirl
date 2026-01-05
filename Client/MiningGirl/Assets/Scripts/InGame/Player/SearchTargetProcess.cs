using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace InGame.Player
{
    public class SearchTargetProcess : IDisposable
    {
        public IHit Target { get; private set; }
        
        private IUnitInfoHandler _handler;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isProcessing;
        
        public SearchTargetProcess(IUnitInfoHandler handler)
        {
            _handler = handler;
            _isProcessing = false;
        }

        public async UniTaskVoid Process()
        {
            _cts ??= new CancellationTokenSource();
            _isProcessing = true;

            while (_isProcessing)
            {
                var enemyList = _handler.GetEnemyList();
                
                if (enemyList == null || enemyList.Count == 0)
                {
                    Target = null;
                    await UniTask.WaitForSeconds(0.1f, cancellationToken: _cts.Token);
                    continue;
                }
                
                if (_cts == null || _cts.IsCancellationRequested)
                    return;
                
                var playerPos = _handler.GetPlayerTransform().position;
                var nearEnemy = enemyList
                    .Where(x => x.GetActiveState())
                    .OrderBy(x => (x.GetPosition() - playerPos).sqrMagnitude)
                    .FirstOrDefault();
                
                Target = nearEnemy;
              
                await UniTask.WaitForSeconds(0.1f, cancellationToken: _cts.Token); // 0.1초마다 재탐색
            }
        }

#region IDisposable

        public void Dispose()
        {
            _isProcessing = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

#endregion
    }
}
