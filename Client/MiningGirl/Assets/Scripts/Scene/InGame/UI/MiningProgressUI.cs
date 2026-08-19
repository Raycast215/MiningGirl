using System;
using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 목표 채굴량 대비 현재 채굴량 표시. 목표를 채우면 스테이지 클리어입니다.
    // (제한 시간을 대신하는 새 클리어 조건)
    //
    // 게이지 바 없이 숫자만 보여줍니다.
    public class MiningProgressUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("'{0} / {1}' 형식. 앞이 현재 채굴량, 뒤가 목표입니다.")]
        private TMP_Text label;

        [SerializeField]
        private string format = "{0} / {1}";

        [SerializeField]
        [Tooltip("스테이지 1의 목표 채굴량")]
        private int baseGoal = 10;

        [SerializeField]
        [Tooltip("스테이지가 오를 때마다 목표에 더해지는 양")]
        private int goalPerStage = 5;

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
        // 스테이지 번호를 넘기면 목표가 그만큼 늘어납니다(1스테이지 10 → 2스테이지 15 …).
        public void SetGoalByStage(int stage)
        {
            SetGoal(baseGoal + Mathf.Max(0, stage - 1) * goalPerStage);
        }

        public void SetGoal(int goal = -1)
        {
            Goal = goal > 0 ? goal : baseGoal;
            Current = 0;

            Refresh();
        }

        // 광물을 하나 캘 때마다 호출합니다.
        public void Add(int amount = 1)
        {
            if (amount <= 0 || IsGoalReached)
                return;

            Current += amount;

            Refresh();

            if (!IsGoalReached)
                return;

            // 목표 달성 — 클리어 처리는 받는 쪽에 맡깁니다.
            _onGoalReached?.Invoke();
        }

        private void Refresh()
        {
            if (label != null)
                label.text = string.Format(format, Current, Goal);
        }

        // 게이지 트윈이 없어 멈출 것이 없지만,
        // 호출부가 스태미나와 같은 흐름을 쓰므로 형태를 맞춰 둡니다.
        public void SetPaused(bool paused)
        {
        }
    }
}
