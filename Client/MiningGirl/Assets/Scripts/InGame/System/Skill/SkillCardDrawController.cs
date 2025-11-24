using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InGame.System.Skill
{
    public class SkillCardDrawController
    {
        private int Sum { get; }
        private int StartCardCount { get; }
        private List<SkillData> SkillDataList { get; }
        
        public SkillCardDrawController(int startCardCount, List<SkillData> skillDataList)
        {
            StartCardCount = startCardCount;
            SkillDataList = skillDataList;
            Sum = SkillDataList.Sum(x => x.Weight);
        }

        public List<SkillData> GetStartingData()
        {
            var toList = new List<SkillData>();
            
            for (var i = 0; i < StartCardCount; i++)
            {
                toList.Add(GetSkillData());
            }
            
            return toList;
        }
        
        public SkillData GetSkillData()
        {
            var add = 0;
            var rand = Random.Range(0, Sum + 1);

            foreach (var data in SkillDataList)
            {
                // 가중치 합산.
                add += data.Weight;
                
                if (rand <= add)
                    return data;
            }

            return null;
        }
    }
}