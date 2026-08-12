using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 레벨과 경험치를 '그리기만' 하는 뷰.
    // 레벨업 연출의 진행 순서는 바깥(LevelExpUI)에서 단계별로 지시합니다.
    // 그래야 보너스 선택 팝업이 뜬 동안 바를 멈춰둘 수 있습니다.
    public class LevelExpView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Image barTrack;
        [SerializeField]
        private Image barFill;
        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private TextMeshProUGUI expText;

        [Header("Colors")]
        [SerializeField]
        private Color trackColor = new Color(0.176f, 0.176f, 0.204f, 1f);
        [SerializeField]
        private Color fillColor = new Color(0.937f, 0.624f, 0.153f, 1f);
        [SerializeField]
        private Color textColor = Color.white;

        [Header("Animation")]
        [SerializeField]
        [Tooltip("바를 0에서 끝까지 채울 때의 시간(초). 변화량이 적으면 아래 최소 시간 쪽으로 보간됩니다.")]
        private float fillDuration = 0.7f;
        [SerializeField]
        [Tooltip("아주 조금 오를 때도 최소한 이 시간(초)은 움직이게 해서 뚝 끊기지 않게 합니다.")]
        private float minFillDuration = 0.3f;
        [SerializeField]
        private Ease fillEase = Ease.InOutSine;

        [Header("Preview (인스펙터에서 값을 바꾸면 즉시 반영됩니다)")]
        [SerializeField]
        private int level = 3;
        [SerializeField]
        private int currentExp = 7;
        [SerializeField]
        private int requiredExp = 10;

        private Tween _fillTween;

        public void SetLevelText(int value)
        {
            level = value;

            if (levelText != null)
            {
                levelText.text = $"Lv.{value}";
                levelText.color = textColor;
            }
        }

        public void SetExpText(int exp, int required)
        {
            currentExp = exp;
            requiredExp = required;

            if (expText != null)
            {
                expText.text = $"{exp} / {required}";
                expText.color = textColor;
            }
        }

        // 트윈 없이 즉시 반영합니다(리셋 등).
        public void SetImmediate(int newLevel, int exp, int required)
        {
            KillTween();

            SetLevelText(newLevel);
            SetExpText(exp, required);
            ApplyColors();

            if (barFill != null)
                barFill.fillAmount = GetRatio(exp, required);
        }

        // 지정한 비율까지 바를 채웁니다. 끝나면 onComplete가 호출됩니다.
        public void PlayFillTo(float ratio, Action onComplete = null)
        {
            ApplyColors();

            if (barFill == null || !Application.isPlaying)
            {
                if (barFill != null)
                    barFill.fillAmount = Mathf.Clamp01(ratio);

                onComplete?.Invoke();
                return;
            }

            KillTween();

            var target = Mathf.Clamp01(ratio);
            var delta = Mathf.Abs(target - barFill.fillAmount);

            _fillTween = barFill.DOFillAmount(target, GetDuration(delta))
                .SetEase(fillEase)
                .OnComplete(() => onComplete?.Invoke());
        }

        // 바를 끝까지 채운 뒤 0으로 되돌립니다(레벨업 한 단계).
        public void PlayLevelUpStep(Action onComplete = null)
        {
            ApplyColors();

            if (barFill == null || !Application.isPlaying)
            {
                if (barFill != null)
                    barFill.fillAmount = 0f;

                onComplete?.Invoke();
                return;
            }

            KillTween();

            var current = barFill.fillAmount;

            _fillTween = barFill.DOFillAmount(1f, GetDuration(1f - current))
                .SetEase(fillEase)
                .OnComplete(() =>
                {
                    barFill.fillAmount = 0f;
                    onComplete?.Invoke();
                });
        }

        public void StopAnimation()
        {
            KillTween();
        }

        public float GetRatio(int exp, int required)
        {
            return required <= 0 ? 0f : Mathf.Clamp01((float)exp / required);
        }

        private void ApplyColors()
        {
            if (barTrack != null)
                barTrack.color = trackColor;

            if (barFill != null)
                barFill.color = fillColor;
        }

        // 변화량이 작아도 최소 시간은 보장하고, 클수록 fillDuration에 가까워집니다.
        private float GetDuration(float delta)
        {
            return Mathf.Lerp(minFillDuration, fillDuration, Mathf.Clamp01(delta));
        }

        private void KillTween()
        {
            _fillTween?.Kill();
            _fillTween = null;
        }

        private void OnDestroy()
        {
            KillTween();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (requiredExp < 1)
                requiredExp = 1;

            if (level < 1)
                level = 1;

            if (!Application.isPlaying)
                SetImmediate(level, currentExp, requiredExp);
        }
#endif
    }
}
