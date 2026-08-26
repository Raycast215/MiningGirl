using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.StartScene.UI
{
    // '터치해서 시작' 안내와 터치 버튼을 담당합니다.
    // 언제 보여줄지와 눌렀을 때 무엇을 할지는 StartSceneController가 정합니다.
    public class StartPromptUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("안내 문구 묶음의 루트. 보통 이 컴포넌트가 붙은 오브젝트입니다.")]
        private GameObject root;
        [SerializeField]
        private TMP_Text promptText;
        [SerializeField]
        private Button touchButton;

        [Header("깜빡임")]
        [SerializeField]
        private float blinkAlpha = 0.1f;
        [SerializeField]
        private float blinkDuration = 1.0f;

        private Tween _blinkTween;

        /// <summary>
        /// 터치 버튼에 동작을 연결합니다. 기존에 걸려 있던 것은 모두 지웁니다.
        /// </summary>
        public void Bind(Action onTouch)
        {
            if (touchButton == null)
                return;

            touchButton.onClick.RemoveAllListeners();

            if (onTouch != null)
                touchButton.onClick.AddListener(() => onTouch());
        }

        public void Show()
        {
            SetActive(true);
            StartBlink();
        }

        public void Hide()
        {
            StopBlink();
            SetActive(false);
        }

        private void SetActive(bool active)
        {
            if (root != null)
                root.SetActive(active);

            // 터치 버튼은 안내 문구의 자식이 아니라 형제라서 따로 켜고 끕니다.
            if (touchButton != null)
                touchButton.gameObject.SetActive(active);
        }

        private void StartBlink()
        {
            StopBlink();

            if (promptText != null)
                _blinkTween = promptText.DOFade(blinkAlpha, blinkDuration).SetLoops(-1, LoopType.Yoyo);
        }

        private void StopBlink()
        {
            if (_blinkTween == null)
                return;

            _blinkTween.Kill();
            _blinkTween = null;
        }

        private void OnDestroy()
        {
            StopBlink();
        }
    }
}
