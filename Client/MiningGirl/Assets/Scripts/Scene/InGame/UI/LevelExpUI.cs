using System;
using MainGame.Entity;
using UI.Common;
using UnityEngine;

namespace MainGame.UI
{
    // 레벨과 경험치를 관리하고 LevelExpView에 표시를 위임합니다.
    // 경험치 테이블은 현재 테스트용으로 "레벨당 고정 요구량"만 사용합니다.
    // (추후 엑셀 데이터 기반 테이블로 교체 예정)
    public class LevelExpUI : GameMonoInitializer
    {
        private event Action<int> OnLevelUp;

        [SerializeField]
        private LevelExpView view;

        [SerializeField]
        [Tooltip("테스트용 경험치 테이블 — 레벨업에 필요한 고정 경험치량")]
        private int expPerLevel = 10;

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int RequiredExp => expPerLevel;

        public void Init(Action<int> onLevelUp = null)
        {
            OnLevelUp = null;
            OnLevelUp += onLevelUp;

            Reset();

            IsInitialized = true;
        }

        // 레벨/경험치를 초기 상태로 되돌립니다(스테이지 리셋 등).
        public void Reset()
        {
            Level = 1;
            Exp = 0;

            // 리셋은 트윈 없이 즉시 반영합니다.
            UpdateView(true);
        }

        // 경험치를 획득합니다. 요구량을 넘으면 레벨이 오르고 남은 경험치는 이월됩니다.
        public void AddExp(int amount)
        {
            if (amount <= 0)
                return;

            Exp += amount;

            while (expPerLevel > 0 && Exp >= expPerLevel)
            {
                Exp -= expPerLevel;
                Level++;

                OnLevelUp?.Invoke(Level);
            }

            UpdateView();
        }

        private void UpdateView(bool immediate = false)
        {
            if (view == null)
                return;

            view.SetValue(Level, Exp, expPerLevel, immediate);
        }
    }
}
