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

                // 조준할 적이 없으면 쏘지 않고 쿨이 찬 채로 기다립니다.
                // 적이 들어오는 순간 바로 나가야 초반 웨이브가 답답하지 않습니다.
                //
                // 화면에 적이 보여도 전부 죽을 예정이면 쏘지 않습니다. 시체에 쏘고
                // 쿨을 돌리는 것보다 0.2~0.4초 기다렸다 쏘는 쪽이 실질 화력이 높습니다.
                _cooldowns[skill.Row.Id] = Fire(skill) ? skill.Cooldown : 0f;
            }
        }

        // 스킬을 새로 얻으면 첫 발이 바로 나가도록 쿨을 0으로 둡니다.
        public void ResetCooldown(SkillState skill)
        {
            if (skill != null)
                _cooldowns[skill.Row.Id] = 0f;
        }

        // 한 발이라도 나갔으면 true. 조준할 적이 없어 못 쐈으면 false입니다.
        private bool Fire(SkillState skill)
        {
            var count = skill.ProjectileCount;
            var origin = _muzzle.position;

            if (count <= 1)
            {
                var target = _field.FindNearestTargetable(origin);

                if (target == null)
                    return false;

                FireOne(skill, origin, target);

                return true;
            }

            // 발사체마다 서로 다른 적을 하나씩 맡습니다.
            //
            // 후보가 발사체 수보다 적으면 그만큼만 쏘고 남는 발은 버립니다. 같은 적에
            // 몰아 쏘면 앞 발이 죽인 뒤 나머지가 시체로 날아가기 때문입니다. 그래서
            // 다발형은 적이 많을 때 강하고 적을 때는 단발과 다르지 않습니다.
            _field.FillNearestTargetable(origin, count, _targetBuffer);

            var fired = _targetBuffer.Count;

            if (fired == 0)
                return false;

            for (var i = 0; i < fired; i++)
            {
                // 실제로 나가는 수를 기준으로 가운데 대칭이 되게 벌립니다.
                var offset = (i - (fired - 1) * 0.5f) * MuzzleSpacing;

                FireOne(skill, origin + new Vector3(offset, 0f, 0f), _targetBuffer[i]);
            }

            return true;
        }

        private void FireOne(SkillState skill, Vector3 origin, MonsterUnit target)
        {
            var toTarget = target.Position - origin;

            _launcher.Fire(skill.BuildProjectileSpec(), origin, toTarget, toTarget.magnitude, target);
        }
    }
}
