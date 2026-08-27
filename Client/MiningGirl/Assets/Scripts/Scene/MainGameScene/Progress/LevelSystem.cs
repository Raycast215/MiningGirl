using System;
using UnityEngine;

namespace Scene.MainGameScene.Progress
{
    // 경험치로 굴러가는 레벨. 웨이브와는 별개로 돕니다.
    //
    // 예전에는 처치 수로 돌았고 첫 구간 필요량을 `총 몬스터 수 / 웨이브 수`로 냈습니다.
    // 그러면 마리 수를 바꿀 때마다 레벨 곡선이 따라 움직여서, 밀도와 성장을 따로
    // 조절할 수가 없었습니다. 첫 구간을 시트 상수로 빼서 그 결합을 끊습니다.
    //
    // 대신 새 결합이 하나 생깁니다 - 스테이지가 주는 총 경험치가 곡선의 총합과
    // 맞아야 의도한 레벨에서 끝납니다. 그건 구성으로 맞출 값이지 코드가 정할 값이 아닙니다.
    public class LevelSystem
    {
        public event Action<int> OnLevelUp;

        public int Level { get; private set; } = 1;

        // 이번 구간에서 번 경험치와 필요한 경험치. 경험치 게이지가 이 둘을 씁니다.
        public int ExpInLevel { get; private set; }
        public int RequiredExp { get; private set; }

        public int TotalExp { get; private set; }

        // 처치 수는 이제 레벨과 무관합니다. 기록과 결과 화면에만 씁니다.
        public int TotalKills { get; private set; }

        // 경험치 게이지가 쓰는 분모. 레벨 계산에는 쓰지 않습니다.
        //
        // 마지막 구간은 판이 끝날 때까지 채울 수 없을 때가 있습니다. 스테이지에 남은
        // 경험치가 다음 레벨까지 필요한 양보다 적으면, RequiredExp를 그대로 그릴 때
        // 몬스터를 다 잡아도 게이지가 중간에서 멈춰 판을 덜 끝낸 것처럼 보입니다.
        //
        // 그래서 남은 경험치가 필요량보다 적으면 남은 양을 분모로 씁니다. 마지막
        // 구간만 "다음 레벨까지"가 아니라 "판이 끝날 때까지"를 그리게 되고, 전부
        // 처치하면 게이지가 정확히 가득 찹니다. 레벨은 영향받지 않습니다.
        public int GaugeRequired
        {
            get
            {
                var levelStartExp = TotalExp - ExpInLevel;
                var remaining = _totalStageExp - levelStartExp;

                return remaining > 0 && remaining < RequiredExp ? remaining : RequiredExp;
            }
        }

        public float Progress => GaugeRequired <= 0 ? 0f : Mathf.Clamp01((float)ExpInLevel / GaugeRequired);

        // 이 판에 걸려 있는 경험치 총합. 마지막 구간의 게이지 분모를 정하는 데만 씁니다.
        //
        // 웨이브 테이블에서 계산해 넣습니다. 시트에 따로 적으면 구성을 고칠 때마다
        // 두 곳을 맞춰야 하고, 어긋나도 아무도 안 알려줍니다.
        private readonly int _totalStageExp;

        // 레벨 1에서 2로 오를 때 필요한 경험치. 시트 상수입니다.
        private readonly float _firstStep;

        // 마지막 구간이 첫 구간의 몇 배인지. 1이면 균등합니다.
        private readonly float _curveRate;

        private readonly int _waveCount;

        public LevelSystem(int firstStepExp, int totalStageExp, int waveCount, float curveRate)
        {
            _totalStageExp = Mathf.Max(0, totalStageExp);
            _waveCount = Mathf.Max(1, waveCount);
            _firstStep = Mathf.Max(1f, firstStepExp);
            _curveRate = Mathf.Max(1f, curveRate);

            RequiredExp = CalculateRequired(Level);
        }

        // 저장에서 되돌립니다.
        //
        // 필요량은 저장하지 않고 여기서 다시 냅니다 - CalculateRequired가
        // 시트 값만으로 결정되므로, 곡선이 바뀌면 최신 곡선을 따르는 게 맞습니다.
        public void Restore(int level, int expInLevel, int totalExp, int totalKills)
        {
            Level = Mathf.Max(1, level);
            TotalExp = Mathf.Max(0, totalExp);
            TotalKills = Mathf.Max(0, totalKills);
            RequiredExp = CalculateRequired(Level);
            ExpInLevel = Mathf.Clamp(expInLevel, 0, Mathf.Max(0, RequiredExp - 1));
        }

        // 몬스터 하나를 잡았습니다. 주는 경험치는 몬스터마다 다릅니다.
        public void AddKill(int exp)
        {
            TotalKills++;

            AddExp(exp);
        }

        public void AddExp(int exp)
        {
            if (exp <= 0)
                return;

            TotalExp += exp;
            ExpInLevel += exp;

            // 한 번에 여러 레벨이 오를 수 있습니다. 큰 경험치를 주는 몬스터 하나로도
            // 두 구간을 넘길 수 있어서, 처치 수로 돌 때보다 더 자주 일어납니다.
            while (ExpInLevel >= RequiredExp)
            {
                ExpInLevel -= RequiredExp;
                Level++;
                RequiredExp = CalculateRequired(Level);

                OnLevelUp?.Invoke(Level);
            }
        }

        // 필요량(L) = 첫구간 x (1 + (L-1) x (강도-1) / (웨이브수-1))
        private int CalculateRequired(int level)
        {
            if (_waveCount <= 1)
                return Mathf.Max(1, Mathf.RoundToInt(_firstStep));

            var weight = 1f + (level - 1) * (_curveRate - 1f) / (_waveCount - 1);

            return Mathf.Max(1, Mathf.RoundToInt(_firstStep * weight));
        }
    }
}
