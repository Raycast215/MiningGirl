using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.System.Skill.UI
{
    public interface ISkillUIControllerHandler
    {
        ISkillPointGaugeUIHandler GetSkillPointGaugeUIHandler();
        ISkillInfoUIHandler GetSkillInfoUIHandler();
        ISkillCardRemoveUIHandler GetSkillCardRemoveUIHandler();
        void NextCard(SkillCardElementUI cardUI, bool isSkillExecuted);
        Canvas GetUICanvas();
    }
    
    public class SkillUIController : GameInitializer, ISkillUIControllerHandler
    {
        [SerializeField]
        private Canvas uiCanvas;
        [SerializeField] 
        private SkillPointGaugeUI skillPointGaugeUI;
        [SerializeField]
        private SkillInfoUI skillInfoUI;
        [SerializeField] 
        private SkillCardRemoveUI skillCardRemoveUI;
        [SerializeField]
        private SkillQueueListUI skillQueueListUI;
        [SerializeField]
        private List<SkillCardElementUI> skillUIList;

        [Header("Option")]
        [SerializeField] 
        private float appearDelay = 0.1f;
        [SerializeField]
        private float cardPosXOffset = 400.0f;

        private ISkillDataHandler _skillDataHandler;
        
        public void Init(ISkillDataHandler handler, int maxCost)
        {
            IsInitialized = false;
            
            _skillDataHandler = handler;
            
            skillInfoUI.HideInfoUI();
            skillPointGaugeUI.Init(maxCost);
            skillQueueListUI.Init();
            
            for (var i = 0; i < skillUIList.Count; i++)
            {
                skillUIList[i].Init(_skillDataHandler.GetSkillData(), this);
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

        public ISkillInfoUIHandler GetSkillInfoUIHandler()
        {
            return skillInfoUI;
        }

        public ISkillCardRemoveUIHandler GetSkillCardRemoveUIHandler()
        {
            return skillCardRemoveUI;
        }

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

            // 스킬 사용 체이닝 UI 추가.
            if (isSkillExecuted)
                skillQueueListUI.Enqueue(cardUI.Data.Id, cardUI.GetSpriteIcon);
            
            // 다음 스킬 초기롸.
            cardUI.Init(_skillDataHandler.GetSkillData(), this);
        }

        public Canvas GetUICanvas()
        {
            return uiCanvas;
        }

#endregion
    }
}