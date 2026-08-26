using TMPro;
using UnityEngine;

namespace Legacy.MainGame.Entity.Monster
{
    // 몬스터 머리 위에 붙는 체력 표시.
    //
    // UGUI Canvas 대신 SpriteRenderer 두 장으로 만들었습니다.
    // 몬스터는 한 화면에 수십~수백 마리가 깔리는데, 그만큼 월드 캔버스를 두면
    // 캔버스 리빌드 비용이 몬스터 수에 그대로 비례해 붙습니다.
    // 스프라이트는 위치·스케일만 바꾸면 되므로 그 비용이 없습니다.
    public class MonsterHealthBar : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("바탕(닳은 부분)")]
        private SpriteRenderer back;

        [SerializeField]
        [Tooltip("남은 체력만큼 채워지는 부분")]
        private SpriteRenderer fill;

        [SerializeField]
        [Tooltip("현재/최대 체력 숫자")]
        private TMP_Text label;

        [Header("Size")]
        [SerializeField]
        [Tooltip("바 전체 가로 길이(월드 유닛)")]
        private float barWidth = 1.5f;

        [SerializeField]
        [Tooltip("바 두께(월드 유닛). 숫자가 안에 들어가므로 글자보다 넉넉해야 합니다.")]
        private float barHeight = 0.44f;

        [SerializeField]
        [Tooltip("바탕이 채움보다 얼마나 더 큰지(테두리처럼 보이게 합니다)")]
        private float backPadding = 0.06f;

        [Header("Colors")]
        [SerializeField]
        private Color backColor = new Color(0.05f, 0.05f, 0.07f, 1f);

        [SerializeField]
        private Color fillColor = new Color(0.20f, 0.60f, 0.28f, 1f);

        [SerializeField]
        [Tooltip("체력이 이 비율 아래로 내려가면 경고 색으로 바뀝니다")]
        [Range(0f, 1f)]
        private float dangerRatio = 0.3f;

        [SerializeField]
        private Color dangerColor = new Color(0.72f, 0.18f, 0.18f, 1f);

        // 스프라이트 한 장이 월드에서 차지하는 크기.
        // PPU에 따라 달라지므로 값을 박지 않고 실제 스프라이트에서 읽습니다.
        private float _unitX = 1f;
        private float _unitY = 1f;

        private bool _isCached;

        // 같은 값을 다시 넣는 낭비를 막습니다.
        // (몬스터가 많을수록 이 한 줄이 프레임당 호출 수를 크게 줄입니다.)
        private float _lastRatio = -1f;
        private bool _visible = true;

        private void Awake()
        {
            CacheUnitSize();
        }

        private void CacheUnitSize()
        {
            if (_isCached)
                return;

            var sprite = back != null ? back.sprite : (fill != null ? fill.sprite : null);

            if (sprite == null)
                return;

            var size = sprite.bounds.size;

            if (size.x > 0f) _unitX = size.x;
            if (size.y > 0f) _unitY = size.y;

            _isCached = true;

            // 바탕은 한 번만 맞춰 두면 됩니다.
            if (back != null)
            {
                back.color = backColor;

                // 바탕을 살짝 키워 테두리처럼 보이게 합니다.
                // 배경색이 어두운 화면에서는 같은 크기면 채움에 완전히 가려 보이지 않습니다.
                back.transform.localScale = new Vector3(
                    (barWidth + backPadding) / _unitX, (barHeight + backPadding) / _unitY, 1f);
                back.transform.localPosition = Vector3.zero;
            }
        }

        // 스폰·피격마다 호출됩니다.
        public void SetValue(float current, float max)
        {
            CacheUnitSize();

            var ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);

            if (label != null)
                label.text = $"{Mathf.Max(0, Mathf.CeilToInt(current))}/{Mathf.Max(0, Mathf.CeilToInt(max))}";

            if (Mathf.Approximately(ratio, _lastRatio))
                return;

            _lastRatio = ratio;

            if (fill == null)
                return;

            fill.color = dangerRatio > 0f && ratio <= dangerRatio ? dangerColor : fillColor;

            // 가운데 피벗 스프라이트를 왼쪽 기준으로 줄이려면
            // 폭을 줄인 만큼 중심도 왼쪽으로 옮겨줘야 합니다.
            fill.transform.localScale = new Vector3(barWidth * ratio / _unitX, barHeight / _unitY, 1f);
            fill.transform.localPosition = new Vector3(-barWidth * 0.5f + barWidth * ratio * 0.5f, 0f, 0f);
        }

        // 화면 밖 몬스터는 본체와 함께 꺼집니다.
        public void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;

            _visible = visible;

            if (back != null) back.enabled = visible;
            if (fill != null) fill.enabled = visible;
            if (label != null) label.enabled = visible;
        }
    }
}
