using Cysharp.Threading.Tasks;
using Data;

namespace InGame.System.Skill.Logic
{
    public struct TargetHit
    {
        public TargetHit(SkillDataRowTable data, SkillEffectDataRowTable effectData, IHit target)
        {
            Hit(data, effectData, target).Forget();
        }

        private async UniTaskVoid Hit(SkillDataRowTable data, SkillEffectDataRowTable effectData, IHit target)
        {
            for (var i = 0; i < effectData.EffectValue; i++)
            {
                target.Damage();
                await UniTask.WaitForSeconds(0.2f);
            }
        }
    }
}