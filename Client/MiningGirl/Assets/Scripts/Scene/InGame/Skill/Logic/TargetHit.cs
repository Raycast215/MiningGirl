using Cysharp.Threading.Tasks;
using Data;

namespace InGame.System.Skill.Logic
{
    public struct TargetHit
    {
        // public TargetHit(SkillDataRowTable data, SkillEffectDataRowTable effectData, IHit target, IInGameHandler inGameHandler)
        // {
        //     Hit(data, effectData, target, inGameHandler).Forget();
        // }
        //
        // private async UniTaskVoid Hit(SkillDataRowTable data, SkillEffectDataRowTable effectData, IHit target, IInGameHandler inGameHandler)
        // {
        //     var damage = data.EffectValueList[0];
        //     
        //     for (var i = 0; i < effectData.EffectValue; i++)
        //     {
        //         target.Damage(damage);
        //         inGameHandler.ShowDamageFloatingText((int)damage, target.GetPosition());
        //         await UniTask.WaitForSeconds(0.2f);
        //     }
        // }
    }
}