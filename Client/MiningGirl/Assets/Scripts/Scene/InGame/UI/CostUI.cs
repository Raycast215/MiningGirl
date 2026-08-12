using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UI.Common;
using UnityEngine;

namespace MainGame.UI
{
    // 코스트 자원을 관리하고 오브(CostOrbView)에 표시하는 UI 클래스.
    // 초당 1씩 회복하며 최대치까지 차오르고, 보스전에서는 최대치를 초과할 수 있습니다.
    public class CostUI : GameMonoInitializer, IDisposable
    {
        private event Action<int> OnCostChanged;

        [SerializeField]
        private CostOrbView orbView;

        [SerializeField]
        private int maxCost = 10;
        [SerializeField]
        [Tooltip("코스트 1이 차오르는 데 걸리는 시간(초)")]
        private float regenInterval = 3.0f;

        // 현재 보유 코스트
        public int Cost { get; private set; }
        // 최대치를 초과한 상태인지 (보스전 오버차지)
        public bool IsOvercharged => Cost > maxCost;

        private float _regenTimer;
        private CancellationTokenSource _cts;

        public void Init(Action<int> onCostChanged = null)
        {
            OnCostChanged = null;
            OnCostChanged += onCostChanged;

            SetCost(0);

            IsInitialized = true;
        }

        // 코스트를 지정 값으로 초기화합니다(스테이지 시작/리셋 등).
        public void SetCost(int cost)
        {
            Dispose();

            Cost = Mathf.Max(0, cost);
            _regenTimer = 0.0f;
            _cts = new CancellationTokenSource();

            // 리셋은 연출 없이 즉시 반영합니다.
            UpdateView(true);
        }

        public void StopProcess()
        {
            Dispose();
        }

        // 회복 루프 시작 — 이 시점부터 초당 1씩 코스트가 차오릅니다.
        public async UniTaskVoid Execute()
        {
            UpdateView();

            try
            {
                while (true)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: _cts.Token);

                    // 최대치를 넘은 상태에서는 자연 회복을 멈춥니다.
                    // (오버차지 감소 처리는 보스전 작업 시 여기에 추가 예정)
                    if (Cost >= maxCost)
                    {
                        _regenTimer = 0.0f;
                        UpdateView();
                        continue;
                    }

                    _regenTimer += Time.deltaTime;

                    while (_regenTimer >= regenInterval && Cost < maxCost)
                    {
                        _regenTimer -= regenInterval;
                        Cost++;
                        OnCostChanged?.Invoke(Cost);
                    }

                    UpdateView();
                }
            }
            catch (Exception _)
            {
                // 취소되면 조용히 종료합니다.
            }
        }

        // 코스트가 충분한지 확인합니다(카드 사용 가능 여부 판단용).
        public bool CanAfford(int amount)
        {
            return Cost >= amount;
        }

        // 코스트를 소모합니다. 부족하면 false를 반환하고 아무것도 하지 않습니다.
        public bool TrySpend(int amount)
        {
            if (amount < 0 || !CanAfford(amount))
                return false;

            Cost -= amount;
            OnCostChanged?.Invoke(Cost);
            UpdateView();

            return true;
        }

        // 코스트를 지급합니다. allowOvercharge가 true면 최대치를 초과할 수 있습니다(보스전).
        public void Add(int amount, bool allowOvercharge = false)
        {
            if (amount <= 0)
                return;

            Cost += amount;

            if (!allowOvercharge)
                Cost = Mathf.Min(Cost, maxCost);

            OnCostChanged?.Invoke(Cost);
            UpdateView();
        }

        private void UpdateView(bool immediate = false)
        {
            if (orbView == null)
                return;

            var progress = regenInterval <= 0.0f ? 0.0f : Mathf.Clamp01(_regenTimer / regenInterval);

            orbView.SetValue(Cost, progress, maxCost, immediate);
        }

        private void OnDestroy()
        {
            Dispose();
        }

#region Interface

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

#endregion
    }
}
