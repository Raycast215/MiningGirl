using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 맞은 자리에서 떠올랐다 사라지는 피해 숫자 하나.
    //
    // 레거시(Legacy/InGame/FloatingDamage/Damage.cs)에서 가져왔습니다. 움직임과 시간은
    // 그대로 두고, 정지 처리만 덜어냈습니다. 레거시는 게임이 자체 정지 플래그로 멈춰서
    // DOTween을 직접 세워야 했지만, 이 씬은 3택에서 timeScale을 0으로 만듭니다.
    // DOTween 기본 갱신은 스케일된 시간을 쓰므로 같이 멈춥니다.
    public class FloatingDamageText : MonoBehaviour
    {
        // 시작 높이와 도착 높이. 맞은 지점 바로 위에서 떠오릅니다.
        private const float StartOffsetY = 0.2f;
        private const float EndOffsetY = 1.0f;
        private const float RiseDuration = 1.0f;

        [SerializeField]
        private TMP_Text damageText;

        private Action<FloatingDamageText> _onFinished;
        private Tween _riseTween;

        public void Show(int damage, Vector3 position, Action<FloatingDamageText> onFinished)
        {
            _onFinished = onFinished;

            damageText.text = damage.ToString();

            // 몬스터 그림은 z가 1이라, 숫자를 0에 두어야 앞에 나옵니다.
            var start = new Vector3(position.x, position.y + StartOffsetY, 0f);
            var end = new Vector3(position.x, position.y + EndOffsetY, 0f);

            transform.position = start;
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);

            _riseTween?.Kill();
            _riseTween = transform.DOMove(end, RiseDuration).SetEase(Ease.OutQuad).OnComplete(Finish);
        }

        // 판이 끝나거나 다시 시작할 때 남아 있는 숫자를 즉시 걷습니다.
        public void ForceFinish()
        {
            if (!gameObject.activeSelf)
                return;

            _riseTween?.Kill();
            _riseTween = null;

            Finish();
        }

        private void OnDestroy()
        {
            // 씬을 나갈 때 트윈이 죽은 오브젝트를 붙들고 있으면 예외가 납니다.
            _riseTween?.Kill();
            _riseTween = null;
        }

        private void Finish()
        {
            gameObject.SetActive(false);

            var callback = _onFinished;
            _onFinished = null;

            callback?.Invoke(this);
        }
    }
}
