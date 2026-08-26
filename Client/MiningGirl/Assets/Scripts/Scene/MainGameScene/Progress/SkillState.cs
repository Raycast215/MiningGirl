using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Scene.MainGameScene.Progress
{
    // 런 안에서 굴러가는 스킬 하나의 상태.
    // 시트 값(레벨별 배열)과 3택으로 쌓은 강화를 합쳐 실제 수치를 냅니다.
    public class SkillState
    {
        public SkillDataTableRow Row { get; }

        public int Level { get; private set; }

        // 강화 종류별 누적. 합연산과 곱연산을 따로 들고 마지막에 (기본값 + 합) x 곱으로 냅니다.
        private readonly Dictionary<ESkillUpgradeType, float> _addValues = new Dictionary<ESkillUpgradeType, float>();
        private readonly Dictionary<ESkillUpgradeType, float> _mulValues = new Dictionary<ESkillUpgradeType, float>();

        // 3택에서 같은 강화가 몇 번 나왔는지. 카드에 "관통 +2" 같은 걸 보여줄 때 씁니다.
        private readonly Dictionary<ESkillUpgradeType, int> _upgradeCounts = new Dictionary<ESkillUpgradeType, int>();

        public SkillState(SkillDataTableRow row)
        {
            Row = row;
            Level = 1;
        }

        public float Cooldown => Mathf.Max(0.05f, ValueAtLevel(Row.CooldownList, Level));

        public float Damage => Combine(ESkillUpgradeType.Damage, ValueAtLevel(Row.EffectValueList, Level));

        public int ProjectileCount =>
            Mathf.Max(1, Mathf.RoundToInt(Combine(ESkillUpgradeType.ProjectileCount, Row.ProjectileCount)));

        public int PierceCount =>
            Mathf.Max(0, Mathf.RoundToInt(Combine(ESkillUpgradeType.PierceCount, Row.PierceCount)));

        public float HitRange => Mathf.Max(0f, Combine(ESkillUpgradeType.HitRange, Row.HitRange));

        // 지금 레벨과 쌓인 강화가 반영된 발사체 값 묶음.
        public Battle.ProjectileSpec BuildProjectileSpec()
        {
            var wave = Row.ProjectileWaveList;

            return new Battle.ProjectileSpec(
                Row.EffectAssetId,
                Row.ProjectileSpeed,
                Damage,
                PierceCount,
                HitRange,
                Row.ProjectileMoveType,
                wave != null && wave.Count > 0 ? wave[0] : 0f,
                wave != null && wave.Count > 1 ? wave[1] : 0f);
        }

        public void LevelUp()
        {
            Level++;
        }

        public void ApplyUpgrade(SkillUpgradeDataTableRow upgrade)
        {
            if (upgrade == null)
                return;

            if (upgrade.ValueType == EEffectValueType.Add)
            {
                _addValues.TryGetValue(upgrade.UpgradeType, out var sum);
                _addValues[upgrade.UpgradeType] = sum + upgrade.EffectValue;
            }
            else
            {
                _mulValues.TryGetValue(upgrade.UpgradeType, out var product);

                if (product <= 0f)
                    product = 1f;

                _mulValues[upgrade.UpgradeType] = product * (1f + upgrade.EffectValue);
            }

            _upgradeCounts.TryGetValue(upgrade.UpgradeType, out var count);
            _upgradeCounts[upgrade.UpgradeType] = count + 1;
        }

        public int GetUpgradeCount(ESkillUpgradeType type)
        {
            _upgradeCounts.TryGetValue(type, out var count);

            return count;
        }

        // 다음 레벨의 위력. 3택 카드에 "위력 17 → 23"을 보여주는 데 씁니다.
        public float GetDamageAtLevel(int level)
        {
            return Combine(ESkillUpgradeType.Damage, ValueAtLevel(Row.EffectValueList, level));
        }

        // 배열 길이를 넘는 레벨은 마지막 구간의 변화율을 곱연산으로 이어 갑니다.
        //
        // 배열 길이가 최대 레벨이 아니기 때문입니다. 곱연산이라 쿨다운은 0에 수렴하되
        // 도달하지 않아 하한값을 따로 둘 필요가 없습니다.
        public static float ValueAtLevel(IReadOnlyList<float> list, int level)
        {
            if (list == null || list.Count == 0)
                return 0f;

            if (level <= 1)
                return list[0];

            if (level <= list.Count)
                return list[level - 1];

            var last = list[list.Count - 1];

            if (list.Count < 2)
                return last;

            var previous = list[list.Count - 2];

            if (previous <= 0f)
                return last;

            return last * Mathf.Pow(last / previous, level - list.Count);
        }

        private float Combine(ESkillUpgradeType type, float baseValue)
        {
            _addValues.TryGetValue(type, out var add);
            _mulValues.TryGetValue(type, out var mul);

            if (mul <= 0f)
                mul = 1f;

            return (baseValue + add) * mul;
        }
    }
}
