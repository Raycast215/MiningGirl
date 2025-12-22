using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;

namespace InGame.System.Skill.Logic
{
    public struct RangeAll
    {
        public RangeAll(SkillDataRowTable data, SkillEffectDataRowTable effectData, IInGameHandler inGameHandler)
        {
            Hit(data, effectData, inGameHandler).Forget();
        }
        
        private async UniTaskVoid Hit(SkillDataRowTable data, SkillEffectDataRowTable effectData, IInGameHandler inGameHandler)
        {
            for (var i = 0; i < effectData.EffectValue; i++)
            {
                var playerPos = inGameHandler.GetPlayerTransform().localPosition;
                var targetList = inGameHandler.GetEnemyList()
                    .Where(x => Vector3.Distance(playerPos, x.GetPosition()) < data.EffectValueList[1])
                    .ToList();

                foreach (var target in targetList)
                {
                    target.Damage();
                }
                
                await UniTask.WaitForSeconds(0.2f);
            }
        }
    }
}