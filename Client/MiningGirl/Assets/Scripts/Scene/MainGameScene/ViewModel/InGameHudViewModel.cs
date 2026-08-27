using System.Collections.Generic;
using Scene.MainGameScene.Battle;
using Scene.MainGameScene.Progress;
using Scene.MainGameScene.Wave;

namespace Scene.MainGameScene.ViewModel
{
    // 게이지 하나가 필요한 값. 표시 계산은 GaugeBarView가 합니다.
    public readonly struct GaugeValue
    {
        public readonly float Current;
        public readonly float Max;

        public GaugeValue(float current, float max)
        {
            Current = current;
            Max = max;
        }

        public override bool Equals(object obj)
        {
            return obj is GaugeValue other
                && Current.Equals(other.Current)
                && Max.Equals(other.Max);
        }

        public override int GetHashCode()
        {
            return Current.GetHashCode() ^ (Max.GetHashCode() << 2);
        }
    }

    // 슬롯 한 칸에 무엇이 들어 있는지. 쿨다운은 매 프레임 바뀌므로 여기 넣지 않습니다.
    public readonly struct SkillSlotSnapshot
    {
        public readonly bool HasSkill;
        public readonly string IconAssetId;
        public readonly int Level;

        public SkillSlotSnapshot(bool hasSkill, string iconAssetId, int level)
        {
            HasSkill = hasSkill;
            IconAssetId = iconAssetId;
            Level = level;
        }

        public bool SameAs(SkillSlotSnapshot other)
        {
            return HasSkill == other.HasSkill && IconAssetId == other.IconAssetId && Level == other.Level;
        }
    }

    // 인게임 상시 표시의 표시용 상태.
    //
    // Model(WaveRunner·LevelSystem·Tower·SkillInventory)을 읽어 화면에 그대로 넣을 수 있는
    // 형태로 바꿉니다. 포맷 문자열도 여기서 만듭니다. View는 받아 그리기만 합니다.
    public class InGameHudViewModel
    {
        // 몇 번째 스테이지인지. 판이 도는 동안 바뀌지 않아 Tick에서 매번 넣지 않습니다.
        public ObservableProperty<string> StageText { get; } = new ObservableProperty<string>(string.Empty);

        public ObservableProperty<string> WaveText { get; } = new ObservableProperty<string>(string.Empty);
        public ObservableProperty<string> ElapsedText { get; } = new ObservableProperty<string>(string.Empty);
        public ObservableProperty<GaugeValue> Exp { get; } = new ObservableProperty<GaugeValue>();
        public ObservableProperty<GaugeValue> TowerHealth { get; } = new ObservableProperty<GaugeValue>();

        // 슬롯 구성(획득·레벨업)이 바뀔 때만 올라갑니다.
        // 배열 자체를 알림에 실으면 내용이 같아도 매번 발행되므로 번호만 씁니다.
        public ObservableProperty<int> SlotRevision { get; } = new ObservableProperty<int>();

        public IReadOnlyList<SkillSlotSnapshot> Slots => _slots;

        public ObservableProperty<bool> IsPaused { get; } = new ObservableProperty<bool>();

        private readonly WaveRunner _waveRunner;
        private readonly LevelSystem _levelSystem;
        private readonly Tower _tower;
        private readonly SkillInventory _inventory;
        private readonly SkillRunner _skillRunner;

        private readonly SkillSlotSnapshot[] _slots;

        private int _revision;

        public InGameHudViewModel(
            WaveRunner waveRunner,
            LevelSystem levelSystem,
            Tower tower,
            SkillInventory inventory,
            SkillRunner skillRunner,
            int slotViewCount)
        {
            _waveRunner = waveRunner;
            _levelSystem = levelSystem;
            _tower = tower;
            _inventory = inventory;
            _skillRunner = skillRunner;

            _slots = new SkillSlotSnapshot[slotViewCount < 0 ? 0 : slotViewCount];
        }

        // 쿨다운은 매 프레임 바뀝니다. 알림으로 쏘면 낭비라 View가 그릴 때 읽어 갑니다.
        public float GetCooldownRatio(int slotIndex)
        {
            var skills = _inventory.Skills;

            return slotIndex < 0 || slotIndex >= skills.Count
                ? 0f
                : _skillRunner.GetCooldownRatio(skills[slotIndex]);
        }

        // 값을 넣기만 하면 ObservableProperty가 실제로 바뀐 것만 걸러 알립니다.
        // 그래서 매 프레임 불러도 알림은 변화가 있을 때만 나갑니다.
        public void Tick(float elapsedSeconds)
        {
            WaveText.Value = $"WAVE {_waveRunner.CurrentWaveNo}/{_waveRunner.TotalWaveCount}";
            ElapsedText.Value = FormatTime(elapsedSeconds);
            Exp.Value = new GaugeValue(_levelSystem.KillsInLevel, _levelSystem.GaugeRequired);
            TowerHealth.Value = new GaugeValue(_tower.CurrentHealth, _tower.MaxHealth);

            RefreshSlots();
        }

        // 스테이지 Id에서 번호를 뽑아 표시 문자열을 만듭니다.
        //
        // Id는 "Stage_01" 꼴입니다. 규칙이 깨진 Id가 들어오면 Id를 그대로 보여 줍니다 -
        // 화면이 비는 것보다 "Stage_XX"라도 뜨는 편이 무엇이 잘못됐는지 알기 쉽습니다.
        public static string FormatStage(string stageId)
        {
            if (string.IsNullOrEmpty(stageId))
                return string.Empty;

            var separator = stageId.LastIndexOf('_');

            if (separator < 0 || separator + 1 >= stageId.Length)
                return stageId;

            var tail = stageId.Substring(separator + 1);

            return int.TryParse(tail, out var number) ? $"STAGE {number}" : stageId;
        }

        public static string FormatTime(float seconds)
        {
            var total = seconds <= 0f ? 0 : (int)seconds;

            return $"{total / 60:00}:{total % 60:00}";
        }

        private void RefreshSlots()
        {
            var skills = _inventory.Skills;
            var changed = false;

            for (var i = 0; i < _slots.Length; i++)
            {
                var snapshot = i < skills.Count
                    ? new SkillSlotSnapshot(true, skills[i].Row.IconAssetId, skills[i].Level)
                    : new SkillSlotSnapshot(false, null, 0);

                if (_slots[i].SameAs(snapshot))
                    continue;

                _slots[i] = snapshot;
                changed = true;
            }

            if (changed)
                SlotRevision.Value = ++_revision;
        }
    }
}
