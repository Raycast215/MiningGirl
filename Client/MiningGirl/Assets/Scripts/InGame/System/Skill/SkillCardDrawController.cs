using System.Collections.Generic;
using System.Linq;
using Data;
using Manager;
using UnityEngine;

namespace InGame.System.Skill
{
    public class SkillCardDrawController
    {
        private int WeightSum { get; }
        private int StartCardCount { get; }
        private List<SkillDataRowTable> SkillDataList { get; }
        
        public SkillCardDrawController(int startCardCount)
        {
            StartCardCount = startCardCount;
            SkillDataList = new List<SkillDataRowTable>();
            
            var skillDataTable = DataTableManager.Instance.SkillDataTable;
            foreach (var data in  DataTableManager.Instance.StartingSkillDataTable.Rows)
            {
                for (var i = 0; i < data.Count; i++)
                {
                    
                    SkillDataList.Add(skillDataTable.GetRow(data.SkillId));
                }
            }
            
            // 가중치 합 계산.
            WeightSum = SkillDataList.Sum(x => x.Weight);
        }
        
        public SkillDataRowTable GetSkillData()
        {
            var add = 0;
            var rand = Random.Range(0, WeightSum + 1);

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