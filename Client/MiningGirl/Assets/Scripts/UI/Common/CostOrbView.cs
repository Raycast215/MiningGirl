using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 코스트 표시 오브.
    // 링은 다음 1 코스트가 채워지기까지의 진행도를 나타내고(링 10칸 = 코스트 10),
    // 최대치를 초과하면(보스전 오버차지) 색과 외곽선으로 구분됩니다.
    public class CostOrbView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Image ringTrack;
        [SerializeField]
        private Image ringFill;
        [SerializeField]
        private Image overchargeOutline;

        [Header("Ring Sprites")]
        [SerializeField]
        [Tooltip("칸마다 색이 밝아지는 기본 링")]
        private Sprite ringFillGradientSprite;
        [SerializeField]
        [Tooltip("최대치일 때 쓰는 링 — 전 칸이 마지막 밝기 색")]
        private Sprite ringFillFullSprite;
        [SerializeField]
        private TextMeshProUGUI costText;

        [Header("Colors")]
        [SerializeField]
        private Color trackColor = new Color(0.706f, 0.698f, 0.663f, 1f);
        [SerializeField]
        private Color fillColor = new Color(0.114f, 0.620f, 0.459f, 1f);
        [SerializeField]
        private Color textColor = Color.white;
        [SerializeField]
        private Color overchargeColor = new Color(0.937f, 0.624f, 0.153f, 1f);
        [SerializeField]
        private Color overchargeTextColor = new Color(0.937f, 0.624f, 0.153f, 1f);

        [Header("Animation")]
        [SerializeField]
        [Tooltip("코스트 숫자가 바뀔 때 튀어오르는 크기")]
        private float punchScale = 0.22f;
        [SerializeField]
        private float punchDuration = 0.3f;
        [SerializeField]
        [Tooltip("진동 횟수. 1이면 한 번 두둥하고 정리됩니다.")]
        private int punchVibrato = 1;
        [SerializeField]
        [Range(0f, 1f)]
        private float punchElasticity = 0.6f;
        [SerializeField]
        [Tooltip("비우면 오브 전체가 튑니다. 숫자만 튀게 하려면 CostText를 넣으세요.")]
        private Transform punchTarget;

        [Header("Preview (인스펙터에서 값을 바꾸면 즉시 반영됩니다)")]
        [SerializeField]
        private int cost = 6;
        [SerializeField]
        [Range(0f, 1f)]
        private float chargeProgress = 0.35f;
        [SerializeField]
        private int maxCost = 10;

        // 런타임에서 코스트 값을 갱신할 때 호출합니다.
        // currentCost: 현재 보유 코스트 / progress: 다음 1까지의 진행도(0~1) / max: 최대치
        // 숫자가 실제로 바뀐 순간에만 연출을 재생하기 위한 이전 값입니다.
        private int _shownCost = int.MinValue;
        private Tween _punchTween;

        // immediate가 true면 연출 없이 값만 반영합니다(리셋 등).
        public void SetValue(int currentCost, float progress, int max, bool immediate = false)
        {
            cost = currentCost;
            chargeProgress = progress;
            maxCost = max;

            Apply(immediate);
        }

        private void Awake()
        {
            Apply(true);
        }

        private void Apply(bool immediate = false)
        {
            // 숫자가 실제로 바뀐 순간에만 두둥 연출을 재생합니다.
            // (회복 진행도 때문에 Apply는 매 프레임 호출되므로 값 비교가 필요합니다.)
            var costChanged = !immediate && _shownCost != int.MinValue && _shownCost != cost;
            _shownCost = cost;

            var isOvercharged = cost > maxCost;
            var mainColor = isOvercharged ? overchargeColor : fillColor;

            if (ringTrack != null)
                ringTrack.color = trackColor;

            if (ringFill != null)
            {
                ringFill.color = mainColor;

                // 링은 10칸으로 나뉜 코스트 게이지입니다.
                // 보유 코스트만큼 칸이 차고, 회복 중인 다음 한 칸이 점점 채워집니다.
                // (최대치 이상이면 꽉 찬 상태로 둡니다.)
                var isFull = cost >= maxCost;

                // 최대치에 도달하면 그라데이션 대신 '가장 밝은 색으로 꽉 찬' 링으로 바꿉니다.
                var targetSprite = isFull ? ringFillFullSprite : ringFillGradientSprite;
                if (targetSprite != null && ringFill.sprite != targetSprite)
                    ringFill.sprite = targetSprite;

                if (isFull)
                {
                    ringFill.fillAmount = 1f;
                }
                else
                {
                    var perSegment = maxCost <= 0 ? 0f : 1f / maxCost;
                    ringFill.fillAmount = Mathf.Clamp01(cost * perSegment + Mathf.Clamp01(chargeProgress) * perSegment);
                }
            }

            if (overchargeOutline != null)
            {
                overchargeOutline.color = overchargeColor;
                overchargeOutline.gameObject.SetActive(isOvercharged);
            }

            if (costText != null)
            {
                costText.text = cost.ToString();
                costText.color = isOvercharged ? overchargeTextColor : textColor;
            }

            if (costChanged)
                PlayPunch();
        }

        // 코스트 숫자가 바뀔 때 살짝 튀어오르는 연출입니다.
        private void PlayPunch()
        {
            if (!Application.isPlaying)
                return;

            var target = punchTarget != null ? punchTarget : transform;

            // 이전 연출이 남아 있으면 스케일이 누적되므로 정리하고 원래 크기로 되돌립니다.
            _punchTween?.Kill();
            target.localScale = Vector3.one;

            _punchTween = target.DOPunchScale(Vector3.one * punchScale, punchDuration, punchVibrato, punchElasticity)
                .SetEase(Ease.OutQuad);
        }

        private void OnDestroy()
        {
            _punchTween?.Kill();
            _punchTween = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxCost < 1)
                maxCost = 1;

            Apply(true);
        }
#endif
    }
}
