using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 레벨과 경험치 진행도를 표시하는 뷰.
    // 값 계산은 하지 않고, 전달받은 값을 그리기만 합니다.
    // 경험치 바는 트윈으로 부드럽게 채워지고, 레벨업 시에는 끝까지 찬 뒤
    // 0으로 돌아가 남은 양을 이어서 채웁니다.
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
        [SerializeField]
        [Tooltip("한 번에 여러 레벨이 올랐을 때 재생할 최대 연출 횟수. 넘는 만큼은 건너뜁니다.")]
        private int maxLevelUpAnimations = 3;

        [Header("Preview (인스펙터에서 값을 바꾸면 즉시 반영됩니다)")]
        [SerializeField]
        private int level = 3;
        [SerializeField]
        private int currentExp = 7;
        [SerializeField]
        private int requiredExp = 10;

        // 화면에 '실제로 표시된' 레벨. 레벨업 연출이 한 바퀴 돌 때마다 올라갑니다.
        // 요청된 level과 분리해야, 같은 프레임에 경험치가 여러 번 들어와도
        // 연출이 취소되지 않고 이어집니다.
        private int _displayedLevel = -1;
        private Tween _fillTween;

        // 런타임에서 값을 갱신할 때 호출합니다.
        // immediate가 true면 트윈 없이 즉시 반영합니다(리셋 등).
        public void SetValue(int newLevel, int exp, int required, bool immediate = false)
        {
            level = newLevel;
            currentExp = exp;
            requiredExp = required;

            Apply(immediate);
        }

        private void Awake()
        {
            Apply(true);
        }

        private void Apply(bool immediate)
        {
            if (barTrack != null)
                barTrack.color = trackColor;

            if (expText != null)
            {
                expText.text = $"{currentExp} / {requiredExp}";
                expText.color = textColor;
            }

            if (levelText != null)
                levelText.color = textColor;

            if (barFill != null)
                barFill.color = fillColor;

            var target = requiredExp <= 0 ? 0f : Mathf.Clamp01((float)currentExp / requiredExp);

            if (immediate || !Application.isPlaying || barFill == null)
            {
                KillTween();

                if (barFill != null)
                    barFill.fillAmount = target;

                SetLevelText(level);
                _displayedLevel = level;
                return;
            }

            KillTween();

            if (_displayedLevel < 0)
                _displayedLevel = level;

            // 표시된 레벨보다 요청된 레벨이 높으면 레벨업 연출을 재생합니다.
            if (level > _displayedLevel)
            {
                var seq = DOTween.Sequence();
                var current = barFill.fillAmount;
                var wrapCount = Mathf.Min(level - _displayedLevel, Mathf.Max(1, maxLevelUpAnimations));

                for (var i = 0; i < wrapCount; i++)
                {
                    var from = i == 0 ? current : 0f;

                    seq.Append(barFill.DOFillAmount(1f, GetDuration(1f - from)).SetEase(fillEase));
                    seq.AppendCallback(() =>
                    {
                        barFill.fillAmount = 0f;
                        _displayedLevel++;
                        SetLevelText(_displayedLevel);
                    });
                }

                // 연출을 건너뛴 레벨이 있으면 마지막에 숫자만 맞춥니다.
                seq.AppendCallback(() =>
                {
                    _displayedLevel = level;
                    SetLevelText(level);
                });

                seq.Append(barFill.DOFillAmount(target, GetDuration(target)).SetEase(fillEase));

                _fillTween = seq;
            }
            else
            {
                SetLevelText(level);
                _displayedLevel = level;

                var delta = Mathf.Abs(target - barFill.fillAmount);
                _fillTween = barFill.DOFillAmount(target, GetDuration(delta)).SetEase(fillEase);
            }
        }

        // 변화량이 작아도 최소 시간은 보장하고, 클수록 fillDuration에 가까워집니다.
        private float GetDuration(float delta)
        {
            return Mathf.Lerp(minFillDuration, fillDuration, Mathf.Clamp01(delta));
        }

        private void SetLevelText(int value)
        {
            if (levelText != null)
                levelText.text = $"Lv.{value}";
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

            Apply(true);
        }
#endif
    }
}
