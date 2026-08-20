using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Scene.InGame.UI
{
    // 손패 바로 위에서 카드 사용 실패 사유를 알려주는 공통 문구.
    //
    // 예전에는 카드마다 FailText를 하나씩 들고 있었는데 문제가 셋 있었습니다.
    //  - 카드 안에 갇혀서 작고, 카드가 흔들리는 동안 같이 흔들려 읽기 어려웠습니다.
    //  - 카드 3장이 각자 다른 말을 동시에 띄울 수 있었습니다.
    //  - 조준 중에는 카드가 흐려져서 문구까지 같이 흐려졌습니다.
    // 손패 위 한 자리에서만 말하도록 모았습니다.
    public class CardMessageUI : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text label;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField]
        [Tooltip("뜨는 시간(초)")]
        private float fadeInDuration = 0.08f;

        [SerializeField]
        [Tooltip("머무는 시간(초)")]
        private float holdDuration = 0.7f;

        [SerializeField]
        [Tooltip("사라지는 시간(초)")]
        private float fadeOutDuration = 0.35f;

        [SerializeField]
        [Tooltip("사라지면서 떠오르는 높이. 0이면 제자리에서 사라집니다.")]
        private float riseDistance = 24f;

        private RectTransform _rect;
        private Vector2 _homePosition;
        private Sequence _sequence;
        private bool _isInitialized;

                // 별도 연결 없이 스스로 준비합니다.
        // (초기화를 빼먹으면 시작부터 문구가 띄어 있게 됩니다.)
        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            if (_isInitialized)
                return;

            _rect = (RectTransform)transform;
            _homePosition = _rect.anchoredPosition;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _isInitialized = true;

            Hide();
        }

        // 같은 말을 연달아 띄워도 처음부터 다시 보여줍니다.
        // (코스트가 모자란 카드를 여러 번 시도했을 때 반응이 없으면 먹통처럼 느껴집니다.)
        public void Show(string message)
        {
            if (!_isInitialized)
                Init();

            if (string.IsNullOrEmpty(message))
            {
                Hide();

                return;
            }

            if (label != null)
                label.text = message;

            KillSequence();

            if (canvasGroup == null)
                return;

            _rect.anchoredPosition = _homePosition;
            canvasGroup.alpha = 0f;

            _sequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, fadeInDuration))
                .AppendInterval(holdDuration)
                .Append(canvasGroup.DOFade(0f, fadeOutDuration))
                .SetUpdate(true);

            if (riseDistance > 0f)
            {
                _sequence.Join(_rect.DOAnchorPosY(_homePosition.y + riseDistance, fadeOutDuration)
                    .SetUpdate(true));
            }
        }

        public void Hide()
        {
            KillSequence();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (_rect != null)
                _rect.anchoredPosition = _homePosition;
        }

        private void KillSequence()
        {
            if (_sequence == null)
                return;

            _sequence.Kill();
            _sequence = null;
        }

        private void OnDestroy()
        {
            KillSequence();
        }
    }
}
