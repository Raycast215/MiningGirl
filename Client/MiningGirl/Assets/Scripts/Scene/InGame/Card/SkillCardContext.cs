using System;
using System.Collections.Generic;
using MainGame.Bonus;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace MainGame.Card
{
    // 스킬 카드가 효과를 실행할 때 필요한 것들을 한 번에 넘겨주는 묶음입니다.
    //
    // 스킬 클래스가 컨트롤러들을 직접 참조하지 않도록, 필요한 기능만 델리게이트로 받습니다.
    // (프로젝트의 기존 주입 방식과 같은 결)
    public class SkillCardContext
    {
        // 플레이어 본체 (위치 등).
        // 컨텍스트를 만드는 시점에는 아직 스폰 전일 수 있어 '그때그때 조회'합니다.
        private readonly Func<IEntity> _getPlayer;
        public IEntity Player => _getPlayer?.Invoke();

        // 지금 활성화된 몬스터 목록
                public Func<IReadOnlyList<IEntity>> GetMonsters { get; }

        // 지금 화면에 깔린 광물 목록. '이동' 카드가 조준 대상으로 씁니다.
        public Func<IReadOnlyList<IEntity>> GetResources { get; }

        // 임시 버프 적용 대상
        public TemporaryBuffState Buffs { get; }

        // 플레이어 체력 회복 (비율 0~1)
        public Action<float> RecoverStaminaByRatio { get; }

        // 카메라 (화면 안 판정용)
        public Camera Camera { get; }

        // 카드를 놓은 지점(월드 좌표). 소환 계열 스킬이 씁니다.
        // 드롭 위치를 못 구하면 플레이어 위치로 대체됩니다.
        public Vector3 DropWorldPosition { get; private set; }

        // 코스트 지급 (코스트 카드용)
        public Action<int> AddCost { get; }

        // 지정한 위치에 광물을 소환하고, 캐릭터가 그것을 우선 채굴하게 합니다.
        public Action<Vector3> SpawnSpecialResource { get; }

        // 카드를 놓은 화면 좌표를 월드 좌표로 바꿔 기억합니다.
        public void SetDropScreenPosition(Vector2 screenPosition)
        {
            var player = Player;
            var fallback = player != null ? player.GetPosition() : Vector3.zero;

            if (Camera == null)
            {
                DropWorldPosition = fallback;
                return;
            }

            // 2D라 카메라와 게임 평면의 거리를 깊이로 씁니다.
            var depth = Mathf.Abs(Camera.transform.position.z - fallback.z);
            var world = Camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));

            world.z = fallback.z;

            DropWorldPosition = world;
        }

        public SkillCardContext(
            Func<IEntity> getPlayer,
            Func<IReadOnlyList<IEntity>> getMonsters,
            TemporaryBuffState buffs,
            Action<float> recoverStaminaByRatio,
            Camera camera,
            Action<int> addCost = null,
                        Action<Vector3> spawnSpecialResource = null,
            Func<IReadOnlyList<IEntity>> getResources = null)
        {
            AddCost = addCost;
                        SpawnSpecialResource = spawnSpecialResource;
            GetResources = getResources;
            _getPlayer = getPlayer;
            GetMonsters = getMonsters;
            Buffs = buffs;
            RecoverStaminaByRatio = recoverStaminaByRatio;
            Camera = camera;
        }

        // 화면 안에 있는지 판정합니다.
        // 공격 스킬이 화면 밖 적을 때려서 "아무 일도 안 일어난 것처럼" 보이는 걸 막습니다.
        public bool IsOnScreen(Vector3 worldPosition)
        {
            if (Camera == null)
                return true;

            var viewport = Camera.WorldToViewportPoint(worldPosition);

            return viewport.z > 0f
                   && viewport.x >= 0f && viewport.x <= 1f
                   && viewport.y >= 0f && viewport.y <= 1f;
        }

        // 화면 안에서 플레이어와 가장 가까운 적을 찾습니다. 없으면 null.
        // maxRange가 0보다 크면 그 거리 안에서만 찾습니다.
        // 화면 안에서 플레이어와 가장 가까운 적을 찾습니다. 없으면 null.
        // maxRange가 0보다 크면 그 거리 안에서만 찾습니다.
        public IEntity FindNearestMonsterOnScreen(float maxRange = -1f)
        {
            var player = Player;

            return player == null ? null : FindNearestMonsterFrom(player.GetPosition(), maxRange);
        }

        // 지정한 지점에서 가장 가까운 적을 찾습니다(화면 안만). 없으면 null.
        //
        // 카드를 놓은 자리를 origin으로 넘기면 '유저가 겨냥한 곳'이 기준이 됩니다.
        // 플레이어 기준으로 잡으면 카드를 어디에 놓든 같은 적이 맞아버려서
        // 카드를 어디에 두는지가 아무 의미가 없어집니다.
        public IEntity FindNearestMonsterFrom(Vector3 origin, float maxRange = -1f)
        {
            var monsters = GetMonsters?.Invoke();

            if (monsters == null)
                return null;

            IEntity nearest = null;
            var nearestDist = float.MaxValue;

            foreach (var monster in monsters)
            {
                if (monster == null || !monster.GetActiveState())
                    continue;

                var pos = monster.GetPosition();

                if (!IsOnScreen(pos))
                    continue;

                var dist = Vector3.Distance(origin, pos);

                if (maxRange > 0f && dist > maxRange)
                    continue;

                if (dist >= nearestDist)
                    continue;

                nearestDist = dist;
                nearest = monster;
            }

            return nearest;
        }

        // 플레이어 주변 range 안의 적을 모두 찾습니다(화면 안만).
        // 플레이어 주변 range 안의 적을 모두 찾습니다(화면 안만).
        public List<IEntity> FindMonstersInRange(float range)
        {
            var player = Player;

            return player == null ? new List<IEntity>() : FindMonstersInRangeFrom(player.GetPosition(), range);
        }

        // 지정한 지점 주변 range 안의 적을 모두 찾습니다(화면 안만).
        // 지정한 지점 주변 range 안의 적을 찾습니다(화면 안만).
        //
        // maxCount가 0보다 크면 그 수만큼만, 그도 origin에서 가까운 순으로 골라 돌려줍니다.
        // 범위 안에 적이 넘칠 때 어느 쪽이 맞을지 예측되어야 조준이 의미를 갖기 때문입니다.
        // 지정한 지점 주변 range 안의 적을 찾습니다(화면 안만).
        public List<IEntity> FindMonstersInRangeFrom(Vector3 origin, float range, int maxCount = 0)
        {
            return FindInRangeFrom(GetMonsters?.Invoke(), origin, range, maxCount);
        }

        // 지정한 지점 주변 range 안의 광물을 찾습니다(화면 안만).
        public List<IEntity> FindResourcesInRangeFrom(Vector3 origin, float range, int maxCount = 0)
        {
            return FindInRangeFrom(GetResources?.Invoke(), origin, range, maxCount);
        }

        // maxCount가 0보다 크면 그 수만큼만, 그도 origin에서 가까운 순으로 골라 돌려줍니다.
        // 범위 안에 대상이 넘칠 때 어느 쪽이 잡힐지 예측되어야 조준이 의미를 갖기 때문입니다.
        //
        // 반환 전 새 리스트에 담습니다 — 원본(예: ResourceController)은 버퍼를 재사용해
        // 다음 호출에서 내용이 바뀌기 때문입니다.
        private List<IEntity> FindInRangeFrom(IReadOnlyList<IEntity> source, Vector3 origin, float range, int maxCount)
        {
            var result = new List<IEntity>();

            if (source == null)
                return result;

            for (var i = 0; i < source.Count; i++)
            {
                var entity = source[i];

                if (entity == null || !entity.GetActiveState())
                    continue;

                var pos = entity.GetPosition();

                if (!IsOnScreen(pos))
                    continue;

                if (Vector3.Distance(origin, pos) > range)
                    continue;

                result.Add(entity);
            }

            if (maxCount <= 0 || result.Count <= maxCount)
                return result;

            result.Sort((a, b) =>
                (a.GetPosition() - origin).sqrMagnitude.CompareTo((b.GetPosition() - origin).sqrMagnitude));

            result.RemoveRange(maxCount, result.Count - maxCount);

            return result;
        }
    }
}
