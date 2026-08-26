using Cysharp.Threading.Tasks;
using Data;
using Manager;
using UnityEngine;

namespace Legacy.MainGame.Card.Effects
{
    // 아이스볼트 — 캐릭터 자리에서 카드를 놓은 방향으로 얼음 화살을 쏩니다.
    // 처음 맞은 몬스터 하나가 피해를 입고, 이펙트는 그 자리에서 사라집니다.
    //
    // 다른 공격 카드는 '놓은 자리 주변 최근접 N명'을 자동으로 잡습니다.
    // 이 카드만 방향을 보고 날아가서, 어디에 놓느냐가 실제 명중을 좌우합니다.
    public class IceBoltSkillEffect : ISkillCardEffect
    {
        // 애드레서블 주소(그룹 Skill_Effect)
        private const string PrefabName = "Effect_IceBolt";

        // 날아가는 속도(유닛/초). 조준한 곳까지 기다리는 느낌이 없도록 파이어볼보다 빠릅니다.
        private const float MoveSpeed = 14f;

        // 맞았다고 볼 거리
        private const float HitRadius = 0.5f;

        // 시트에 사거리가 없을 때 쓰는 최대 비행 거리
        private const float DefaultRange = 15f;

        // 원본 스프라이트가 2x1 유닛이라 몬스터보다 큽니다. 화면에 보이는 크기만 줄입니다.
        private const float VisualScale = 0.8f;

        // 놓은 자리가 캐릭터와 거의 겹치면 방향을 정할 수 없습니다.
        private const float MinAimDistance = 0.2f;

        // 프리팹을 처음 쓸 때 불러오면 첫 발이 한 박자 늦게 나갑니다.
        // 드래그를 시작하면 CanExecute가 매 프레임 불리므로, 그때 미리 받아 둡니다.
        private bool _isPrewarmStarted;

        public bool CanExecute(SkillCardContext context, SkillCardDataTableRow row)
        {
            Prewarm();

            if (context == null || context.Player == null)
                return false;

            // 적이 없어도 쏠 수 있습니다.
            // 방향만 정해지면 나가고, 맞히는 것은 조준한 사람 몫입니다.
            return GetAimDirection(context).sqrMagnitude > 0f;
        }

        public void Execute(SkillCardContext context, SkillCardDataTableRow row)
        {
            var direction = GetAimDirection(context);

            if (direction.sqrMagnitude <= 0f)
                return;

            SpawnAsync(context, row, context.Player.GetPosition(), direction).Forget();
        }

        // 캐릭터 → 카드를 놓은 자리. 너무 가까우면 방향을 못 정하므로 0을 돌려줍니다.
        private static Vector3 GetAimDirection(SkillCardContext context)
        {
            var player = context != null ? context.Player : null;

            if (player == null)
                return Vector3.zero;

            var aim = context.DropWorldPosition - player.GetPosition();

            aim.z = 0f;

            return aim.magnitude < MinAimDistance ? Vector3.zero : aim.normalized;
        }

        private static async UniTaskVoid SpawnAsync(
            SkillCardContext context, SkillCardDataTableRow row, Vector3 origin, Vector3 direction)
        {
            var manager = AddressableManager.Instance;

            if (manager == null)
                return;

            var prefab = await manager.LoadAsset<GameObject>(PrefabName);

            if (prefab == null)
                return;

            var go = Object.Instantiate(prefab);

            go.name = "IceBolt";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * VisualScale;

            HideDuplicateRootSprite(go);

            var bolt = go.AddComponent<IceBoltObject>();

            bolt.Init(
                getMonsters: () => context.GetMonsters != null ? context.GetMonsters.Invoke() : null,
                direction: direction,
                damage: row.EffectValue,
                moveSpeed: MoveSpeed,
                hitRadius: HitRadius,
                maxDistance: row.EffectRange > 0f ? row.EffectRange : DefaultRange);
        }

        // 프리팹 루트에 애니메이션 첫 프레임과 같은 스프라이트가 한 장 더 붙어 있습니다.
        // 루트가 자식보다 카메라에 가까워서, 그대로 두면 정지 이미지가 애니메이션을 가립니다.
        // 실제로 움직이는 쪽은 Animator가 달린 자식입니다.
        private static void HideDuplicateRootSprite(GameObject go)
        {
            var rootRenderer = go.GetComponent<SpriteRenderer>();

            if (rootRenderer == null)
                return;

            var animated = go.GetComponentInChildren<Animator>(true);

            if (animated == null || animated.gameObject == go)
                return;

            rootRenderer.enabled = false;
        }

        private void Prewarm()
        {
            if (_isPrewarmStarted)
                return;

            var manager = AddressableManager.Instance;

            if (manager == null)
                return;

            _isPrewarmStarted = true;

            manager.LoadAsset<GameObject>(PrefabName).Forget();
        }
    }
}
