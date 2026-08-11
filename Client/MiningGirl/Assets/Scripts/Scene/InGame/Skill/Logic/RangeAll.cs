using Cysharp.Threading.Tasks;
using Data;

namespace InGame.System.Skill.Logic
{
    public static class RangeAll
    {
        // public static async UniTaskVoid Execute(SkillDataRowTable data, SkillEffectDataRowTable effectData, IInGameHandler handler)
        // {
        //     var range = data.EffectValueList[1];
        //     var rangeSqr = range * range;
        //     var damage = data.EffectValueList[0];
        //     var playerPos = handler.GetPlayerTransform().localPosition;
        //     var enemyList = handler.GetEnemyList();
        //
        //     for (var i = 0; i < effectData.EffectValue; i++)
        //     {
        //         foreach (var target in enemyList)
        //         {
        //             if (!target.GetActiveState()) 
        //                 continue;
        //
        //             var delta = target.GetPosition() - playerPos;
        //             if (delta.sqrMagnitude > rangeSqr)
        //                 continue;
        //
        //             target.Damage(damage);
        //             handler.ShowDamageFloatingText((int)damage, target.GetPosition());
        //         }
        //
        //         await UniTask.WaitForSeconds(0.2f);
        //     }
        // }
    }
}