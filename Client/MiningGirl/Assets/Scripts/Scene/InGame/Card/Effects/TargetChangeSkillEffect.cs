using Data;
using Scene.InGame.Entity.Player;

namespace MainGame.Card.Effects
{
    // 이동 — 지금 캐는 광물을 버리고 '가장 안전한 광물'로 옮겨갑니다.
    //
    // 채굴 타겟은 캐릭터 AI가 정하고 유저가 직접 고를 수 없기 때문에,
    // 몬스터에 둘러싸였을 때 빠져나갈 유일한 수단입니다.
    // 화면 밖 광물도 후보에 넣습니다 — 도망쳐야 할 때는 근처가 이미 위험합니다.
    public class TargetChangeSkillEffect : ISkillCardEffect
    {
        // 광물 주변 이 거리 안의 몬스터를 '위험'으로 셉니다.
        private const float DefaultDangerRadius = 4f;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            // 옮겨갈 광물이 하나라도 있어야 합니다.
            return GetPlayer(context) != null;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var player = GetPlayer(context);
            if (player == null)
                return;

            player.MoveToSafestResource(context.GetMonsters?.Invoke(), GetDangerRadius(row));
        }

        private static Player GetPlayer(SkillCardContext context)
        {
            return context?.Player as Player;
        }

        private static float GetDangerRadius(SkillCardDataTableRow row)
        {
            return row.EffectRange > 0f ? row.EffectRange : DefaultDangerRadius;
        }
    }
}
