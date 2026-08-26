using System.Collections.Generic;
using Scene.MainGameScene.Battle;
using UnityEngine;

namespace Scene.MainGameScene.Progress
{
    // 보유 스킬의 쿨다운을 각자 돌리고, 차면 알아서 쏩니다.
    //
    // 인게임에 터치 조작이 없으므로 발동 판단이 전부 여기 모입니다.
    public class SkillRunner
    {
        // 발사체가 여러 발일 때 출발점을 좌우로 벌리는 간격.
        // 같은 지점에서 동시에 나가면 초반 구간이 겹쳐 한 발처럼 보입니다.
        private const float MuzzleSpacing = 0.4f;

        private readonly SkillInventory _inventory;
        private readonly MonsterField _field;
        private readonly ProjectileLauncher _launcher;
        private readonly Transform _muzzle;

        // 스킬 Id별 남은 쿨다운.
        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        private readonly List<MonsterUnit> _targetBuffer = new List<MonsterUnit>();

        public SkillRunner(SkillInventory inventory, MonsterField field, ProjectileLauncher launcher, Transform muzzle)
        {
            _inventory = inventory;
            _field = field;
            _launcher = launcher;
            _muzzle = muzzle;
        }

        // 슬롯 UI가 원형 게이지를 그리는 데 씁니다.
        public float GetCooldownRatio(SkillState skill)
        {
            if (skill == null || !_cooldowns.TryGetValue(skill.Row.Id, out var remaining))
                return 0f;

            var total = skill.Cooldown;

            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        public void Tick(float deltaTime)
        {
            var skills = _inventory.Skills;

            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];

                _cooldowns.TryGetValue(skill.Row.Id, out var remaining);

                if (remaining > 0f)
                {
                    remaining -= deltaTime;
                    _cooldowns[skill.Row.Id] = remaining;

                    if (remaining > 0f)
                        continue;
                }

                // 타겟이 없으면 쏘지 않고 쿨이 찬 채로 기다립니다.
                // 적이 들어오는 순간 바로 나가야 초반 웨이브가 답답하지 않습니다.
                if (_field.AliveCount == 0)
                {
                    _cooldowns[skill.Row.Id] = 0f;

                    continue;
                }

                Fire(skill);

                _cooldowns[skill.Row.Id] = skill.Cooldown;
            }
        }

        // 스킬을 새로 얻으면 첫 발이 바로 나가도록 쿨을 0으로 둡니다.
        public void ResetCooldown(SkillState skill)
        {
            if (skill != null)
                _cooldowns[skill.Row.Id] = 0f;
        }

        private void Fire(SkillState skill)
        {
            var count = skill.ProjectileCount;
            var origin = _muzzle.position;

            // 1발이면 가장 가까운 적, 2발 이상이면 가까운 적들 중에서 발사체마다 따로 뽑습니다.
            // 같은 적이 나와도 상관없습니다. 목적은 효율이 아니라 겹쳐 보이지 않는 것입니다.
            if (count <= 1)
            {
                var target = _field.FindNearest(origin);

                if (target == null)
                    return;

                FireOne(skill, origin, target.Position);

                return;
            }

            _field.FillNearest(origin, count * 2, _targetBuffer);

            if (_targetBuffer.Count == 0)
                return;

            for (var i = 0; i < count; i++)
            {
                // 가운데를 기준으로 좌우 대칭이 되게 벌립니다.
                var offset = (i - (count - 1) * 0.5f) * MuzzleSpacing;
                var muzzle = origin + new Vector3(offset, 0f, 0f);
                var target = _targetBuffer[Random.Range(0, _targetBuffer.Count)];

                FireOne(skill, muzzle, target.Position);
            }
        }

        private void FireOne(SkillState skill, Vector3 origin, Vector3 targetPosition)
        {
            var toTarget = targetPosition - origin;

            _launcher.Fire(skill.BuildProjectileSpec(), origin, toTarget, toTarget.magnitude);
        }
    }
}
