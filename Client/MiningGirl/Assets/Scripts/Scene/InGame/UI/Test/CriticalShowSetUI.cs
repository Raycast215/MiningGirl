using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI.Test
{
    public class CriticalShowSetUI : GameMonoInitializer
    {
        [SerializeField]
        private Button button;
        [SerializeField] 
        private TMP_Text textUI;

        private bool _isShow;

        private void Awake()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Set(!_isShow));
        }

        private void Start()
        {
            _isShow = PlayerPrefs.GetInt("IsCriticalShow") == 1;

            Refresh();
        }

        private void Refresh()
        {
            textUI.text = $"Critical<br>{(_isShow ? "ON" : "OFF")}";
        }

        private void Set(bool isShow)
        {
            _isShow = isShow;

            Refresh();
            
            PlayerPrefs.SetInt("IsCriticalShow", _isShow ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
