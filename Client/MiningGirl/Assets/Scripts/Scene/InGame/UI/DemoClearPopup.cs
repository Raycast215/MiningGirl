using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 마지막 스테이지를 깨면 뜨는 데모 종료 안내.
    // 확인을 누르면 저장을 지우고 시작 씬으로 돌아갑니다.
    public class DemoClearPopup : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI messageText;

        [SerializeField]
        private TextMeshProUGUI resultText;

        [SerializeField]
        private Button confirmButton;

        private Action _onConfirm;

        private void Awake()
        {
            if (confirmButton == null)
                return;

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                var callback = _onConfirm;
                _onConfirm = null;

                Hide();

                callback?.Invoke();
            });
        }

        // clearedStage: 마지막으로 깬 스테이지, gold: 남은 골드
        public void Show(int clearedStage, int gold, Action onConfirm)
        {
            _onConfirm = onConfirm;

            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = "데모 클리어";

            if (messageText != null)
                messageText.text = "여기까지가 데모 버전입니다.\n플레이해 주셔서 감사합니다.";

            // 이번 런의 성과를 간단히 보여줍니다.
            if (resultText != null)
                resultText.text = $"도달 스테이지 {clearedStage}   보유 골드 {gold}";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
