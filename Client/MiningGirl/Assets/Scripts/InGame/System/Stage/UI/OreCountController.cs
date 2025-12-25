using UnityEngine;
using UnityEngine.UI;

namespace InGame.System.Stage.UI
{
    public class OreCountController : GameInitializer
    {
        [SerializeField]
        private OreCountUI rockCountUI;
        
        private VerticalLayoutGroup _verticalLayoutGroup;
        
        public void Initialize()
        {
            _verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            _verticalLayoutGroup.enabled = false;
            
            rockCountUI.Init();
            
            IsInitialized = true;
        }

        public void Appear()
        {
            rockCountUI.Appear();
        }
        
        public void IncreaseOreCount(int addCount)
        {
            rockCountUI.IncreaseCount(addCount);
        }
    }
}