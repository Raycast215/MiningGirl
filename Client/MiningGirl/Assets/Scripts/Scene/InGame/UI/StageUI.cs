using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 현재 스테이지 번호를 '표시만' 합니다.
    //
    // 스테이지 번호 자체는 RunState.Stage가 들고 있습니다.
    // (예전에는 이 클래스가 번호의 원본이라, 세이브가 UI에서 값을 읽어갔습니다.)
    public class StageUI : GameMonoInitializer
    {
        [SerializeField]
        private TextMeshProUGUI stageText;

        [SerializeField]
        [Tooltip("표시 형식. {0}에 스테이지 번호가 들어갑니다.")]
        private string format = "스테이지 {0}";

        public void Init()
        {
            SetStage(1);

            IsInitialized = true;
        }

        public void SetStage(int stage)
        {
            if (stageText == null)
                return;

            stageText.text = string.Format(format, Mathf.Max(1, stage));
        }
    }
}
