using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 게이지 하나를 그리는 공용 뷰. 스태미나와 채굴 진행도가 같은 모양을 쓰므로 한 클래스로 묶었습니다.
    // 값 계산은 하지 않고 표시만 담당합니다.
    public class GaugeBarView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Image track;
        [SerializeField]
        private Image fill;
        [SerializeField]
        private TMP_Text label;

        [Header("Colors")]
        [SerializeField]
        private Color trackColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        [SerializeField]
        private Color fillColor = new Color(0.11f, 0.62f, 0.46f, 1f);

        [SerializeField]
        [Tooltip("값이 이 비율 아래로 떨어지면 경고 색으로 바뀝니다. 0이면 경고 색을 쓰지 않습니다.")]
        [Range(0f, 1f)]
        private float dangerRatio = 0.3f;

        [SerializeField]
        private Color dangerColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        [Header("Options")]
        [SerializeField]
        [Tooltip("채움이 부드럽게 따라가는 시간(초). 0이면 즉시 반영합니다.")]
        private float tweenDuration = 0.2f;

        [SerializeField]
        [Tooltip("'{0} / {1}' 형식. 앞이 현재 값, 뒤가 최대 값입니다.")]
        private string format = "{0} / {1}";

        private Tween _fillTween;

        private void OnDestroy()
        {
            _fillTween?.Kill();
            _fillTween = null;
        }

        // immediate가 true면 트윈 없이 즉시 반영합니다(스테이지 시작 등).
        public void SetValue(float current, float max, bool immediate = false)
        {
            var ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);

            if (track != null)
                track.color = trackColor;

            if (fill != null)
            {
                // 위험 구간에서 색을 바꿔 눈에 띄게 합니다.
                fill.color = dangerRatio > 0f && ratio <= dangerRatio ? dangerColor : fillColor;

                _fillTween?.Kill();
                _fillTween = null;

                if (immediate || tweenDuration <= 0f)
                    fill.fillAmount = ratio;
                else
                    _fillTween = fill.DOFillAmount(ratio, tweenDuration).SetEase(Ease.OutQuad);
            }

            if (label != null)
                label.text = string.Format(format, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
        }

        // 게임 정지 중에는 채움 트윈도 멈춥니다.
        public void SetPaused(bool paused)
        {
            if (_fillTween == null)
                return;

            if (paused)
                _fillTween.Pause();
            else
                _fillTween.Play();
        }
    }
}
