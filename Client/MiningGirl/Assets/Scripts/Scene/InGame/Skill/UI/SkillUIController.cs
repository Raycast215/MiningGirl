using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace InGame.System.Skill.UI
{
    public interface ISkillUIControllerHandler
    {
        ISkillPointGaugeUIHandler GetSkillPointGaugeUIHandler();
        ISkillCardRemoveUIHandler GetSkillCardRemoveUIHandler();
        // void ExecuteSkillEffect(SkillDataRowTable data);
        void NextCard(SkillCardElementUI cardUI, bool isSkillExecuted);
        Canvas GetUICanvas();
    }
    
    public class SkillUIController : GameMonoInitializer, ISkillUIControllerHandler
    {
        [SerializeField]
        private Canvas uiCanvas;
        [SerializeField] 
        private SkillPointGaugeUI skillPointGaugeUI;
        [SerializeField] 
        private SkillCardRemoveUI skillCardRemoveUI;
        [SerializeField]
        private List<SkillCardElementUI> skillUIList;

        [Header("Option")]
        [SerializeField] 
        private float appearDelay = 0.1f;
        [SerializeField]
        private float cardPosXOffset = 400.0f;
        
        public void Init(int maxCost)
        {
            IsInitialized = false;
            
            skillPointGaugeUI.Init(maxCost);
            
            for (var i = 0; i < skillUIList.Count; i++)
            {
                // skillUIList[i].Init(_skillDataHandler.GetSkillData(), this);
                skillUIList[i].SetIndex(i);
            }
            
            IsInitialized = true;
        }
        
        public async UniTaskVoid AppearUI()
        {
            skillPointGaugeUI.Appear();
            
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

        public ISkillCardRemoveUIHandler GetSkillCardRemoveUIHandler()
        {
            return skillCardRemoveUI;
        }

        // public void ExecuteSkillEffect(SkillDataRowTable data)
        // {
        //     _skillDataHandler.ExecuteSkillEffect(data);
        // }

        public void NextCard(SkillCardElementUI cardUI, bool isSkillExecuted)
        {
            var offsetList = new List<int>() { -1, 0, 1 };
            var index = 0;
            
            foreach (var ui in skillUIList.OrderBy(x => x.Index))
            {
                if (ui == cardUI)
                    continue;
                
                var posX = offsetList[index] * cardPosXOffset;
                
                ui.SetIndex(index);
                ui.Move(new Vector2(posX, 0.0f));
                index += 1;
            }
            
            cardUI.SetIndex(index);
            cardUI.Move(new Vector2(offsetList[index] * cardPosXOffset, 0.0f));
            cardUI.Deselect();

            // // 스킬 사용 체이닝 UI 추가.
            // if (isSkillExecuted)
            //     skillQueueListUI.Enqueue(cardUI.Data.Id, cardUI.GetSpriteIcon);
            
            // 다음 스킬 초기롸.
            // cardUI.Init(_skillDataHandler.GetSkillData(), this);
        }

        public Canvas GetUICanvas()
        {
            return uiCanvas;
        }

#endregion
    }
}