using Scene.StartScene.ViewModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.StartScene.UI
{
    // 스테이지 선택 화면(임시).
    //
    // 프리팹이나 씬 오브젝트를 만들지 않고 코드로 세웁니다. 다음 작업에서 제대로
    // 만들 화면이라, 지금 씬에 배선을 남기면 그때 걷어내는 일이 더 큽니다.
    // 정식판이 들어오면 이 파일만 지우면 됩니다.
    public class StageSelectUI : MonoBehaviour
    {
        // 목록 위아래로 비워 두는 여백. 제목과 화면 끝에 붙지 않게 합니다.
        private const float TopInset = 280f;
        private const float BottomInset = 200f;

        private const float ListWidth = 900f;
        private const float RowGap = 24f;
        private const float RowHeightMax = 200f;

        // 이보다 작아지면 배치가 잘못된 것으로 봅니다.
        private const float RowHeightMin = 60f;

        // 뒷판은 반투명이 아니라 불투명입니다.
        //
        // 알파 0.92로 깔아 봤는데 뒤의 남색이 그대로 비쳤습니다. 이 캔버스에서
        // 어두운 색의 알파 블렌딩이 예상대로 안 섞입니다 - 같은 자리에 빨강을
        // 알파 1로 깔면 덮이므로 그리기 자체는 됩니다. 임시 화면에서 그 원인을
        // 파는 것보다, 결과가 확정적인 불투명으로 두는 쪽이 낫습니다.
        private static readonly Color DimColor = new Color32(14, 14, 20, 255);
        private static readonly Color RowColor = new Color32(46, 42, 58, 255);
        private static readonly Color OrderColor = new Color32(226, 180, 96, 255);
        private static readonly Color NameColor = new Color32(255, 248, 232, 255);
        private static readonly Color DetailColor = new Color32(158, 150, 170, 255);

        // 한글 글리프가 있는지 확인할 때 쓰는 글자.
        private const char KoreanProbe = '가';

        private StageSelectViewModel _viewModel;

        public static StageSelectUI Create(Canvas canvas, StageSelectViewModel viewModel)
        {
            if (canvas == null || viewModel == null)
                return null;

            var root = new GameObject("StageSelect", typeof(RectTransform), typeof(Image), typeof(StageSelectUI));
            var rect = (RectTransform)root.transform;

            rect.SetParent(canvas.transform, false);
            Stretch(rect);

            // 뒷판은 어둡게 덮고 입력도 막습니다. 뒤에 있는 시작 버튼이 눌리면
            // 스테이지를 고르지 않은 채로 들어갑니다.
            var dim = root.GetComponent<Image>();
            dim.color = DimColor;
            dim.raycastTarget = true;

            // 마지막에 그려야 로딩 UI 위로 올라옵니다.
            rect.SetAsLastSibling();

            var ui = root.GetComponent<StageSelectUI>();
            ui.Build(rect, viewModel);

            return ui;
        }

        private void Build(RectTransform root, StageSelectViewModel viewModel)
        {
            _viewModel = viewModel;

            var font = FindKoreanFont();

            var title = CreateText(root, "Title", font, 60f, NameColor, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -150f);
            title.rectTransform.sizeDelta = new Vector2(ListWidth, 80f);
            title.text = "스테이지 선택";

            var list = new GameObject("List", typeof(RectTransform));
            var listRect = (RectTransform)list.transform;

            listRect.SetParent(root, false);
            listRect.anchorMin = new Vector2(0.5f, 0f);
            listRect.anchorMax = new Vector2(0.5f, 1f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.offsetMin = new Vector2(-ListWidth * 0.5f, BottomInset);
            listRect.offsetMax = new Vector2(ListWidth * 0.5f, -TopInset);

            // 자동 레이아웃 컴포넌트를 쓰지 않고 직접 배치합니다. 스테이지 수가
            // 고정이라 한 번 계산하면 끝이고, 이 임시 화면에는 과합니다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);

            var items = viewModel.Items;
            var count = items.Count;

            if (count == 0)
                return;

            var available = listRect.rect.height;
            var rowHeight = Mathf.Min(RowHeightMax, (available - RowGap * (count - 1)) / count);

            // 캔버스를 잘못 잡으면 여기가 음수가 되고, 글자 크기까지 음수로 내려가
            // 아무것도 안 보이는 채로 에러도 안 납니다. 눈에 띄게 막아 둡니다.
            if (rowHeight < RowHeightMin)
            {
                Debug.LogWarning($"[StageSelect] 목록에 쓸 세로가 모자랍니다: {available:0}. 캔버스를 확인하십시오.");

                rowHeight = RowHeightMin;
            }

            var step = rowHeight + RowGap;

            // 목록을 세로 가운데에 모읍니다. 스테이지가 늘어도 위에서부터 쌓입니다.
            var top = (count * rowHeight + RowGap * (count - 1)) * 0.5f;

            for (var i = 0; i < count; i++)
                CreateRow(listRect, font, items[i], i, rowHeight, top - step * i);
        }

        private void CreateRow(RectTransform parent, TMP_FontAsset font, StageSelectItem item, int index, float height, float top)
        {
            var row = new GameObject("Stage" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)row.transform;

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, top);

            row.GetComponent<Image>().color = RowColor;

            var captured = index;
            row.GetComponent<Button>().onClick.AddListener(() => _viewModel.Select(captured));

            var order = CreateText(rect, "Order", font, height * 0.42f, OrderColor, TextAlignmentOptions.Center);
            order.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            order.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            order.rectTransform.anchoredPosition = new Vector2(90f, 0f);
            order.rectTransform.sizeDelta = new Vector2(120f, height);
            order.text = item.Order;

            var name = CreateText(rect, "Name", font, height * 0.3f, NameColor, TextAlignmentOptions.MidlineLeft);
            name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            name.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.rectTransform.anchoredPosition = new Vector2(180f, height * 0.16f);
            name.rectTransform.sizeDelta = new Vector2(ListWidth - 220f, height * 0.4f);
            name.text = item.Name;

            var detail = CreateText(rect, "Detail", font, height * 0.2f, DetailColor, TextAlignmentOptions.MidlineLeft);
            detail.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            detail.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            detail.rectTransform.pivot = new Vector2(0f, 0.5f);
            detail.rectTransform.anchoredPosition = new Vector2(180f, -height * 0.18f);
            detail.rectTransform.sizeDelta = new Vector2(ListWidth - 220f, height * 0.32f);
            detail.text = item.Detail;
        }

        private static TMP_Text CreateText(RectTransform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var text = go.AddComponent<TextMeshProUGUI>();

            go.transform.SetParent(parent, false);

            if (font != null)
                text.font = font;

            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;

            return text;
        }

        // 씬에 이미 떠 있는 글자에서 폰트를 빌립니다.
        //
        // TMP 기본 폰트는 한글 글리프가 없어 스테이지 이름이 네모로 나옵니다.
        // 이 화면은 임시라 폰트를 어드레서블에 새로 올리거나 직렬화 배선을
        // 만들지 않고, 같은 씬에서 한글을 이미 그리고 있는 것을 그대로 씁니다.
        private static TMP_FontAsset FindKoreanFont()
        {
            var texts = FindObjectsOfType<TMP_Text>(true);

            for (var i = 0; i < texts.Length; i++)
            {
                var font = texts[i].font;

                if (font == null)
                    continue;

                if (HasKorean(font))
                    return font;

                // 폴백에 한글이 있으면 본체를 그대로 씁니다 - TMP가 알아서 넘깁니다.
                var fallbacks = font.fallbackFontAssetTable;

                if (fallbacks == null)
                    continue;

                for (var k = 0; k < fallbacks.Count; k++)
                {
                    if (HasKorean(fallbacks[k]))
                        return font;
                }
            }

            Debug.LogWarning("[StageSelect] 한글 폰트를 찾지 못했습니다. 이름이 네모로 나올 수 있습니다.");

            return null;
        }

        private static bool HasKorean(TMP_FontAsset font)
        {
            return font != null
                && font.characterLookupTable != null
                && font.characterLookupTable.ContainsKey(KoreanProbe);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void Close()
        {
            _viewModel = null;

            Destroy(gameObject);
        }
    }
}
