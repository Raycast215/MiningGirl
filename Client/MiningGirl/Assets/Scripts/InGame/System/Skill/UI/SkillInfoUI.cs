using Manager;
using TMPro;
using UnityEngine;

namespace InGame.System.Skill.UI
{
    public interface ISkillInfoUIHandler
    {
        void ShowInfoUI(string skillId);
        void HideInfoUI();
    }
    
    public class SkillInfoUI : GameInitializer, ISkillInfoUIHandler
    {
        [SerializeField]
        private TMP_Text skillNameText;
        [SerializeField] 
        private TMP_Text skillDescText;

        private void SetInfo(string skillId)
        {
            var skillData = DataTableManager.Instance.SkillDataTable.GetRow(skillId);
            
            skillNameText.text = $"{skillData.NameKey}";
            skillDescText.text = $"{skillData.DescKey}";
        }
        
#region ISkillInfoUIHandler

        public void ShowInfoUI(string skillId)
        {
            gameObject.SetActive(true);
            SetInfo(skillId);
        }

        public void HideInfoUI()
        {
            gameObject.SetActive(false);
        }

#endregion
    }
}