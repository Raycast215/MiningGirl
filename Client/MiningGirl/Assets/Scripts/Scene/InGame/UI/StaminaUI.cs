using UI.Common;
using System;
using System;
using UnityEngine;

namespace MainGame.UI
{
    // 스태미나 표시. 채굴할 때도, 몬스터에게 맞을 때도 줄어들고
    // 전부 소모하면 스테이지가 재시작됩니다.
    //
    // 아직 스태미나 시스템 자체가 없어서 지금은 표시 껍데기만 있습니다.
    // 소모/회복 로직이 생기면 SetValue를 호출해 주면 됩니다.
    public class StaminaUI : MonoBehaviour
    {
        [SerializeField]
        private GaugeBarView view;

        [SerializeField]
        [Tooltip("임시 최대치. 캐릭터 데이터와 연결되면 제거합니다.")]
        private float testMaxStamina = 100f;

        [SerializeField]
        [Tooltip("광물을 하나 캘 때 소모하는 스태미나")]
        private float miningCost = 10f;

        [SerializeField]
        [Tooltip("몬스터에게 한 번 맞을 때 소모하는 스태미나")]
        private float hitCost = 1f;

        // 스태미나가 바닥나면 한 번만 호출됩니다(스테이지 재시작).
        private Action _onEmpty;

        // 강화로 얻은 보정치를 읽어옵니다(최대치, 채굴 소모 감소, 피격 소모 감소).
        private Func<(float maxAdd, float maxMul, float miningReduce, float hitReduce)> _bonusProvider;

        public void SetBonusProvider(Func<(float, float, float, float)> provider)
        {
            _bonusProvider = provider;
        }

        // 강화를 반영한 최종 최대치
        private float GetFinalMax()
        {
            if (_bonusProvider == null)
                return testMaxStamina;

            var (add, mul, _, _) = _bonusProvider.Invoke();

            return Mathf.Max(1f, (testMaxStamina + add) * mul);
        }

        // 소모는 0 아래로 내려가지 않게 막습니다(소모 0이면 무한 채굴이 되므로 하한을 둡니다).
        private float GetMiningCost()
        {
            if (_bonusProvider == null)
                return miningCost;

            var (_, _, reduce, _) = _bonusProvider.Invoke();

            return Mathf.Max(1f, miningCost - reduce);
        }

        private float GetHitCost()
        {
            if (_bonusProvider == null)
                return hitCost;

            var (_, _, _, reduce) = _bonusProvider.Invoke();

            return Mathf.Max(0.1f, hitCost - reduce);
        }

        public void SetEmptyHandler(Action handler)
        {
            _onEmpty = handler;
        }

        // 광물 하나를 캤을 때
        public void ConsumeByMining()
        {
            Consume(GetMiningCost());
        }

        // 몬스터에게 맞았을 때
        public void ConsumeByHit()
        {
            Consume(GetHitCost());
        }

        public float Max { get; private set; }
        public float Current { get; private set; }

        // 0~1 진행도. 스태미나가 바닥나면 0입니다.
        public float Ratio => Max <= 0f ? 0f : Mathf.Clamp01(Current / Max);
        public bool IsEmpty => Current <= 0f;

        public void Init(float max = -1f)
        {
            Max = max > 0f ? max : GetFinalMax();
            Current = Max;

            view?.SetValue(Current, Max, true);
        }

        // 스테이지가 새로 시작되면 가득 채웁니다.
        public void Reset()
        {
            // 강화로 최대치가 올랐을 수 있으니 매 스테이지 다시 계산합니다.
            Max = GetFinalMax();
            Current = Max;

            view?.SetValue(Current, Max, true);
        }

        // 채굴·피격 양쪽에서 호출합니다. 0 아래로는 내려가지 않습니다.
        public void Consume(float amount)
        {
            if (amount <= 0f)
                return;

            var wasEmpty = IsEmpty;

            Current = Mathf.Max(0f, Current - amount);

            view?.SetValue(Current, Max);

            // 바닥나는 순간 한 번만 알립니다.
            if (!wasEmpty && IsEmpty)
                _onEmpty?.Invoke();
        }

        // 회복 카드 등으로 되돌릴 때 씁니다.
        public void Recover(float amount)
        {
            if (amount <= 0f)
                return;

            Current = Mathf.Min(Max, Current + amount);

            view?.SetValue(Current, Max);
        }

        public void SetPaused(bool paused)
        {
            view?.SetPaused(paused);
        }
    }
}
