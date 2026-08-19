using System.Collections.Generic;
using Data;
using UnityEngine;

namespace MainGame.Bonus
{
    // 레벨업으로 획득한 보너스를 누적해서 보관합니다.
    //
    // 어떤 스탯인지는 EffectType, 어떻게 적용할지는 ValueType(Add/Mul)으로 판단하므로
    // 시트에서 계산 방식을 바꿔도 코드를 고칠 필요가 없습니다.
    // 최종 스탯 = (기본값 + 합연산 누적) x 곱연산 누적
    public class LevelUpBonusState
    {
        // 채굴 데미지
        public float MiningDamageAdd { get; private set; }
        public float MiningDamageMultiplier { get; private set; } = 1f;

        // 채굴 속도 (값이 클수록 공격 주기가 짧아집니다)
        public float MiningSpeedMultiplier { get; private set; } = 1f;

        // 이동 속도
        public float MoveSpeedMultiplier { get; private set; } = 1f;

        // 치명타 데미지 (기본값이 배율이라 Add도 배율 단위로 더합니다. 0.1 = +10%p)
        public float CriDamageAdd { get; private set; }
        public float CriDamageMultiplier { get; private set; } = 1f;

        // 치명타 확률 (기본값이 퍼센트 단위라 Add는 100을 곱해 %p로 환산합니다. 0.05 = +5%p)
        public float CriRateAdd { get; private set; }
        public float CriRateMultiplier { get; private set; } = 1f;

        // 추가타 확률 (치명타 확률과 같은 단위)
        public float ExtraHitRateAdd { get; private set; }
        public float ExtraHitRateMultiplier { get; private set; } = 1f;

        // 최대 체력
        // 스태미나 계열 — 소모 감소는 값이 클수록 덜 깎이므로 '감소량'으로 누적합니다.
        public float MaxStaminaAdd { get; private set; }
        public float MaxStaminaMultiplier { get; private set; } = 1f;
        public float MiningStaminaCostReduce { get; private set; }
        public float HitStaminaCostReduce { get; private set; }

        public float MaxHealthAdd { get; private set; }
        public float MaxHealthMultiplier { get; private set; } = 1f;

        // 획득 골드
        public int MonsterKillGoldAdd { get; private set; }
        public int ResourceMineGoldAdd { get; private set; }

        // 스킬별 획득 레벨 (Id -> 획득 횟수)
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public int GetLevel(string id)
        {
            return string.IsNullOrEmpty(id) ? 0 : _levels.TryGetValue(id, out var level) ? level : 0;
        }

        // maxLevel이 -1이면 제한 없음
        public bool CanAcquire(string id, int maxLevel)
        {
            if (maxLevel < 0)
                return true;

            return GetLevel(id) < maxLevel;
        }

        // 저장용 — 현재 강화 레벨 전체를 그대로 넘겨줍니다.
        public IReadOnlyDictionary<string, int> GetAllLevels() => _levels;

        // 불러오기 — 저장된 레벨을 그대로 복원한 뒤 누적 효과를 다시 계산합니다.
        // (효과 합계는 Acquire를 거치며 쌓이므로, 레벨만 넣으면 스탯이 반영되지 않습니다.)
        public void RestoreLevels(IReadOnlyDictionary<string, int> levels, LevelUpBonusSkillDataTable table)
        {
            Reset();

            if (levels == null || table?.Rows == null)
                return;

            foreach (var row in table.Rows)
            {
                if (row == null || !levels.TryGetValue(row.Id, out var level))
                    continue;

                // 레벨 수만큼 반복 적용해 Add/Mul 누적을 원래대로 만듭니다.
                for (var i = 0; i < level; i++)
                    Acquire(row);
            }
        }

        public void Reset()
        {
            MiningDamageAdd = 0f;
            MiningDamageMultiplier = 1f;
            MiningSpeedMultiplier = 1f;
            MoveSpeedMultiplier = 1f;
            CriDamageAdd = 0f;
            CriDamageMultiplier = 1f;
            CriRateAdd = 0f;
            CriRateMultiplier = 1f;
            ExtraHitRateAdd = 0f;
            ExtraHitRateMultiplier = 1f;
            MaxStaminaAdd = 0f;
            MaxStaminaMultiplier = 1f;
            MiningStaminaCostReduce = 0f;
            HitStaminaCostReduce = 0f;

            MaxHealthAdd = 0f;
            MaxHealthMultiplier = 1f;
            MonsterKillGoldAdd = 0;
            ResourceMineGoldAdd = 0;

            _levels.Clear();
        }

        // 스킬 획득을 기록하고 누적 효과를 반영합니다.
        // 즉시 효과(골드/경험치 지급)는 누적할 값이 없으므로 여기서는 레벨만 기록합니다.
        public void Acquire(LevelUpBonusSkillDataTableRow row)
        {
            if (row == null)
                return;

            _levels[row.Id] = GetLevel(row.Id) + 1;

            var isAdd = row.ValueType == EEffectValueType.Add;
            var value = row.EffectValue;

            switch (row.EffectType)
            {
                case ELevelUpBonusEffectType.MiningDamage:
                    if (isAdd) MiningDamageAdd += value;
                    else MiningDamageMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.MiningSpeed:
                    // 속도는 배율로만 의미가 있어 Add도 배율에 더합니다.
                    MiningSpeedMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.MoveSpeed:
                    MoveSpeedMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.CriDamage:
                    if (isAdd) CriDamageAdd += value;
                    else CriDamageMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.CriRate:
                    // 기본 CriRate는 40 = 40% 단위라 Add 값(0.05)을 %p로 환산합니다.
                    if (isAdd) CriRateAdd += value * 100f;
                    else CriRateMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.ExtraHitRate:
                    if (isAdd) ExtraHitRateAdd += value * 100f;
                    else ExtraHitRateMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.MaxStamina:
                    if (isAdd) MaxStaminaAdd += value;
                    else MaxStaminaMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.MiningStaminaCost:
                    MiningStaminaCostReduce += value;
                    break;

                case ELevelUpBonusEffectType.HitStaminaCost:
                    HitStaminaCostReduce += value;
                    break;

                case ELevelUpBonusEffectType.MaxHealth:
                    if (isAdd) MaxHealthAdd += value;
                    else MaxHealthMultiplier *= 1f + value;
                    break;

                case ELevelUpBonusEffectType.MonsterKillGold:
                    MonsterKillGoldAdd += Mathf.RoundToInt(value);
                    break;

                case ELevelUpBonusEffectType.ResourceMineGold:
                    ResourceMineGoldAdd += Mathf.RoundToInt(value);
                    break;
            }
        }
    }
}
