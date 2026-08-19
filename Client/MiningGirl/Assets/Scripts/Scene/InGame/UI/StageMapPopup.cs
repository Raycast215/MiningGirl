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
        public async UniTaskVoid PlayAsync(int fromStage, int toStage, int maxStage,
            Func<int, bool> isCardStage, Action onComplete)
        {
            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = $"스테이지 {toStage}";

            EnsureNodes(maxStage);

            // 먼저 이전 상태로 그립니다. 그래야 칸이 옮겨가는 것이 보입니다.
            Draw(fromStage, maxStage, isCardStage);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeInDuration);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(fadeInDuration + holdDuration),
                ignoreTimeScale: true);

            // 다음 칸으로 이동
            Draw(toStage, maxStage, isCardStage);

            await UniTask.Delay(TimeSpan.FromSeconds(showDuration), ignoreTimeScale: true);

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, fadeOutDuration);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(fadeOutDuration), ignoreTimeScale: true);

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
