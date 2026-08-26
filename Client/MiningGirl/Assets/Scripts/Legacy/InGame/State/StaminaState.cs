using System;
using UnityEngine;

namespace Legacy.Scene.InGame.State
{
    // 스태미나. 채굴할 때도 몬스터에게 맞을 때도 줄고, 바닥나면 스테이지 실패입니다.
    //
    // 예전에는 이 수치와 계산이 StaminaUI(MonoBehaviour) 안에 있어서,
    // 소모량이 맞는지 보려면 씬을 띄우고 게임을 돌려야 했습니다.
    // 지금은 new StaminaState(settings) 하고 Consume을 불러 값만 보면 됩니다.
    public class StaminaState
    {
        private readonly RunSettings _settings;

        // 강화로 얻은 보정치(최대치 가산, 최대치 배율, 채굴 소모 감소, 피격 소모 감소)
        private Func<(float maxAdd, float maxMul, float miningReduce, float hitReduce)> _bonusProvider;

        public float Max { get; private set; }
        public float Current { get; private set; }

        public float Ratio => Max <= 0f ? 0f : Mathf.Clamp01(Current / Max);
        public bool IsEmpty => Current <= 0f;

        // 값이 바뀔 때마다. 화면 갱신용입니다.
        public event Action OnChanged;

        // 바닥나는 순간 한 번만. 스테이지 실패 처리를 받는 쪽이 붙입니다.
        public event Action OnEmpty;

        public StaminaState(RunSettings settings)
        {
            _settings = settings ?? new RunSettings();

            Reset();
        }

        public void SetBonusProvider(Func<(float, float, float, float)> provider)
        {
            _bonusProvider = provider;

            // 보정치가 붙으면 최대치가 달라지므로 다시 채웁니다.
            Reset();
        }

        // 강화를 반영한 최종 최대치
        public float GetFinalMax()
        {
            if (_bonusProvider == null)
                return _settings.MaxStamina;

            var (add, mul, _, _) = _bonusProvider.Invoke();

            return Mathf.Max(1f, (_settings.MaxStamina + add) * mul);
        }

        // 소모는 하한을 둡니다. 0이 되면 무한 채굴이 되기 때문입니다.
        public float GetMiningCost()
        {
            if (_bonusProvider == null)
                return _settings.MiningStaminaCost;

            var (_, _, reduce, _) = _bonusProvider.Invoke();

            return Mathf.Max(1f, _settings.MiningStaminaCost - reduce);
        }

        public float GetHitCost()
        {
            if (_bonusProvider == null)
                return _settings.HitStaminaCost;

            var (_, _, _, reduce) = _bonusProvider.Invoke();

            return Mathf.Max(0.1f, _settings.HitStaminaCost - reduce);
        }

        // 스테이지가 새로 시작되면 가득 채웁니다.
        // 강화로 최대치가 올랐을 수 있으니 매번 다시 계산합니다.
        public void Reset()
        {
            Max = GetFinalMax();
            Current = Max;

            OnChanged?.Invoke();
        }

        public void Consume(float amount)
        {
            if (amount <= 0f)
                return;

            var wasEmpty = IsEmpty;

            Current = Mathf.Max(0f, Current - amount);

            OnChanged?.Invoke();

            // 바닥나는 순간 한 번만 알립니다.
            if (!wasEmpty && IsEmpty)
                OnEmpty?.Invoke();
        }

        public void ConsumeByMining() => Consume(GetMiningCost());
        public void ConsumeByHit() => Consume(GetHitCost());

        public void Recover(float amount)
        {
            if (amount <= 0f)
                return;

            Current = Mathf.Min(Max, Current + amount);

            OnChanged?.Invoke();
        }

        // 힐 카드용 — 최대치의 비율만큼 회복합니다.
        public void RecoverByRatio(float ratio)
        {
            if (ratio <= 0f)
                return;

            Recover(Max * ratio);
        }
    }
}
