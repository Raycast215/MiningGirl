using System.Collections.Generic;
using Data;
using UnityEngine;

namespace MainGame.Bonus
{
    // 레벨업으로 획득한 보너스를 누적해서 보관합니다.
    // 어떤 효과인지는 데이터 테이블의 EffectType으로 판단하므로,
    // 같은 타입의 스킬을 시트에 추가해도 이 코드는 바꿀 필요가 없습니다.
    public class LevelUpBonusState
    {
        // 채굴 데미지 합연산 누적치
        public float MiningDamageAdd { get; private set; }
        // 채굴 속도 배율 (값이 클수록 빠름 = 공격 주기가 짧아짐)
        public float MiningSpeedMultiplier { get; private set; } = 1f;
        // 이동 속도 배율
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        // 치명타 데미지 배율 (기본 CriDamage에 곱해집니다)
        public float CriDamageMultiplier { get; private set; } = 1f;
        // 치명타 확률 배율
        public float CriRateMultiplier { get; private set; } = 1f;
        // 추가타 확률 배율
        public float ExtraHitRateMultiplier { get; private set; } = 1f;
        // 적 처치 시 추가 골드
        public int MonsterKillGoldAdd { get; private set; }
        // 광물 채굴 시 추가 골드
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

        public void Reset()
        {
            MiningDamageAdd = 0f;
            MiningSpeedMultiplier = 1f;
            MoveSpeedMultiplier = 1f;
            CriDamageMultiplier = 1f;
            CriRateMultiplier = 1f;
            ExtraHitRateMultiplier = 1f;
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

            switch (row.EffectType)
            {
                case ELevelUpBonusEffectType.MiningDamage:
                    MiningDamageAdd += row.EffectValue;
                    break;

                case ELevelUpBonusEffectType.MiningSpeed:
                    MiningSpeedMultiplier *= 1f + row.EffectValue;
                    break;

                case ELevelUpBonusEffectType.MoveSpeed:
                    MoveSpeedMultiplier *= 1f + row.EffectValue;
                    break;

                case ELevelUpBonusEffectType.CriDamage:
                    CriDamageMultiplier *= 1f + row.EffectValue;
                    break;

                case ELevelUpBonusEffectType.CriRate:
                    CriRateMultiplier *= 1f + row.EffectValue;
                    break;

                case ELevelUpBonusEffectType.ExtraHitRate:
                    ExtraHitRateMultiplier *= 1f + row.EffectValue;
                    break;

                case ELevelUpBonusEffectType.MonsterKillGold:
                    MonsterKillGoldAdd += Mathf.RoundToInt(row.EffectValue);
                    break;

                case ELevelUpBonusEffectType.ResourceMineGold:
                    ResourceMineGoldAdd += Mathf.RoundToInt(row.EffectValue);
                    break;
            }
        }
    }
}
