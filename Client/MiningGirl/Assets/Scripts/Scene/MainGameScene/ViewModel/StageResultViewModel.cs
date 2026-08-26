using System;

namespace Scene.MainGameScene.ViewModel
{
    // 스테이지 결과의 표시용 상태와 커맨드.
    // 1차에서는 보상 지급이 없어 별 표시와 기록만 담습니다.
    public class StageResultViewModel
    {
        // 컨트롤러가 구독합니다. 씬을 다시 여는 건 컨트롤러 몫입니다.
        public event Action RetryRequested;

        // 별 기준. 시트에서 옵니다.
        //
        // 밸런스를 잡다 보면 자주 건드릴 값이라 코드에 박지 않았습니다.
        // 기본값 1.0은 "한 대도 안 맞아야 별 셋"이라는 뜻이고, 그게 확정된 규칙입니다.
        private readonly float _threeStarHealthRate;
        private readonly float _twoStarHealthRate;

        public StageResultViewModel(float threeStarHealthRate, float twoStarHealthRate)
        {
            _threeStarHealthRate = threeStarHealthRate;
            _twoStarHealthRate = twoStarHealthRate;
        }

        public ObservableProperty<bool> IsVisible { get; } = new ObservableProperty<bool>();

        // 0~3. 실패는 0이고, 이때도 빈 별 셋을 그려 몇 개를 노릴 수 있었는지 보여 줍니다.
        public ObservableProperty<int> StarCount { get; } = new ObservableProperty<int>();

        public ObservableProperty<string> TitleText { get; } = new ObservableProperty<string>(string.Empty);
        public ObservableProperty<string> WaveText { get; } = new ObservableProperty<string>(string.Empty);
        public ObservableProperty<string> ElapsedText { get; } = new ObservableProperty<string>(string.Empty);
        public ObservableProperty<string> TowerText { get; } = new ObservableProperty<string>(string.Empty);

        public void Show(
            bool cleared,
            int reachedWave,
            int totalWave,
            float elapsedSeconds,
            float towerHealth,
            float towerMaxHealth)
        {
            TitleText.Value = cleared ? "STAGE CLEAR" : "STAGE FAILED";
            WaveText.Value = $"도달 웨이브    {reachedWave} / {totalWave}";
            ElapsedText.Value = $"소요 시간    {InGameHudViewModel.FormatTime(elapsedSeconds)}";
            TowerText.Value = $"남은 타워 체력    {Ceil(towerHealth)} / {Ceil(towerMaxHealth)}";

            // 처치 수는 넣지 않습니다. 클리어 조건이 전멸이라 클리어면 항상 총량과 같아 정보가 없습니다.
            StarCount.Value = CalculateStars(cleared, towerHealth, towerMaxHealth);

            // 남은 타워 체력을 같이 보여 줘야 왜 별이 둘인지 납득이 됩니다.

            IsVisible.Value = true;
        }

        public void Hide()
        {
            IsVisible.Value = false;
        }

        // View의 버튼이 부르는 커맨드입니다.
        public void Retry()
        {
            RetryRequested?.Invoke();
        }

        private int CalculateStars(bool cleared, float towerHealth, float towerMaxHealth)
        {
            if (!cleared)
                return 0;

            var ratio = towerMaxHealth <= 0f ? 0f : towerHealth / towerMaxHealth;

            if (ratio >= _threeStarHealthRate)
                return 3;

            return ratio >= _twoStarHealthRate ? 2 : 1;
        }

        private static int Ceil(float value)
        {
            return value <= 0f ? 0 : (int)Math.Ceiling(value);
        }
    }
}
