using UnityEngine;

namespace UI.Common
{
    // 자기 크기를 고정해두고, 부모(레이아웃 셀) 크기에 맞춰 스케일만 줄입니다.
    //
    // 카드처럼 안쪽 요소가 원본 비율대로 짜인 UI에 씁니다.
    // RectTransform을 직접 늘리면 글자 크기와 여백이 원본과 어긋나는데,
    // 스케일로 줄이면 카드가 통째로 축소돼 손패에서 보던 모습 그대로 유지됩니다.
    [ExecuteAlways]
    public class ScaleToFitParent : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("원본 크기. 이 크기를 유지한 채 스케일만 조절합니다.")]
        private Vector2 designSize = new Vector2(400f, 550f);

        [SerializeField]
        [Tooltip("부모 크기에서 뺄 여백")]
        private Vector2 padding;

        [SerializeField]
        [Tooltip("자기 영역 밖으로 삐져나온 자식의 크기(카드 아래 띠 등). 스케일 계산에만 더합니다.")]
        private Vector2 overflow;

        [SerializeField]
        [Tooltip("이 값보다 크게는 키우지 않습니다")]
        private float maxScale = 1f;

        [SerializeField]
        [Tooltip("바깥에서 곱할 수 있는 최대 배율(선택 강조 등). 미리 자리를 비워둡니다.")]
        private float maxExtraScale = 1f;

        // 선택 강조처럼 바깥에서 곱하고 싶은 배율.
        // 이 컴포넌트가 매 프레임 스케일을 정하므로, 직접 localScale을 만지면 덮어써집니다.
        private float _extraScale = 1f;

        public void SetExtraScale(float value)
        {
            _extraScale = value <= 0f ? 1f : value;

            Apply();
        }

        private RectTransform _rect;
        private RectTransform _parent;

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (Rect == null)
                return;

            _parent = Rect.parent as RectTransform;

            if (_parent == null || designSize.x <= 0f || designSize.y <= 0f)
                return;

            // 크기는 원본 그대로 두고
            if (Rect.sizeDelta != designSize)
                Rect.sizeDelta = designSize;

            var available = new Vector2(
                Mathf.Max(1f, _parent.rect.width - padding.x),
                Mathf.Max(1f, _parent.rect.height - padding.y));

            // 가로세로 중 더 빡빡한 쪽에 맞춥니다.
            // 삐져나온 자식까지 포함해 부모 안에 들어가도록 계산합니다.
            // (크기 자체는 designSize로 두어야 안쪽 요소 비율이 안 깨집니다.)
            var fitSize = designSize + overflow;

            var scale = Mathf.Min(available.x / fitSize.x, available.y / fitSize.y);

            scale = Mathf.Min(scale, maxScale);

            // 선택 강조로 커지는 몫까지 미리 빼둡니다.
            // 나중에 곱하기만 하면 그만큼 셀 밖으로 삐져나갑니다.
            if (maxExtraScale > 1f)
                scale /= maxExtraScale;

            var target = Vector3.one * scale * _extraScale;

            if (Rect.localScale != target)
                Rect.localScale = target;
        }
    }
}
