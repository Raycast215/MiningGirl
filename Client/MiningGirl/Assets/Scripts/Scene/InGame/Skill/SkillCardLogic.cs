using System;
using Data;
using InGame.System.Skill.UI;
using UnityEngine;

namespace InGame.System.Skill
{
    public interface ISkillLogicHandler
    {
        void StartDrag();
        void Drag(float deltaY);
        void EndDrag(float deltaY, Action callback);
    }
    
    public class SkillCardLogic : ISkillLogicHandler
    {
        public bool IsSelected { get; private set; }
        
      
        private ISkillCardUIHandler SkillCardUIHandler { get; set; }
        private float CardRemoveTargetPosY => -20.0f;
        private float CardExecuteTargetPosY => 700.0f;

        public SkillCardLogic(ISkillCardUIHandler skillCardUIHandler)
        {
            SkillCardUIHandler = skillCardUIHandler;
            IsSelected = false;
        }
        
        private void ExecuteSkill(Action callback)
        {
            var skillPointUI = SkillCardUIHandler
                .GetSkillUIControllerHandler()
                .GetSkillPointGaugeUIHandler();
            
            // if (skillPointUI.GetSkillPoint() >= Data.Cost)
            // {
            //     SkillCardUIHandler.ChangeSkillCard(true);
            //     SkillCardUIHandler.ExecuteSkillEffect();
            //     skillPointUI.UpdateGaugeUI((int)-Data.Cost);
            //     Debug.Log($"스킬 발동! Id: {Data.Id}, Cost: {Data.Cost}");
            // }
            // else
            // {
            //     Clear();
            //     
            //     // 카드 선택 해제.
            //     SkillCardUIHandler.Deselect();
            //     
            //     callback?.Invoke();
            //     
            //     Debug.Log("스킬 포인트 부족... 스킬 사용 실패");
            // }
        }

        private void DeleteCard(Action callback)
        {
            var skillPointUI = SkillCardUIHandler
                .GetSkillUIControllerHandler()
                .GetSkillPointGaugeUIHandler();
            
            if (skillPointUI.GetSkillPoint() > 0)
            {
                SkillCardUIHandler.ChangeSkillCard(false);
                skillPointUI.UpdateGaugeUI(-1);
                // Debug.Log($"카드 삭제! Id: {Data.Id}, Cost: {Data.Cost}");
            }
            else
            {
                Clear();
                
                // 카드 선택 해제.
                SkillCardUIHandler.Deselect();
                
                callback?.Invoke();
                
                Debug.Log("스킬 포인트 부족... 카드 삭제 실패");
            }
        }

        private void Clear()
        {
            IsSelected = false;
            
            // 카드 제거 UI 비활성화.
            SkillCardUIHandler
                .GetSkillUIControllerHandler()
                .GetSkillCardRemoveUIHandler()
                .HideCardRemoveUI();
        }

#region ISkillLogicHandler

        public void StartDrag()
        {
            IsSelected = true;
            
            // 카드 선택.
            SkillCardUIHandler.Select();
        }
        
        public void Drag(float deltaY)
        {
            // 카드 발동 UI 구역인 경우.
            if (deltaY > CardExecuteTargetPosY)
                SkillCardUIHandler.ShowEffect();
            else
                SkillCardUIHandler.HideEffect();
            
            // 카드 제거 UI 구역인 경우.
            if (deltaY < CardRemoveTargetPosY)
            {
                // 카드 제거 UI 활성화.
                SkillCardUIHandler
                    .GetSkillUIControllerHandler()
                    .GetSkillCardRemoveUIHandler()
                    .ShowCardRemoveUI();
            }
            else
            {
                // 카드 제거 UI 비활성화.
                SkillCardUIHandler
                    .GetSkillUIControllerHandler()
                    .GetSkillCardRemoveUIHandler()
                    .HideCardRemoveUI();
            }
        }
        
        public void EndDrag(float deltaY, Action callback)
        {
            Clear();
            
            // 카드 삭제
            if (deltaY < CardRemoveTargetPosY)
            {
                DeleteCard(callback);
                return;
            }
            
            // 스킬 발동
            if (deltaY > CardExecuteTargetPosY)
            {
                ExecuteSkill(callback);
                return;
            }
            
            // 카드 선택 해제.
            SkillCardUIHandler.Deselect();
            callback?.Invoke();
        }

#endregion
    }
}