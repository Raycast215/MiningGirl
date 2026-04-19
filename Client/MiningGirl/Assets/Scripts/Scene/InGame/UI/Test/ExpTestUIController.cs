using UnityEngine;

namespace Scene.InGame.UI.Level.Test
{
    public class ExpTestUIController : GameInitializer
    {
        [SerializeField] 
        private ExpTestUI expX1;
        [SerializeField] 
        private ExpTestUI expX5;
        [SerializeField]
        private ExpTestUI expX10;

        public void Init(IInGameUIHandler handler)
        {
            expX1.Init(() => handler.AddExpCount(1));
            expX5.Init(() => handler.AddExpCount(5));
            expX10.Init(() => handler.AddExpCount(10));
        }
    }
}