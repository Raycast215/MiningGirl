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
        [Tooltip("줄어든 만큼을 잠깐 남겨 보여주는 뒷바. 없어도 동작합니다.")]
        private Image delayedFill;

        [Header("Slider")]
        [SerializeField]
        [Tooltip("Image 대신 Slider로 표시할 때 넣습니다. 넣으면 이쪽이 우선입니다.")]
        private Slider slider;

        [SerializeField]
        [Tooltip("줄어든 만큼을 잠깐 남겨 보여주는 뒷 슬라이더")]
        private Slider delayedSlider;
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

        [Header("Consume Effect")]
        [SerializeField]
        [Tooltip("뒷바가 따라오기 전 머무는 시간(초)")]
        private float delayedHold = 0.25f;

        [SerializeField]
        [Tooltip("뒷바가 줄어드는 시간(초)")]
        private float delayedDuration = 0.35f;

        [SerializeField]
        private Color delayedColor = new Color(0.85f, 0.35f, 0.25f, 0.9f);

        private Tween _fillTween;
        private Tween _delayedTween;

        // 직전 비율. 줄었는지 늘었는지 판단해 연출을 고릅니다.
        private float _lastRatio = -1f;

        private void OnDestroy()
        {
            _fillTween?.Kill();
            _fillTween = null;

            _delayedTween?.Kill();
            _delayedTween = null;

        }

        // immediate가 true면 트윈 없이 즉시 반영합니다(스테이지 시작 등).
        public void SetValue(float current, float max, bool immediate = false)
        {
            var ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);

            if (track != null)
                track.color = trackColor;

            // 값이 줄었는지 판단해 소모 연출을 고릅니다.
            var decreased = _lastRatio >= 0f && ratio < _lastRatio - 0.0001f;

            // 슬라이더를 넣었으면 그쪽을 씁니다.
            if (slider != null)
            {
                var sliderFill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;

                if (sliderFill != null)
                    sliderFill.color = dangerRatio > 0f && ratio <= dangerRatio ? dangerColor : fillColor;

                _fillTween?.Kill();
                _fillTween = null;

                if (immediate || tweenDuration <= 0f)
                    slider.value = ratio;
                else
                    _fillTween = DOTween.To(() => slider.value, v => slider.value = v, ratio, tweenDuration)
                        .SetEase(Ease.OutQuad);
            }
            else
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

            UpdateDelayedFill(ratio, immediate, decreased);

            _lastRatio = ratio;

            if (label != null)
                label.text = string.Format(format, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
        }

        // 줄어든 만큼을 잠깐 남겨 보여줍니다.
        // 얼마나 깎였는지가 눈에 보여야 소모가 체감됩니다.
        private void UpdateDelayedFill(float ratio, bool immediate, bool decreased)
        {
            // 슬라이더 쪽을 먼저 봅니다.
            if (delayedSlider != null)
            {
                var image = delayedSlider.fillRect != null ? delayedSlider.fillRect.GetComponent<Image>() : null;

                if (image != null)
                    image.color = delayedColor;

                _delayedTween?.Kill();
                _delayedTween = null;

                // 즉시 반영이거나 회복이면 바로 따라갑니다.
                if (immediate || !decreased)
                {
                    delayedSlider.value = ratio;

                    return;
                }

                _delayedTween = DOTween
                    .To(() => delayedSlider.value, v => delayedSlider.value = v, ratio, delayedDuration)
                    .SetDelay(delayedHold)
                    .SetEase(Ease.OutQuad);

                return;
            }

            if (delayedFill == null)
                return;

            delayedFill.color = delayedColor;

            if (immediate)
            {
                _delayedTween?.Kill();
                _delayedTween = null;
                delayedFill.fillAmount = ratio;

                return;
            }

            if (!decreased)
            {
                // 회복은 뒷바가 먼저 따라가야 자연스럽습니다.
                _delayedTween?.Kill();
                _delayedTween = null;
                delayedFill.fillAmount = ratio;

                return;
            }

            // 이미 따라오는 중이면 목표만 바꿔 이어갑니다.
            // 매번 새로 만들면 연속 소모 때 뒷바가 제자리에 멈춥니다.
            _delayedTween?.Kill();

            _delayedTween = delayedFill
                .DOFillAmount(ratio, delayedDuration)
                .SetDelay(delayedHold)
                .SetEase(Ease.OutQuad);
        }

        // 게임 정지 중에는 채움 트윈도 멈춥니다.
        public void SetPaused(bool paused)
        {
            SetTweenPaused(_fillTween, paused);
            SetTweenPaused(_delayedTween, paused);
        }

        private static void SetTweenPaused(Tween tween, bool paused)
        {
            if (tween == null || !tween.IsActive())
                return;

            if (paused)
                tween.Pause();
            else
                tween.Play();
        }
    }
}
