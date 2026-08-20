using System;
using System.Text.RegularExpressions;
using Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 카드 정리 화면의 카드 한 장.
    //
    // 인게임 손패 카드(SkillCardElementUI)와 같은 겉모습을 씁니다.
    // 같은 카드가 화면마다 다르게 보이면 무엇인지 알아보기 어렵기 때문입니다.
    public class CardCleanupItemView : MonoBehaviour
    {
        // 설명 안의 숫자를 노란색으로 칠합니다.
        private const string NumberColor = "#F2A426";

        private static readonly Regex NumberPattern = new Regex(@"\d+(\.\d+)?%?");

        [SerializeField]
        private Button selectButton;

        [SerializeField]
        [Tooltip("카드 테두리(OutLine). 카테고리별 프레임이 들어갑니다")]
        private Image outline;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI typeText;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        private TextMeshProUGUI descText;

        [SerializeField]
        [Tooltip("카드 아래에 표시할 보유 수량. 새 카드면 대신 NEW가 나옵니다")]
        private TextMeshProUGUI countText;

        [Header("Symbols")]
        [SerializeField]
        [Tooltip("Attack / Assist / Support 순서. 해당하는 것만 켜집니다")]
        private GameObject[] categorySymbols;

        [Header("Marks")]
        [SerializeField]
        [Tooltip("이번에 처음 얻은 카드에만 켜집니다")]
        private GameObject newBadge;

        [SerializeField]
        [Tooltip("버릴 카드로 골랐을 때 카드 위에 뜨는 표시. 카드 밖에 두어 회전 영향을 받지 않습니다.")]
        private GameObject discardMark;

        [Header("Frames")]
        [SerializeField]
        [Tooltip("Attack / Assist / Support 순서")]
        private Sprite[] categoryFrames;

        [Header("Select")]
        [SerializeField]
        [Tooltip("확대·기울기를 적용할 카드 본체(Contents)")]
        private RectTransform cardRoot;

        [SerializeField]
        [Tooltip("버릴 카드로 고르면 이만큼 커집니다")]
        private float discardScale = 1.12f;

        [SerializeField]
        [Tooltip("버릴 카드로 고르면 이만큼 기울어집니다(도)")]
        private float discardTilt = 3f;

        [SerializeField]
        [Tooltip("선택 표시가 바뀌는 시간(초). 0이면 즉시 바뀝니다.")]
        private float selectTweenDuration = 0.4f;

        private Action _onClick;
        private global::UI.Common.ScaleToFitParent _fitter;
        private CanvasGroup _canvasGroup;

        // 드로우 연출이 끝난 뒤 돌아올 자리. 연출 도중 화면이 닫히면
        // 어긋난 위치로 남기 때문에 다음에 열 때 여기로 되돌립니다.
        private Vector2 _cardRootHomePos;

        private void Awake()
        {
            if (cardRoot != null)
            {
                _fitter = cardRoot.GetComponent<global::UI.Common.ScaleToFitParent>();
                _canvasGroup = cardRoot.GetComponent<CanvasGroup>();

                if (_canvasGroup == null)
                    _canvasGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();

                _cardRootHomePos = cardRoot.anchoredPosition;

                // 손패 카드(SkillCardElementUI)는 조준 중에도 이름·코스트를 진하게
                // 남기려고 그 자식 CanvasGroup에 Ignore Parent Groups가 켜져 있습니다.
                // 정리 화면은 같은 프리팹을 쓰지만 그 규칙이 필요 없고,
                // 그대로 두면 카드를 감춰도 코스트 뱃지와 이름만 공중에 남습니다.
                var groups = cardRoot.GetComponentsInChildren<CanvasGroup>(true);

                for (var i = 0; i < groups.Length; i++)
                {
                    if (groups[i] == null || groups[i] == _canvasGroup)
                        continue;

                    groups[i].ignoreParentGroups = false;
                }
            }

            if (selectButton == null)
                return;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onClick?.Invoke());
        }

        // animate: 선택 표시(확대·기울기)를 트윈으로 바꿀지 여부.
        // 화면을 처음 채울 때는 false로 넣어 즉시 반영합니다.
        // 카드를 재사용하기 때문에, 트윈을 쓰면 지난번에 버리기로 골랐던 칸이
        // 기울어진 채 떠서 천천히 바로서는 것이 보입니다.
        public void SetData(SkillCardDataTableRow row, bool isNew, bool isDiscard, int deckCount, Action onClick,
            bool animate = true)
        {
            _onClick = onClick;
            // 지난번 드로우 연출이 중간에 끊겼으면 투명하거나 어긋난 자리로 남아 있습니다.
            // 카드를 다시 채울 때마다 보이는 상태로 되돌립니다.
            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.alpha = 1f;
            }

            if (cardRoot != null)
                cardRoot.anchoredPosition = _cardRootHomePos;


            if (row == null)
                return;

            if (nameText != null)
                nameText.text = row.Name;

            if (costText != null)
                costText.text = row.Cost.ToString();

            if (typeText != null)
                typeText.text = GetCategoryName(row.SkillCategoryType);

            if (descText != null)
                descText.text = BuildDesc(row);

            SetSymbol(row.SkillCategoryType);
            SetIcon(row.AssetId);

            // NEW는 이번에 처음 얻은 카드에만, 수량은 나머지에만 보여줍니다.
            if (newBadge != null)
                newBadge.SetActive(isNew);

            if (countText != null)
            {
                countText.gameObject.SetActive(!isNew);
                countText.text = $"{deckCount}장 보유";
            }

            if (discardMark != null)
                discardMark.SetActive(isDiscard);

            if (outline != null)
            {
                var sprite = GetCategoryFrame(row.SkillCategoryType);

                if (sprite != null)
                    outline.sprite = sprite;
            }

            // 버릴 카드는 살짝 키우고 기울여 구분합니다.
            // 카드 본체만 변형합니다. 루트째 키우면 아래 수량 줄까지 움직여
            // 다음 줄 카드와 겹칩니다.
            var target = cardRoot != null ? cardRoot : (RectTransform)transform;
            var scale = isDiscard ? discardScale : 1f;

            // 크기는 ScaleToFitParent가 정하므로 곱할 배율만 넘깁니다.
            if (_fitter != null)
                _fitter.SetExtraScale(scale);
            else
                target.localScale = Vector3.one * scale;

            var angle = isDiscard ? discardTilt : 0f;

            target.DOKill();

            if (animate && selectTweenDuration > 0f && gameObject.activeInHierarchy)
                target.DOLocalRotate(new Vector3(0f, 0f, angle), selectTweenDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            else
                target.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // 드로우 연출 준비 — 감춰둡니다.
        public void PrepareDraw()
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
        }

        // 새로 얻은 카드가 나타나는 연출.
        public void PlayDraw(float duration)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, duration).SetUpdate(true);

            if (cardRoot == null)
                return;

            // 살짝 아래에서 올라오게 합니다.
            var pos = cardRoot.anchoredPosition;

            cardRoot.DOKill();
            cardRoot.anchoredPosition = pos + new Vector2(0f, -60f);
            cardRoot.DOAnchorPos(pos, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void SetSymbol(ESkillCategoryType type)
        {
            if (categorySymbols == null)
                return;

            for (var i = 0; i < categorySymbols.Length; i++)
            {
                if (categorySymbols[i] == null)
                    continue;

                categorySymbols[i].SetActive(i == (int)type);
            }
        }

        private Sprite GetCategoryFrame(ESkillCategoryType type)
        {
            if (categoryFrames == null)
                return null;

            var index = (int)type;

            return index >= 0 && index < categoryFrames.Length ? categoryFrames[index] : null;
        }

        private static string GetCategoryName(ESkillCategoryType type)
        {
            return type switch
            {
                ESkillCategoryType.Attack => "공격",
                ESkillCategoryType.Assist => "보조",
                _ => "서포트",
            };
        }

        // 설명은 '{0} 데미지로 공격' 같은 서식이라 실제 값을 채우고,
        // 숫자만 노란색으로 칠해 눈에 들어오게 합니다.
        private static string BuildDesc(SkillCardDataTableRow row)
        {
            if (string.IsNullOrEmpty(row.Desc))
                return string.Empty;

            string text;

            try
            {
                text = string.Format(row.Desc, row.EffectValue, row.DurationTime, row.EffectRange);
            }
            catch
            {
                text = row.Desc;
            }

            return NumberPattern.Replace(text, m => $"<color={NumberColor}>{m.Value}</color>");
        }

        private void SetIcon(string assetId)
        {
            // 아이콘은 어드레서블에 있습니다. 캐시에 있으면 즉시, 없으면 불러온 뒤 들어갑니다.
            Manager.AddressableManager.Instance?.ApplySprite(assetId, iconImage);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
