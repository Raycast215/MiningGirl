using System;
using UnityEngine;

namespace Scene.InGame.State
{
    // 클리어 조건. 목표 채굴량을 채우면 스테이지가 끝납니다(제한 시간 대신).
    public class MiningState
    {
        private readonly RunSettings _settings;

        public int Goal { get; private set; }
        public int Current { get; private set; }

        // 0~1 진행도. 코스트 후반 가속이 이 값을 기준으로 삼습니다.
        public float Progress => Goal <= 0 ? 0f : Mathf.Clamp01((float)Current / Goal);
        public bool IsGoalReached => Current >= Goal;

        public event Action OnChanged;

        // 목표를 채운 순간 한 번만.
        public event Action OnGoalReached;

        public MiningState(RunSettings settings)
        {
            _settings = settings ?? new RunSettings();

            SetGoalByStage(1);
        }

        // 스테이지가 오를수록 목표가 늘어납니다(1스테이지 10 → 2스테이지 15 …).
        public void SetGoalByStage(int stage)
        {
            Goal = _settings.MiningGoalBase + Mathf.Max(0, stage - 1) * _settings.MiningGoalPerStage;
            Current = 0;

            OnChanged?.Invoke();
        }

        // 광물을 하나 캘 때마다 호출합니다.
        public void Add(int amount = 1)
        {
            if (amount <= 0 || IsGoalReached)
                return;

            Current += amount;

            OnChanged?.Invoke();

            if (!IsGoalReached)
                return;

            OnGoalReached?.Invoke();
        }

        // 테스트용 — 남은 양을 한 번에 채웁니다.
        public void ForceComplete()
        {
            Add(Goal - Current);
        }
    }
}
