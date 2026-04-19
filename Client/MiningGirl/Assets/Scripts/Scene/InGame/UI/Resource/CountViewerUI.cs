using TMPro;
using UnityEngine;

namespace Scene.InGame.UI.Resource
{
    public class CountViewerUI : GameInitializer
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