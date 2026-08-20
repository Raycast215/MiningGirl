using Data;
using UnityEngine;

namespace MainGame.Bonus
{
    // 선택한 캐릭터의 기본 스탯을 보관하고, 레벨업 보너스를 얹은 최종 값을 계산합니다.
    // 기본값은 CharacterStatDataTable에서 오고, 보너스는 LevelUpBonusState에서 옵니다.
    public class CharacterStatContext
    {
        public CharacterStatDataRow BaseStat { get; private set; }
        public LevelUpBonusState Bonus { get; }

        // 카드로 걸리는 일시 버프. 영구 성장(Bonus)과 계층이 다릅니다.
        public TemporaryBuffState Buffs { get; } = new TemporaryBuffState();

        public CharacterStatContext(LevelUpBonusState bonus)
        {
            Bonus = bonus;
        }

        public bool HasStat => BaseStat != null;
        public string SelectedCharacterId => BaseStat?.Id;

        // 캐릭터 선택 시 호출합니다. 재시작 시에는 다시 호출하지 않아
        // 선택한 캐릭터와 강화 상태가 그대로 유지됩니다.
        public void SetCharacter(CharacterStatDataRow row)
        {
            BaseStat = row;
        }

        // 카드 버프로 인한 골드 획득 배율
        public float GetGoldGainMultiplier()
        {
            return Buffs.GetMultiplier(TemporaryBuffState.EBuffType.GoldGain);
        }

        // 카드 버프로 인한 경험치 획득 배율

        // 피격 후 무적 시간(초)
        public float GetInvincibleDuration()
        {
            return BaseStat?.InvincibleDuration ?? 2f;
        }

        // 채굴 데미지 = 기본 + 레벨업 합연산
        public float GetDamage()
        {
            var baseValue = BaseStat?.Damage ?? 1f;
            return (baseValue + (Bonus?.MiningDamageAdd ?? 0f)) * (Bonus?.MiningDamageMultiplier ?? 1f);
        }

        // 채굴 주기(초). 속도 보너스가 오르면 짧아집니다.
        public float GetAttackDelay()
        {
            var baseValue = BaseStat?.AttackDelay ?? 2f;

            var speed = Mathf.Max(0.01f,
                (Bonus?.MiningSpeedMultiplier ?? 1f)
                * Buffs.GetMultiplier(TemporaryBuffState.EBuffType.MiningSpeed));

            return Mathf.Max(0.2f, baseValue / speed);
        }

        public float GetMoveSpeed()
        {
            var baseValue = BaseStat?.MoveSpeed ?? 1f;

            return baseValue
                   * (Bonus?.MoveSpeedMultiplier ?? 1f)
                   * Buffs.GetMultiplier(TemporaryBuffState.EBuffType.MoveSpeed);
        }

        public float GetAttackDistance()
        {
            return BaseStat?.AttackDistance ?? 1f;
        }

        // 치명타 확률(%). 데이터는 15 = 15% 형태입니다. 100%를 넘지 않게 제한합니다.
        public float GetCriRate()
        {
            var baseValue = BaseStat?.CriRate ?? 0f;
            return Mathf.Clamp((baseValue + (Bonus?.CriRateAdd ?? 0f)) * (Bonus?.CriRateMultiplier ?? 1f), 0f, 100f);
        }

        // 치명타 추가 배율. 0.3이면 데미지의 1.3배가 됩니다.
        public float GetCriDamage()
        {
            var baseValue = BaseStat?.CriDamage ?? 0f;
            return (baseValue + (Bonus?.CriDamageAdd ?? 0f)) * (Bonus?.CriDamageMultiplier ?? 1f);
        }

        // 추가타 확률(%). 한 번의 채굴이 두 번 들어갈 확률입니다. 100%를 넘지 않게 제한합니다.
        public float GetExtraHitRate()
        {
            var baseValue = BaseStat?.ExtraHitRate ?? 0f;
            return Mathf.Clamp((baseValue + (Bonus?.ExtraHitRateAdd ?? 0f)) * (Bonus?.ExtraHitRateMultiplier ?? 1f), 0f, 100f);
        }
    }
}
