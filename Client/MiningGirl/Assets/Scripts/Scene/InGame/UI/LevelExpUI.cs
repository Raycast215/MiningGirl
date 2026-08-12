using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UI.Common;
using UnityEngine;

namespace MainGame.UI
{
    // 레벨과 경험치를 관리하고 표시 시퀀스를 주도합니다.
    //
    // 데이터(Level/Exp)는 경험치를 받는 즉시 정확하게 갱신되지만,
    // 화면 표시는 한 레벨씩 순차로 따라갑니다.
    // 레벨업 연출이 한 단계 끝날 때마다 보너스 선택 콜백을 호출하고,
    // 선택이 끝날 때까지(continue 호출 전까지) 다음 연출을 시작하지 않습니다.
    public class LevelExpUI : GameMonoInitializer
    {
        // (도달한 레벨, 계속 진행 콜백)
        private event Action<int, Action> OnLevelUp;

        [SerializeField]
        private LevelExpView view;

        [SerializeField]
        [Tooltip("테스트용 경험치 테이블 — 레벨업에 필요한 고정 경험치량")]
        private int expPerLevel = 10;

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int RequiredExp => expPerLevel;

        // 화면에 표시 중인 레벨. 데이터보다 뒤처져 있으면 아직 보여줄 레벨업이 남은 것입니다.
        public int DisplayedLevel { get; private set; } = 1;
        public bool HasPendingLevelUp => Level > DisplayedLevel;

        private bool _isSequenceRunning;
        private bool _waitingForContinue;
        private CancellationTokenSource _cts;

        public void Init(Action<int, Action> onLevelUp = null)
        {
            OnLevelUp = null;
            OnLevelUp += onLevelUp;

            Reset();

            IsInitialized = true;
        }

        // 레벨/경험치를 초기 상태로 되돌립니다(스테이지 리셋 등).
        public void Reset()
        {
            StopSequence();

            Level = 1;
            Exp = 0;
            DisplayedLevel = 1;

            if (view != null)
                view.SetImmediate(Level, Exp, expPerLevel);
        }

        // 경험치를 획득합니다. 데이터는 즉시 반영되고, 표시는 시퀀스가 따라갑니다.
        public void AddExp(int amount)
        {
            if (amount <= 0)
                return;

            Exp += amount;

            while (expPerLevel > 0 && Exp >= expPerLevel)
            {
                Exp -= expPerLevel;
                Level++;
            }

            if (!_isSequenceRunning)
                RunSequence().Forget();
        }

        private void StopSequence()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _isSequenceRunning = false;
            _waitingForContinue = false;

            if (view != null)
                view.StopAnimation();
        }

        private async UniTaskVoid RunSequence()
        {
            _isSequenceRunning = true;

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var token = _cts.Token;

            try
            {
                // 표시 레벨이 데이터 레벨을 따라잡을 때까지 한 레벨씩 연출합니다.
                while (DisplayedLevel < Level)
                {
                    var stepDone = false;
                    view.PlayLevelUpStep(() => stepDone = true);
                    await UniTask.WaitUntil(() => stepDone, cancellationToken: token);

                    DisplayedLevel++;
                    view.SetLevelText(DisplayedLevel);
                    view.SetExpText(0, expPerLevel);

                    // 보너스 선택이 끝날 때까지 다음 연출을 멈춥니다.
                    _waitingForContinue = true;
                    OnLevelUp?.Invoke(DisplayedLevel, () => _waitingForContinue = false);

                    await UniTask.WaitUntil(() => !_waitingForContinue, cancellationToken: token);
                }

                // 남은 경험치만큼 마저 채웁니다.
                view.SetExpText(Exp, expPerLevel);

                var fillDone = false;
                view.PlayFillTo(view.GetRatio(Exp, expPerLevel), () => fillDone = true);
                await UniTask.WaitUntil(() => fillDone, cancellationToken: token);
            }
            catch (Exception _)
            {
                // 취소되면 조용히 종료합니다.
            }
            finally
            {
                _isSequenceRunning = false;
            }
        }
    }
}
