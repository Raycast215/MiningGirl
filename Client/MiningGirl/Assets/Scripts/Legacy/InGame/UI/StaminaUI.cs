using UI.Common;
using UnityEngine;

namespace Legacy.MainGame.UI
{
    // 스태미나 게이지를 '표시만' 합니다.
    //
    // 수치와 소모·회복 규칙은 RunState.Stamina(StaminaState)가 들고 있습니다.
    // (예전에는 이 클래스가 Current/Max와 소모 공식을 전부 갖고 있었습니다.)
    public class StaminaUI : MonoBehaviour
    {
        [SerializeField]
        private GaugeBarView view;

        public void SetValue(float current, float max, bool immediate = false)
        {
            if (view != null)
                view.SetValue(current, max, immediate);
        }

        public void SetPaused(bool paused)
        {
            if (view != null)
                view.SetPaused(paused);
        }
    }
}
