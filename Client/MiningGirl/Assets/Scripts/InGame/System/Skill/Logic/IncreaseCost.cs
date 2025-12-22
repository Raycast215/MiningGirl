using System;
using System.Linq;
using Data;

namespace InGame.System.Skill.Logic
{
    public struct IncreaseCost
    {
        public IncreaseCost(SkillDataRowTable data, Action<int> callback)
        {
            callback?.Invoke((int)data.EffectValueList.FirstOrDefault());
        }
    }
}
