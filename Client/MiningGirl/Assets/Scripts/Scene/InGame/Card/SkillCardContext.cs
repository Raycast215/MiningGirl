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

        // 임시 버프 적용 대상
        public TemporaryBuffState Buffs { get; }

        // 플레이어 체력 회복 (비율 0~1)
        public Action<float> HealPlayerByRatio { get; }

        // 카메라 (화면 안 판정용)
        public Camera Camera { get; }

        public SkillCardContext(
            Func<IEntity> getPlayer,
            Func<IReadOnlyList<IEntity>> getMonsters,
            TemporaryBuffState buffs,
            Action<float> healPlayerByRatio,
            Camera camera)
        {
            _getPlayer = getPlayer;
            GetMonsters = getMonsters;
            Buffs = buffs;
            HealPlayerByRatio = healPlayerByRatio;
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
        public IEntity FindNearestMonsterOnScreen(float maxRange = -1f)
        {
            var monsters = GetMonsters?.Invoke();
            var player = Player;

            if (monsters == null || player == null)
                return null;

            var origin = player.GetPosition();
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
        public List<IEntity> FindMonstersInRange(float range)
        {
            var result = new List<IEntity>();
            var monsters = GetMonsters?.Invoke();
            var player = Player;

            if (monsters == null || player == null)
                return result;

            var origin = player.GetPosition();

            foreach (var monster in monsters)
            {
                if (monster == null || !monster.GetActiveState())
                    continue;

                var pos = monster.GetPosition();
                if (!IsOnScreen(pos))
                    continue;

                if (Vector3.Distance(origin, pos) > range)
                    continue;

                result.Add(monster);
            }

            return result;
        }
    }
}
