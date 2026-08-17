using System;
using UI.Common;
using UnityEngine;

namespace MainGame.UI
{
    // 목표 채굴량 대비 현재 채굴량 표시. 목표를 채우면 스테이지 클리어입니다.
    // (제한 시간을 대신하는 새 클리어 조건)
    public class MiningProgressUI : MonoBehaviour
    {
        [SerializeField]
        private GaugeBarView view;

        [SerializeField]
        [Tooltip("스테이지 1의 목표 채굴량")]
        private int baseGoal = 10;

        [SerializeField]
        [Tooltip("스테이지가 오를 때마다 목표에 더해지는 양")]
        private int goalPerStage = 10;

        // 목표를 채운 순간 한 번만 호출됩니다.
        private Action _onGoalReached;

        public int Goal { get; private set; }
        public int Current { get; private set; }

        // 0~1 진행도. 코스트 후반 가속이 이 값을 기준으로 삼습니다.
        public float Progress => Goal <= 0 ? 0f : Mathf.Clamp01((float)Current / Goal);
        public bool IsGoalReached => Current >= Goal;

        public void Init(Action onGoalReached, int goal = -1)
        {
            _onGoalReached = onGoalReached;

            SetGoal(goal);
        }

        // 스테이지가 바뀌면 목표를 새로 받고 진행도를 0으로 되돌립니다.
        // 스테이지 번호를 넘기면 목표가 그만큼 늘어납니다(1스테이지 10 → 2스테이지 20 …).
        public void SetGoalByStage(int stage)
        {
            SetGoal(baseGoal + Mathf.Max(0, stage - 1) * goalPerStage);
        }

        public void SetGoal(int goal = -1)
        {
            Goal = goal > 0 ? goal : baseGoal;
            Current = 0;

            view?.SetValue(Current, Goal, true);
        }

        // 광물을 하나 캘 때마다 호출합니다.
        public void Add(int amount = 1)
        {
            if (amount <= 0 || IsGoalReached)
                return;

            Current += amount;

            view?.SetValue(Current, Goal);

            if (!IsGoalReached)
                return;

            // 목표 달성 — 클리어 처리는 받는 쪽에 맡깁니다.
            _onGoalReached?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            view?.SetPaused(paused);
        }
    }
}
