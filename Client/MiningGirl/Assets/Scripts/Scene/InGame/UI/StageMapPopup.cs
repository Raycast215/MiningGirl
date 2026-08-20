using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MainGame.UI;
using TMPro;
using UnityEngine;

namespace Scene.InGame.UI
{
    // 스테이지 사이에 잠깐 지나가는 맵 연출.
    //
    // 버튼이 없습니다. 10번 넘게 보게 될 화면이라 누르게 하면 번거롭습니다.
    // 맵이 뜨고 → 다음 칸으로 옮겨가고 → 자동으로 사라집니다.
    public class StageMapPopup : MonoBehaviour
    {
        [SerializeField]
        private StageMapNodeView nodePrefab;

        [SerializeField]
        private RectTransform nodeRoot;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [Header("Timing")]
        [SerializeField]
        [Tooltip("맵이 나타나는 시간(초)")]
        private float fadeInDuration = 0.3f;

        [SerializeField]
        [Tooltip("다음 칸으로 옮겨가기 전 잠깐 멈추는 시간(초)")]
        private float holdDuration = 0.4f;

        [SerializeField]
        [Tooltip("칸이 바뀐 뒤 머무는 시간(초)")]
        private float showDuration = 0.5f;

        [SerializeField]
        [Tooltip("사라지는 시간(초)")]
        private float fadeOutDuration = 0.3f;

        private readonly List<StageMapNodeView> _nodes = new List<StageMapNodeView>();

        public void Init()
        {
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // fromStage에서 toStage로 넘어가는 연출을 재생하고 끝나면 onComplete를 부릅니다.
        // isCardStage: 해당 스테이지가 카드 정리 스테이지인지 판단하는 함수
        // fromStage에서 toStage로 넘어가는 연출을 재생하고 끝나면 onComplete를 부릅니다.
        // isCardStage: 해당 스테이지가 카드 정리 스테이지인지 판단하는 함수
        // onShown: 맵이 화면을 다 덮은 직후에 불립니다. 이 시점에는 뒤가 보이지 않으므로
        //          화면 덮개를 걷는 등 들키고 싶지 않은 정리를 여기서 합니다.
        // showInstantly: 페이드 없이 그 프레임에 바로 화면을 덮습니다.
        //          캐릭터 선택 팝업이 닫히는 순간처럼, 뒤에 있는 인게임 화면이
        //          한 순간도 드러나면 안 되는 경우에 씁니다.
        public async UniTask PlayAsync(int fromStage, int toStage, int maxStage,
            Func<int, bool> isCardStage, Action onComplete, Action onShown = null,
            bool showInstantly = false, Func<UniTask> onBeforeHide = null)
        {
            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = $"스테이지 {toStage}";

            EnsureNodes(maxStage);

            // 먼저 이전 상태로 그립니다. 그래야 칸이 옮겨가는 것이 보입니다.
            Draw(fromStage, maxStage, isCardStage);

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();

                if (showInstantly)
                {
                    canvasGroup.alpha = 1f;
                }
                else
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1f, fadeInDuration);
                }
            }

            if (!showInstantly)
                await UniTask.Delay(TimeSpan.FromSeconds(fadeInDuration), ignoreTimeScale: true);

            // 여기서부턴 맵이 화면을 다 가리고 있습니다.
            onShown?.Invoke();

            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), ignoreTimeScale: true);

            // 다음 칸으로 이동
            Draw(toStage, maxStage, isCardStage);

            await UniTask.Delay(TimeSpan.FromSeconds(showDuration), ignoreTimeScale: true);

            // 아직 맵이 화면을 덮고 있는 동안 처리할 일을 기다립니다(예: 카드 정리).
            // 맵을 먼저 걷어버리면 다음 화면이 뜨기 전까지 인게임이 잠깐 드러납니다.
            if (onBeforeHide != null)
                await onBeforeHide.Invoke();

            // onBeforeHide 안에서 이미 내려갔다면(예: 카드 정리 화면이 대신 덮음)
            // 사라지는 연출을 다시 할 필요가 없습니다.
            if (gameObject.activeSelf)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.DOKill();
                    canvasGroup.DOFade(0f, fadeOutDuration);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(fadeOutDuration), ignoreTimeScale: true);
            }

            Hide();

            onComplete?.Invoke();
        }

        private void Draw(int currentStage, int maxStage, Func<int, bool> isCardStage)
        {
            for (var i = 0; i < _nodes.Count; i++)
            {
                var stage = i + 1;

                if (stage > maxStage)
                {
                    _nodes[i].gameObject.SetActive(false);
                    continue;
                }

                _nodes[i].gameObject.SetActive(true);

                var state = stage < currentStage ? StageMapNodeView.EState.Cleared
                    : stage == currentStage ? StageMapNodeView.EState.Current
                    : StageMapNodeView.EState.Locked;

                _nodes[i].SetData(stage, state, isCardStage != null && isCardStage.Invoke(stage));
            }
        }

        private void EnsureNodes(int count)
        {
            if (nodePrefab == null || nodeRoot == null)
                return;

            while (_nodes.Count < count)
                _nodes.Add(Instantiate(nodePrefab, nodeRoot));
        }
    }
}
