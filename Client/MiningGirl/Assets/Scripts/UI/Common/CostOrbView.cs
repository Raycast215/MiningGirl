using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 코스트 표시 오브.
    // 링은 다음 1 코스트가 채워지기까지의 진행도를 나타내고(초당 1 회복 = 링 한 바퀴 1초),
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
        public void SetValue(int currentCost, float progress, int max)
        {
            cost = currentCost;
            chargeProgress = progress;
            maxCost = max;

            Apply();
        }

        private void Awake()
        {
            Apply();
        }

        private void Apply()
        {
            var isOvercharged = cost > maxCost;
            var mainColor = isOvercharged ? overchargeColor : fillColor;

            if (ringTrack != null)
                ringTrack.color = trackColor;

            if (ringFill != null)
            {
                ringFill.color = mainColor;

                // 최대치 이상이면 링을 꽉 채우고, 그 아래면 다음 1까지의 진행도를 표시합니다.
                ringFill.fillAmount = cost >= maxCost ? 1f : Mathf.Clamp01(chargeProgress);
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
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxCost < 1)
                maxCost = 1;

            Apply();
        }
#endif
    }
}
