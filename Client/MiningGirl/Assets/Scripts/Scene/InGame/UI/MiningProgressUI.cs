using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 목표 채굴량 대비 현재 채굴량을 '표시만' 합니다. 게이지 없이 숫자만 보여줍니다.
    //
    // 목표 계산과 클리어 판정은 RunState.Mining(MiningState)이 합니다.
    public class MiningProgressUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("'{0} / {1}' 형식. 앞이 현재 채굴량, 뒤가 목표입니다.")]
        private TMP_Text label;

        [SerializeField]
        private string format = "{0} / {1}";

        public void SetValue(int current, int goal)
        {
            if (label != null)
                label.text = string.Format(format, current, goal);
        }

        // 게이지 트윈이 없어 멈출 것이 없지만, 호출부가 다른 UI와 같은 흐름을 쓰므로 형태를 맞춰 둡니다.
        public void SetPaused(bool paused)
        {
        }
    }
}
