using System;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 제자리에서 한 번 재생되고 사라지는 이펙트.
    //
    // 발사체와 달리 날아가지도, 무엇을 맞히지도 않습니다. 폭발처럼 "이미 일어난 일"을
    // 눈에 보이게 하는 것이 전부라 판정도 대상도 없습니다.
    //
    // 애니메이션이 끝나는 시점을 Animator에 묻지 않고 duration으로 셉니다. 상태가
    // 하나뿐인 재생용 컨트롤러라 물어볼 것이 없고, 풀에서 꺼내 쓰는 동안 Animator가
    // 이전 재생 위치를 들고 있는 경우를 이쪽에서 함께 처리할 수 있습니다.
    public class OneShotEffect : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("재생 시간(초). 애니메이션 길이에 맞춥니다.")]
        private float duration = 0.3f;

        [SerializeField]
        [Tooltip("비워 두면 자식에서 찾습니다. 없어도 됩니다.")]
        private Animator animator;

        // 어느 풀에서 나왔는지. EffectSpawner가 되돌릴 때 씁니다.
        public string PoolKey { get; set; }

        private float _remaining;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void Play(Vector3 position)
        {
            transform.position = position;

            _remaining = Mathf.Max(0.01f, duration);

            gameObject.SetActive(true);

            // 풀에서 꺼낸 것은 지난번 재생이 끝난 자리에 멈춰 있습니다.
            // 되감지 않으면 마지막 프레임만 잠깐 보이고 사라집니다.
            if (animator != null && animator.isActiveAndEnabled)
                animator.Rebind();
        }

        // 재생이 끝났으면 true. 시간은 EffectSpawner가 넣어 줍니다.
        public bool Tick(float deltaTime)
        {
            if (!gameObject.activeSelf)
                return true;

            _remaining -= deltaTime;

            return _remaining <= 0f;
        }

        public void Stop()
        {
            _remaining = 0f;

            gameObject.SetActive(false);
        }
    }
}
