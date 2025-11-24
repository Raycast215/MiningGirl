using TMPro;
using UnityEngine;

namespace InGame.System.Skill.UI
{
    public interface ISkillInfoUIHandler
    {
        void SetInfo(string skillId);
        void ShowInfoUI();
        void HideInfoUI();
    }
    
    public class SkillInfoViewer : GameInitializer, ISkillInfoUIHandler
    {
        [SerializeField]
        private TMP_Text skillNameText;
        [SerializeField] 
        private TMP_Text skillDescText;

#region ISkillInfoUIHandler

        public void SetInfo(string skillId)
        {
            skillNameText.text = $"SKILL NAME {skillId}";
            skillDescText.text = $"SKILL DESC SKILL VALUE <br><color=#FFFF00>{Random.Range(10, 100)}</color> INCREASE";
        }

        public void ShowInfoUI()
        {
            gameObject.SetActive(true);
        }

        public void HideInfoUI()
        {
            gameObject.SetActive(false);
        }

#endregion
    }
}