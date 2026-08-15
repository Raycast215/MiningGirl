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

        [SerializeField]
        [Tooltip("스테이지 후반에 적용되는 회복 속도 배율(2면 두 배 빨라짐)")]
        private float lateStageSpeedMultiplier = 2f;

        // 현재 보유 코스트
        public int Cost { get; private set; }
        // 최대치를 초과한 상태인지 (보스전 오버차지)
        public bool IsOvercharged => Cost > maxCost;

        private float _regenTimer;
        private bool _isPaused;

        // 후반 가속이 켜졌는지
        private bool _isSpeedUp;
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
            // 스테이지가 새로 시작되면 가속도 초기화됩니다.
            _isSpeedUp = false;

            Dispose();

            Cost = Mathf.Max(0, cost);
            _regenTimer = 0.0f;
            _cts = new CancellationTokenSource();

            // 리셋은 연출 없이 즉시 반영합니다.
            UpdateView(true);
        }

        // 팝업 등으로 게임을 잠시 멈출 때 사용합니다(보유 코스트는 유지됩니다).
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
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

                    if (_isPaused)
                        continue;

                    // 최대치를 넘은 상태에서는 자연 회복을 멈춥니다.
                    // (오버차지 감소 처리는 보스전 작업 시 여기에 추가 예정)
                    if (Cost >= maxCost)
                    {
                        _regenTimer = 0.0f;
                        UpdateView();
                        continue;
                    }

                    _regenTimer += Time.deltaTime;

                    var interval = CurrentRegenInterval;

                    while (_regenTimer >= interval && Cost < maxCost)
                    {
                        _regenTimer -= interval;
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

        // 지금 적용 중인 회복 간격. 후반에는 배율만큼 짧아집니다.
        private float CurrentRegenInterval
        {
            get
            {
                if (!_isSpeedUp)
                    return regenInterval;

                var multiplier = Mathf.Max(0.01f, lateStageSpeedMultiplier);

                return Mathf.Max(0.05f, regenInterval / multiplier);
            }
        }

        // 스테이지 후반 코스트 가속을 켜고 끕니다.
        public void SetSpeedUp(bool value)
        {
            if (_isSpeedUp == value)
                return;

            _isSpeedUp = value;

            Debug.Log($"[Cost] 회복 가속 {(value ? "ON" : "OFF")} — 간격 {CurrentRegenInterval:F2}초");
        }

        public bool IsSpeedUp => _isSpeedUp;

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
