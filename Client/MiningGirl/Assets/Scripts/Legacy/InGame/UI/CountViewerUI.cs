using TMPro;
using UnityEngine;

namespace Legacy.Scene.InGame.UI.Resource
{
    public class CountViewerUI : GameMonoInitializer
    {
        [SerializeField]
        private TMP_Text countText;

        private int _count;
        
        public void SetCount(int count)
        {
            _count = count;
            countText.text = $"{_count}";
        }

        public void AddCount(int add)
        {
            _count += add;
            countText.text = $"{_count}";
        }
    }
}