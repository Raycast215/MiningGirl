using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.System.Skill.UI
{
    public interface ISkillUIControllerHandler
    {
        ISkillPointGaugeUIHandler GetSkillPointGaugeUIHandler();
        ISkillInfoUIHandler GetSkillInfoUIHandler();
        ISkillCardRemoveUIHandler GetSkillCardRemoveUIHandler();
    }
    
    public class SkillUIController : GameInitializer, ISkillUIControllerHandler
    {
        [SerializeField] 
        private SkillPointGaugeUI skillPointGaugeUI;
        [SerializeField]
        private SkillInfoUI skillInfoUI;
        [SerializeField] 
        private SkillCardRemoveUI skillCardRemoveUI;
        [SerializeField]
        private List<SkillCardElementUI> skillUIList;

        [Header("Option")]
        [SerializeField] 
        private float appearDelay = 0.1f;
        
        public void Init(List<SkillData> startingList, int maxCost)
        {
            IsInitialized = false;
            
            skillInfoUI.HideInfoUI();
            skillPointGaugeUI.Init(maxCost);
            
            for (var i = 0; i < skillUIList.Count; i++)
            {
                skillUIList[i].Init(startingList[i], this);
            }
            
            IsInitialized = true;
        }
        
        public async UniTaskVoid AppearUI()
        {
            foreach (var skillUI in skillUIList)
            {
                await UniTask.WaitForSeconds(appearDelay);
                skillUI.Appear();
            }
        }

#region ISkillUIControllerHandler

        public ISkillPointGaugeUIHandler GetSkillPointGaugeUIHandler()
        {
            return skillPointGaugeUI;
        }

        public ISkillInfoUIHandler GetSkillInfoUIHandler()
        {
            return skillInfoUI;
        }

        public ISkillCardRemoveUIHandler GetSkillCardRemoveUIHandler()
        {
            return skillCardRemoveUI;
        }

#endregion
    }
}