using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 현재 스테이지 번호를 표시합니다.
    // 지금은 반복할 때마다 인덱스만 올라가고, 보스전/이벤트 구분은 나중에 붙입니다.
    public class StageUI : GameMonoInitializer
    {
        [SerializeField]
        private TextMeshProUGUI stageText;

        [SerializeField]
        [Tooltip("표시 형식. {0}에 스테이지 번호가 들어갑니다.")]
        private string format = "스테이지 {0}";

        public int Stage { get; private set; } = 1;

        public void Init()
        {
            SetStage(1);

            IsInitialized = true;
        }

        public void SetStage(int stage)
        {
            Stage = Mathf.Max(1, stage);

            UpdateText();
        }

        // 다음 스테이지로 넘어갈 때 호출합니다.
        public void NextStage()
        {
            SetStage(Stage + 1);
        }

        private void UpdateText()
        {
            if (stageText == null)
                return;

            stageText.text = string.Format(format, Stage);
        }
    }
}
