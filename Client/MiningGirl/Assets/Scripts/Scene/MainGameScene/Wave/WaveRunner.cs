using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Scene.MainGameScene.Battle;
using UnityEngine;

namespace Scene.MainGameScene.Wave
{
    // 웨이브 진행. 전환 기준은 시간입니다.
    //
    // Duration이 지나면 몬스터가 남아 있어도 다음 웨이브로 넘어갑니다.
    // 못 잡은 몬스터는 그대로 누적되므로, 밀리면 밀릴수록 화면이 불어납니다.
    public class WaveRunner
    {
        private enum EPhase
        {
            StartDelay, // 스테이지 시작 후 1웨이브까지
            Running,    // 웨이브 진행 중
            Gap,        // 웨이브 사이 간격
            Finished,   // 마지막 웨이브까지 끝남
        }

        private readonly struct SpawnEntry
        {
            public readonly float Time;
            public readonly MonsterDataTableRow Monster;

            public SpawnEntry(float time, MonsterDataTableRow monster)
            {
                Time = time;
                Monster = monster;
            }
        }

        public event Action<int> OnWaveStarted;
        public event Action OnAllWavesFinished;

        public int TotalWaveCount => _waves.Count;

        // 화면에 WAVE {현재}/{총}으로 나갑니다. 아직 시작 전이면 1로 보여 줍니다.
        public int CurrentWaveNo => _waveIndex < 0 ? 1 : Mathf.Min(_waveIndex + 1, TotalWaveCount);

        public bool IsFinished => _phase == EPhase.Finished;

        private readonly List<WaveDataTableRow> _waves;
        private readonly MonsterDataTable _monsterTable;
        private readonly MonsterField _field;

        private readonly List<SpawnEntry> _schedule = new List<SpawnEntry>();

        private readonly float _waveStartDelay;
        private readonly float _waveGap;

        private EPhase _phase = EPhase.StartDelay;
        private int _waveIndex = -1;
        private int _scheduleIndex;
        private float _timer;
        private float _waveDuration;

        public WaveRunner(
            WaveDataTable waveTable,
            MonsterDataTable monsterTable,
            MonsterField field,
            string stageId,
            float waveStartDelay,
            float waveGap)
        {
            _monsterTable = monsterTable;
            _field = field;
            _waveStartDelay = Mathf.Max(0f, waveStartDelay);
            _waveGap = Mathf.Max(0f, waveGap);

            _waves = waveTable?.Rows?
                .Where(row => row != null && row.StageId == stageId)
                .OrderBy(row => row.WaveNo)
                .ToList() ?? new List<WaveDataTableRow>();

            _timer = _waveStartDelay;
        }

        // 저장이 읽는 웨이브 진행 상태.
        //
        // _schedule 자체는 저장하지 않습니다. BuildSchedule이 시트에서 결정론적으로
        // 다시 만들기 때문입니다. 대신 길이를 함께 내보내, 복원 때 다시 만든 것과
        // 대조합니다 - 그 웨이브 행이 바뀌었으면 길이가 달라지고, 그러면 이어붙일
        // 수 없습니다.
        public string CapturePhase()
        {
            return _phase.ToString();
        }

        public int CaptureWaveIndex()
        {
            return _waveIndex;
        }

        public float CaptureTimer()
        {
            return _timer;
        }

        public int CaptureScheduleIndex()
        {
            return _scheduleIndex;
        }

        public int CaptureScheduleCount()
        {
            return _schedule.Count;
        }

        // 저장에서 되돌립니다. 스케줄 길이가 저장과 다르면 false입니다.
        //
        // OnWaveStarted를 다시 쏘지 않습니다. 이어붙이는 것이지 새 웨이브가
        // 시작되는 게 아니고, 쏘면 화면이 "WAVE N 시작"을 한 번 더 알립니다.
        // 화면 갱신은 컨트롤러가 CurrentWaveNo를 읽어 따로 합니다.
        public bool RestoreState(string phase, int waveIndex, float timer, int scheduleIndex, int savedScheduleCount)
        {
            EPhase parsed;

            if (!System.Enum.TryParse(phase, out parsed))
            {
                Debug.LogWarning($"[Wave] 저장의 진행 단계를 읽지 못했습니다: {phase}");

                return false;
            }

            // 실패하면 아무것도 건드리지 않고 돌아갑니다. 반쯤 되돌린 상태로 두면
            // 호출한 쪽이 새 판으로 시작해도 웨이브만 저장 시점에 가 있게 됩니다.
            if (parsed != EPhase.Running)
            {
                if (savedScheduleCount != 0)
                    return false;

                _phase = parsed;
                _waveIndex = waveIndex;
                _timer = timer;
                _schedule.Clear();
                _scheduleIndex = 0;

                return true;
            }

            if (waveIndex < 0 || waveIndex >= _waves.Count)
            {
                Debug.LogWarning($"[Wave] 저장의 웨이브 번호가 범위를 벗어납니다: {waveIndex}");

                return false;
            }

            var wave = _waves[waveIndex];
            var duration = Mathf.Max(0.1f, wave.Duration);

            // BuildSchedule이 _schedule과 _waveDuration을 쓰므로 임시로 넣고 재 봅니다.
            var keptSchedule = new List<SpawnEntry>(_schedule);
            var keptDuration = _waveDuration;

            _waveDuration = duration;

            BuildSchedule(wave);

            if (_schedule.Count != savedScheduleCount)
            {
                _schedule.Clear();
                _schedule.AddRange(keptSchedule);
                _waveDuration = keptDuration;

                return false;
            }

            _phase = parsed;
            _waveIndex = waveIndex;
            _timer = timer;
            _scheduleIndex = Mathf.Clamp(scheduleIndex, 0, _schedule.Count);

            return true;
        }

