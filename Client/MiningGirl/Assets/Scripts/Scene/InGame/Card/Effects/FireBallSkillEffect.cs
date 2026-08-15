using Data;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 파이어볼 — 캐릭터 곁을 지키다가 최대 범위 안의 적에게 날아가 부딪히고 돌아옵니다.
    //
    // EffectRange는 '적을 찾을 최대 거리'입니다.
    // 놓은 자리에 고정하면 캐릭터가 채굴하러 떠난 뒤 쓸모가 없어져서
    // 캐릭터를 따라다니는 방식으로 만들었습니다.
    public class FireBallSkillEffect : ISkillCardEffect
    {
        private const string SpriteResourcePath = "Effect/fireball_temp";

        // 날아가는 속도(유닛/초)
        private const float MoveSpeed = 9f;

        // 부딪혔다고 볼 거리
        private const float HitRadius = 0.45f;

        // 대기 중 자전 반경 (캐릭터에게서 2만큼 떨어짐)
        private const float IdleDistance = 2f;

        // 대기 중 자전 속도(초당 각도)
        private const float OrbitSpeed = 120f;

        // 화면에 보이는 불덩이 크기(유닛)
        private const float VisualScale = 0.55f;

        private const float DefaultRange = 3f;
        private const float DefaultDuration = 10f;

        private static Sprite _sprite;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            return context?.Player != null;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var player = context.Player;
            if (player == null)
                return;

            var go = new GameObject("FireBall");
            go.transform.position = player.GetPosition();
            go.transform.localScale = Vector3.one * VisualScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite();
            renderer.sortingOrder = 100;   // 캐릭터·광물 위에 보이도록

            var fireBall = go.AddComponent<FireBallObject>();

            fireBall.Init(
                getCenter: () => context.Player?.GetPosition() ?? go.transform.position,
                getMonsters: () => context.GetMonsters?.Invoke(),
                maxRange: row.EffectRange > 0f ? row.EffectRange : DefaultRange,
                damage: row.EffectValue,
                duration: row.DurationTime > 0f ? row.DurationTime : DefaultDuration,
                moveSpeed: MoveSpeed,
                hitRadius: HitRadius,
                idleDistance: IdleDistance,
                orbitSpeed: OrbitSpeed);
        }

        // 임시 스프라이트. 정식 이펙트가 나오면 이 경로만 교체하면 됩니다.
        private static Sprite GetSprite()
        {
            if (_sprite == null)
                _sprite = Resources.Load<Sprite>(SpriteResourcePath);

            return _sprite;
        }
    }
}
