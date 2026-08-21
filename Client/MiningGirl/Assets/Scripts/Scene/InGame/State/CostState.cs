using System;
using UnityEngine;

namespace Scene.InGame.State
{
    // 카드 사용 자원(코스트). 시간이 지나면 저절로 차오릅니다.
    //
    // 예전에는 CostUI 안에서 UniTask 무한 루프로 돌았습니다.
    // 그러면 회복 속도를 검증하려면 실제로 몇 초를 기다려야 했습니다.
    // 지금은 Tick(deltaTime)을 원하는 만큼 불러서 즉시 확인할 수 있습니다.
    //   예) 3초치를 한 번에: state.Tick(3f) → Cost가 1 올라야 정상
    public class CostState
    {
        private readonly RunSettings _settings;

        private float _regenTimer;

        public int Cost { get; private set; }
        public int Max => _settings.MaxCost;

        // 최대치를 초과한 상태인지(보스전 오버차지)
        public bool IsOvercharged => Cost > _settings.MaxCost;

        // 스테이지 후반 가속이 켜졌는지
        public bool IsSpeedUp { get; private set; }

        // 다음 1이 차오르기까지의 진행도(0~1). 오브 연출이 씁니다.
        public float RegenProgress
        {
            get
            {
                var interval = CurrentRegenInterval;

                return interval <= 0f ? 0f : Mathf.Clamp01(_regenTimer / interval);
            }
        }

        // 지금 적용 중인 회복 간격. 후반에는 배율만큼 짧아집니다.
        public float CurrentRegenInterval
        {
            get
            {
                if (!IsSpeedUp)
                    return _settings.CostRegenInterval;

                var multiplier = Mathf.Max(0.01f, _settings.CostLateSpeedMultiplier);

                return Mathf.Max(0.05f, _settings.CostRegenInterval / multiplier);
            }
        }

        public event Action<int> OnCostChanged;
        public event Action OnChanged;

        public CostState(RunSettings settings)
        {
            _settings = settings ?? new RunSettings();
        }

        // 스테이지 시작/리셋. 가속도 함께 풀립니다.
        public void Reset(int cost = 0)
        {
            IsSpeedUp = false;
            Cost = Mathf.Max(0, cost);
            _regenTimer = 0f;

            OnChanged?.Invoke();
        }

        // 시간을 흘려보냅니다. 호출하지 않으면 회복이 멈춥니다(일시정지·스테이지 종료).
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            // 최대치를 넘은 상태에서는 자연 회복을 멈춥니다.
            // (오버차지 감소 처리는 보스전 작업 때 여기에 붙습니다)
            if (Cost >= _settings.MaxCost)
            {
                _regenTimer = 0f;

                return;
            }

            _regenTimer += deltaTime;

            var interval = CurrentRegenInterval;
            var gained = false;

            while (_regenTimer >= interval && Cost < _settings.MaxCost)
            {
                _regenTimer -= interval;
                Cost++;
                gained = true;

                OnCostChanged?.Invoke(Cost);
            }

            if (gained)
                OnChanged?.Invoke();
        }

        public void SetSpeedUp(bool value)
        {
            if (IsSpeedUp == value)
                return;

            IsSpeedUp = value;
        }

        // 카드를 쓸 수 있는지
        public bool CanAfford(int amount) => Cost >= amount;

        // 부족하면 false를 돌려주고 아무것도 하지 않습니다.
        public bool TrySpend(int amount)
        {
            if (amount < 0 || !CanAfford(amount))
                return false;

            Cost -= amount;

            OnCostChanged?.Invoke(Cost);
            OnChanged?.Invoke();

            return true;
        }

        // allowOvercharge가 true면 최대치를 넘길 수 있습니다(보스전).
        public void Add(int amount, bool allowOvercharge = false)
        {
            if (amount <= 0)
                return;

            Cost += amount;

            if (!allowOvercharge)
                Cost = Mathf.Min(Cost, _settings.MaxCost);

            OnCostChanged?.Invoke(Cost);
            OnChanged?.Invoke();
        }
    }
}
