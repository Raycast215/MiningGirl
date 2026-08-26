using System.Collections.Generic;
using Legacy.MainGame.Card.Effects;
using UnityEngine;

namespace Legacy.MainGame.Card
{
    // ESkillType → 실제 효과 구현을 연결합니다.
    //
    // 새 스킬을 추가할 때 손댈 곳은 여기 한 곳입니다.
    // (enum에 타입 추가 → 효과 클래스 작성 → 이 표에 등록)
    public static class SkillCardEffectFactory
    {
        private static readonly Dictionary<ESkillType, ISkillCardEffect> Effects =
            new Dictionary<ESkillType, ISkillCardEffect>
            {
                { ESkillType.AirShot,       new AirShotSkillEffect() },
                { ESkillType.Strike,        new StrikeSkillEffect() },
                { ESkillType.DoubleAttack,  new DoubleAttackSkillEffect() },
                { ESkillType.IceBolt,       new IceBoltSkillEffect() },

                { ESkillType.MoveSpeedUp,   new MoveSpeedUpSkillEffect() },
                { ESkillType.MiningSpeedUp, new MiningSpeedUpSkillEffect() },
                { ESkillType.GoldGainUp,    new GoldGainUpSkillEffect() },

                { ESkillType.Heal,          new HealSkillEffect() },
                { ESkillType.TargetChange,  new TargetChangeSkillEffect() },
                { ESkillType.CostUp,        new CostUpSkillEffect() },
                { ESkillType.FireBall,      new FireBallSkillEffect() },
                { ESkillType.SpecialResource, new SpecialResourceSkillEffect() },
            };

        public static ISkillCardEffect Get(ESkillType type)
        {
            if (Effects.TryGetValue(type, out var effect))
                return effect;

            Debug.LogWarning($"[SkillCard] {type} 에 해당하는 효과 구현이 없습니다.");

            return null;
        }
    }
}
