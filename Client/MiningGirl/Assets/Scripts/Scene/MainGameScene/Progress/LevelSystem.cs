using System;
using UnityEngine;

namespace Scene.MainGameScene.Progress
{
    // 처치 수로 굴러가는 레벨. 웨이브와는 별개로 돕니다.
    //
    // 필요량을 스테이지 전체 몬스터 수에서 뽑기 때문에, 스테이지가 400마리 20웨이브가 되면
    // 첫 구간이 저절로 20이 됩니다. 스테이지마다 곡선을 다시 적지 않아도 됩니다.
    public class LevelSystem
    {
        public event Action<int> OnLevelUp;

        public int Level { get; private set; } = 1;

        // 이번 구간에서 잡은 수와 필요한 수. 경험치 게이지가 이 둘을 씁니다.
        public int KillsInLevel { get; private set; }
        public int RequiredKills { get; private set; }

        public int TotalKills { get; private set; }

        // 경험치 게이지가 쓰는 분모. 레벨 계산에는 쓰지 않습니다.
        //
        // 마지막 구간은 판이 끝날 때까지 채울 수 없을 때가 있습니다. 200마리 20웨이브면
        // 마지막 레벨업이 192마리째에 오고, 남은 8마리로는 다음 구간의 14를 못 채웁니다.
        // RequiredKills를 그대로 그리면 몬스터를 다 잡아도 게이지가 8/14에서 멈춰
        // 판을 덜 끝낸 것처럼 보입니다.
        //
        // 그래서 남은 몬스터가 필요량보다 적으면 남은 수를 분모로 씁니다. 마지막
        // 구간만 "다음 레벨까지"가 아니라 "판이 끝날 때까지"를 그리게 되고, 전부
        // 처치하면 게이지가 정확히 가득 찹니다. 레벨은 그대로 17에서 끝납니다.
        public int GaugeRequired
        {
            get
            {
                var levelStartKills = TotalKills - KillsInLevel;
                var remaining = _totalMonsterCount - levelStartKills;

                return remaining > 0 && remaining < RequiredKills ? remaining : RequiredKills;
            }
        }

        public float Progress => GaugeRequired <= 0 ? 0f : Mathf.Clamp01((float)KillsInLevel / GaugeRequired);

        // 이 판에 나오는 몬스터 총수. 마지막 구간의 게이지 분모를 정하는 데 씁니다.
        private readonly int _totalMonsterCount;

        // 첫 구간 필요량. TotalMonsterCount / WaveCount 입니다.
        private readonly float _firstStep;

        // 마지막 구간이 첫 구간의 몇 배인지. 1이면 균등합니다.
        private readonly float _curveRate;

        private readonly int _waveCount;

        public LevelSystem(int totalMonsterCount, int waveCount, float curveRate)
        {
            _totalMonsterCount = Mathf.Max(0, totalMonsterCount);
            _waveCount = Mathf.Max(1, waveCount);
            _firstStep = Mathf.Max(1f, (float)totalMonsterCount / _waveCount);
            _curveRate = Mathf.Max(1f, curveRate);

            RequiredKills = CalculateRequired(Level);
        }

        // 몬스터 1마리 처치 = 경험치 1입니다.
        public void AddKill()
        {
            TotalKills++;
            KillsInLevel++;

            // 한 프레임에 여러 마리가 죽어 두 레벨이 한꺼번에 오를 수도 있습니다.
            while (KillsInLevel >= RequiredKills)
            {
                KillsInLevel -= RequiredKills;
                Level++;
                RequiredKills = CalculateRequired(Level);

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