        // 이 스테이지에 나오는 몬스터 종류. 프리팹을 미리 불러 두는 데 씁니다.
        public IEnumerable<string> CollectMonsterIds()
        {
            return _waves
                .Where(row => row.MonsterIds != null)
                .SelectMany(row => row.MonsterIds)
                .Distinct();
        }

        public void Tick(float deltaTime)
        {
            switch (_phase)
            {
                case EPhase.StartDelay:
                case EPhase.Gap:
                    _timer -= deltaTime;

                    if (_timer <= 0f)
                        BeginNextWave();

                    return;

                case EPhase.Running:
                    _timer += deltaTime;

                    SpawnDue();

                    if (_timer < _waveDuration)
                        return;

                    EndWave();

                    return;
            }
        }

        private void BeginNextWave()
        {
            _waveIndex++;

            if (_waveIndex >= _waves.Count)
            {
                Finish();

                return;
            }

            var wave = _waves[_waveIndex];

            _waveDuration = Mathf.Max(0.1f, wave.Duration);
            _timer = 0f;
            _scheduleIndex = 0;
            _phase = EPhase.Running;

            BuildSchedule(wave);

            OnWaveStarted?.Invoke(CurrentWaveNo);
        }

        // 종류별로 Duration 안에 균등하게 흩뿌립니다.
        //
        // n번째 = SpawnDelay + (Duration - SpawnDelay) x (n-1) / Count
        //
        // Duration / Count를 그대로 쓰면 마지막 마리가 웨이브 종료 시각에 걸립니다.
        // 웨이브 20처럼 보스 한 마리짜리 구성에서는 보스가 끝나는 순간 나오게 됩니다.
        private void BuildSchedule(WaveDataTableRow wave)
        {
            _schedule.Clear();

            if (wave.MonsterIds == null || wave.Counts == null)
                return;

            var kinds = Mathf.Min(wave.MonsterIds.Count, wave.Counts.Count);

            for (var k = 0; k < kinds; k++)
            {
                var monster = _monsterTable?.GetRow(wave.MonsterIds[k]);

                if (monster == null)
                {
                    Debug.LogError($"[Wave] {wave.Id}가 없는 몬스터를 가리킵니다: {wave.MonsterIds[k]}");

                    continue;
                }

                var count = wave.Counts[k];

                if (count <= 0)
                    continue;

                var start = Mathf.Clamp(monster.SpawnDelay, 0f, _waveDuration);
                var span = Mathf.Max(0f, _waveDuration - start);

                for (var n = 0; n < count; n++)
                    _schedule.Add(new SpawnEntry(start + span * n / count, monster));
            }

            _schedule.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        private void SpawnDue()
        {
            while (_scheduleIndex < _schedule.Count && _schedule[_scheduleIndex].Time <= _timer)
            {
                _field.Spawn(_schedule[_scheduleIndex].Monster);
                _scheduleIndex++;
            }
        }

        private void EndWave()
        {
            // 시간이 끝나도 아직 안 나온 몬스터가 있으면 전부 내보냅니다.
            // 총 몬스터 수가 고정이라는 전제가 여기서 깨지면 경험치 총량도 같이 깨집니다.
            while (_scheduleIndex < _schedule.Count)
            {
                _field.Spawn(_schedule[_scheduleIndex].Monster);
                _scheduleIndex++;
            }

            if (_waveIndex + 1 >= _waves.Count)
            {
                Finish();

                return;
            }

            _phase = EPhase.Gap;
            _timer = _waveGap;
        }

        private void Finish()
        {
            _phase = EPhase.Finished;

            OnAllWavesFinished?.Invoke();
        }
    }
}
