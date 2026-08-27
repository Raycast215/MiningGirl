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

        // 화면에 보이는 레벨. 획득 1 + 이 스킬에 붙은 강화 횟수의 합입니다.
        //
        // 순수 표시값이라 스탯 계산에 쓰지 않습니다. 스킬 레벨업 카드가 사라지면서
        // 위력을 올리는 경로가 강화 하나로 모였고, 시트의 레벨별 배열은 첫 값만 씁니다.
        // 종류를 가리지 않고 셉니다 - 관통과 범위도 그 스킬에 넣은 투자입니다.
        public int Level => 1 + _totalUpgradeCount;

        // 강화 종류별 누적. 합연산과 곱연산을 따로 들고 마지막에 (기본값 + 합) x 곱으로 냅니다.
        private readonly Dictionary<ESkillUpgradeType, float> _addValues = new Dictionary<ESkillUpgradeType, float>();
        private readonly Dictionary<ESkillUpgradeType, float> _mulValues = new Dictionary<ESkillUpgradeType, float>();

        // 3택에서 같은 강화가 몇 번 나왔는지. 카드에 "관통 +2" 같은 걸 보여줄 때 씁니다.
        private readonly Dictionary<ESkillUpgradeType, int> _upgradeCounts = new Dictionary<ESkillUpgradeType, int>();

        // 지금까지 이 스킬에 넣은 강화의 총 횟수. 표시 레벨의 재료입니다.
        private int _totalUpgradeCount;

        public SkillState(SkillDataTableRow row)
        {
            Row = row;
        }

        // 레벨이 스탯에 붙지 않으므로 시트 배열의 첫 값만 씁니다.
        // 나머지 네 값은 지우지 않고 미사용으로 둡니다.
        //
        // 강화스킬이 쿨다운을 덮어쓸 수 있습니다. 0이면 안 건드린다는 뜻입니다 -
        // 덮어쓰는 값은 감산이 아니라 대입이라 시트의 그 칸이 곧 결과입니다.
        public float Cooldown =>
            Mastery.HasValue && Mastery.Cooldown > 0f
                ? Mastery.Cooldown
                : Mathf.Max(0.05f, FirstValue(Row.CooldownList));

        public float Damage => Combine(ESkillUpgradeType.Damage, FirstValue(Row.EffectValueList));

        public int ProjectileCount =>
            Mathf.Max(1, Mathf.RoundToInt(Combine(ESkillUpgradeType.ProjectileCount, Row.ProjectileCount)));

        public int PierceCount =>
            Mathf.Max(0, Mathf.RoundToInt(Combine(ESkillUpgradeType.PierceCount, Row.PierceCount)));

        public float HitRange => Mathf.Max(0f, Combine(ESkillUpgradeType.HitRange, Row.HitRange));

        // 지금 레벨과 쌓인 강화가 반영된 발사체 값 묶음.
        // 이 스킬에 걸린 강화스킬. 없으면 HasValue가 false입니다.
        //
        // 런당 하나뿐이라 인벤토리가 아니라 스킬에 답니다 - 발사할 때 이 스킬이
        // 강화스킬을 가졌는지만 보면 되고, 어느 스킬인지 되짚을 필요가 없습니다.
        public Battle.MasterySpec Mastery { get; private set; }

        public void SetMastery(SkillMasteryDataTableRow row)
        {
            Mastery = new Battle.MasterySpec(row);
        }

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
                wave != null && wave.Count > 1 ? wave[1] : 0f,
                Mastery);
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

            _totalUpgradeCount++;
        }

        // 저장이 종류별 횟수를 훑을 때 씁니다.
        public IEnumerable<KeyValuePair<ESkillUpgradeType, int>> UpgradeCounts => _upgradeCounts;

        public int GetUpgradeCount(ESkillUpgradeType type)
        {
            _upgradeCounts.TryGetValue(type, out var count);

            return count;
        }

        // 시트의 레벨별 배열에서 첫 값만 꺼냅니다.
        //
        // 스킬 레벨업 카드가 없어져 인덱싱할 레벨이 없습니다. 나머지 네 값은
        // 시트에 그대로 두고 쓰지 않습니다 - 되살릴 때 비용이 달라집니다.
        public static float FirstValue(IReadOnlyList<float> list)
        {
            return list == null || list.Count == 0 ? 0f : list[0];
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
