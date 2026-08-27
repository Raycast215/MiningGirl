using UnityEngine;

namespace Utility
{
    // 노치와 홈 인디케이터를 피해 자식 UI를 안쪽으로 물립니다.
    //
    // 앵커를 Screen.safeArea / Screen 크기로 계산하는데, 이 둘이 항상 같은
    // 프레임에 갱신되지는 않습니다. 게임뷰 해상도를 바꾼 직후나 기기 회전 도중에
    // safeArea는 새 크기, Screen.width/height는 옛 크기를 내는 순간이 있습니다.
    // 그러면 비율이 1을 넘어 앵커가 화면 밖으로 나가고, 그 뒤로는 크기가 다시
    // 안 바뀌니 Refresh가 다시 안 불려 그대로 굳습니다.
    //
    // 실제로 상단 HUD(STAGE / WAVE / 경과시간 / 배속 / 메뉴)가 통째로 화면 위로
    // 사라진 적이 있습니다. 앵커가 (0, 0.02)~(1.90, 1.42)였습니다. 에러는 안 났고
    // 측정용 캡처에서 HUD가 안 찍히는 것으로만 드러났습니다.
    //
    // 그래서 둘을 답니다. 계산 결과를 0~1로 자르고, 크기가 다시 잡히면 다시 겁니다.
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _appliedSafeArea;
        private int _appliedWidth;
        private int _appliedHeight;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            Refresh();
        }

        // 한 프레임 늦게 따라오는 값을 잡습니다.
        //
        // OnRectTransformDimensionsChange만으로는 못 잡습니다. 그 콜백이 불린
        // 시점에 Screen이 아직 옛 값이면 틀린 채로 끝나고, 크기가 더 안 바뀌면
        // 다시 부를 계기가 없습니다.
        private void Update()
        {
            if (Screen.width == _appliedWidth
                && Screen.height == _appliedHeight
                && Screen.safeArea == _appliedSafeArea)
                return;

            Refresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_rectTransform == null)
                return;

            Refresh();
        }

        private void Refresh()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            var width = Screen.width;
            var height = Screen.height;

            // 0으로 나누면 앵커가 NaN이 되고 자식이 통째로 사라집니다.
            if (width <= 0 || height <= 0)
                return;

            var safeArea = Screen.safeArea;

            var min = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            var max = new Vector2(safeArea.xMax / width, safeArea.yMax / height);

            // 화면 밖 앵커는 안전 영역이 아니라 계산이 어긋난 것입니다.
            // 자르면 최악이라도 화면 전체가 되지, HUD가 사라지지는 않습니다.
            min.x = Mathf.Clamp01(min.x);
            min.y = Mathf.Clamp01(min.y);
            max.x = Mathf.Clamp01(max.x);
            max.y = Mathf.Clamp01(max.y);

            if (max.x <= min.x || max.y <= min.y)
                return;

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;

            _appliedSafeArea = safeArea;
            _appliedWidth = width;
            _appliedHeight = height;
        }
    }
}
