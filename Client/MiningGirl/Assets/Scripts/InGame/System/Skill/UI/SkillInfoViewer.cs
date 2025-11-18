using TMPro;
using UnityEngine;

namespace InGame.System.Skill.UI
{
    public class SkillInfoViewer : GameInitializer
    {
        [SerializeField]
        private TMP_Text skillNameText;
        [SerializeField] 
        private TMP_Text skillDescText;

        public void Set(int index)
        {
            skillNameText.text = $"SKILL NAME {index}";
            skillDescText.text = $"SKILL DESC SKILL VALUE <br><color=#FFFF00>{Random.Range(10, 100)}</color> INCREASE";
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
