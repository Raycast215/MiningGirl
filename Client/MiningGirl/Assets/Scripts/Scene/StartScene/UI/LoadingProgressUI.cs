using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.StartScene.UI
{
    // 로딩 게이지와 상태 메시지를 '표시만' 합니다.
    // 무엇을 얼마나 불러왔는지는 StartSceneController가 판단하고, 여기서는 받은 값을 그리기만 합니다.
    public class LoadingProgressUI : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;
        [SerializeField]
        private TMP_Text messageText;

        private Tween _progressTween;

        public void Show()
        {
            SetActive(true);
        }

        public void Hide()
        {
            KillProgressTween();
            SetActive(false);
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
                messageText.text = message;
        }

        public void SetProgress(float value)
        {
            KillProgressTween();

            if (slider != null)
                slider.value = value;
        }

        /// <summary>
        /// 게이지를 목표치까지 서서히 채웁니다. 완료를 기다려야 하면 호출부에서 duration만큼 대기합니다.
        /// </summary>
        public void AnimateProgress(float target, float duration)
        {
            KillProgressTween();

            if (slider != null)
                _progressTween = slider.DOValue(target, duration);
        }

        private void SetActive(bool active)
        {
            if (slider != null)
                slider.gameObject.SetActive(active);

            // 메시지가 게이지의 자식이면 위에서 같이 꺼지지만, 밖으로 빼는 경우를 대비해 따로 처리합니다.
            if (messageText != null)
                messageText.gameObject.SetActive(active);
        }

        private void KillProgressTween()
        {
            if (_progressTween == null)
                return;

            _progressTween.Kill();
            _progressTween = null;
        }

        private void OnDestroy()
        {
            KillProgressTween();
        }
    }
}
