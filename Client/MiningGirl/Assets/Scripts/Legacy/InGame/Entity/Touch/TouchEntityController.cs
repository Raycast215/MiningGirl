using System;
using System.Collections.Generic;
using Legacy.Scene.InGame.Entity.Interface;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Legacy.Scene.InGame.Entity.Touch
{
    // 화면 아무 곳이나 탭하면 캐릭터 주변(pushRange) 몬스터를 한꺼번에 밀어냅니다.
    // 피해는 주지 않습니다 — 처치는 카드가 전담하고, 밀치기는 '시간을 버는' 수단입니다.
    // 몬스터를 하나씩 조준하는 방식은 폰에서 손가락이 대상을 가려 쓰기 어려워 화면 탭으로 바꿨습니다.
    public class TouchEntityController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("캐릭터로부터 이 거리 안에 있는 몬스터를 모두 밀어냅니다.")]
        private float pushRange = 5f;

        [SerializeField]
        [Tooltip("밀어내는 거리")]
        private float pushDistance = 2.5f;

        [SerializeField]
        [Tooltip("밀치기 사이의 대기 시간(초). 연타로 무한 방어하는 것을 막습니다.")]
        private float pushCooldown = 2f;

        [SerializeField]
        [Tooltip("밀려나는 데 걸리는 시간(초). 0이면 순간이동처럼 보입니다.")]
        private float pushDuration = 0.2f;

        // 팝업 등으로 게임이 멈춘 동안에는 밀치기도 막습니다.
        private bool _isPaused;

        // 다음 밀치기까지 남은 시간
        private float _cooldownTimer;

        // 사거리 판정 기준이 되는 캐릭터 위치
        private Func<Vector3> _getPlayerPosition;

        // 밀어낼 대상 목록 조회(활성 몬스터)
        private Func<IReadOnlyList<IEntity>> _getMonsters;

        // 쿨타임 진행도(0=사용 가능, 1=방금 씀) — UI 표시용
        public float CooldownRatio => pushCooldown <= 0f ? 0f : Mathf.Clamp01(_cooldownTimer / pushCooldown);
        public bool IsReady => _cooldownTimer <= 0f;
        public float PushRange => pushRange;

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        // 스테이지가 새로 시작되면 쿨타임을 풉니다.
        public void ResetCooldown()
        {
            _cooldownTimer = 0f;
        }

        public void SetPlayerPositionProvider(Func<Vector3> provider)
        {
            _getPlayerPosition = provider;
        }

        public void SetMonsterProvider(Func<IReadOnlyList<IEntity>> provider)
        {
            _getMonsters = provider;
        }

        private void Update()
        {
            if (_isPaused)
                return;

            // 정지 중에는 쿨타임도 함께 멈춥니다.
            if (_cooldownTimer > 0f)
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);

            if (IsTouchDown() && IsReady)
                TryPush();
        }

        private bool IsTouchDown()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Input.GetMouseButtonDown(0);
#else
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
        }

        // 캐릭터 주변 몬스터를 바깥으로 밀어냅니다. 한 마리도 없으면 쿨타임을 쓰지 않습니다.
        private void TryPush()
        {
            // UI(카드·버튼) 위 터치는 무시합니다.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_getMonsters == null || _getPlayerPosition == null)
                return;

            var monsters = _getMonsters.Invoke();

            if (monsters == null)
                return;

            var origin = _getPlayerPosition.Invoke();
            var pushed = 0;

            foreach (var monster in monsters)
            {
                if (monster == null || !monster.GetActiveState())
                    continue;

                var targetPos = monster.GetPosition();

                if (Vector3.Distance(origin, targetPos) > pushRange)
                    continue;

                // 위치를 직접 대입하면 순간이동처럼 보입니다.
                // 피격 넉백과 같은 DOMove 트윈(Monster.PushFrom)으로 밀어냅니다.
                var pushable = monster as Legacy.MainGame.Entity.Monster.Monster;

                if (pushable == null)
                    continue;

                pushable.PushFrom(origin, pushDistance, pushDuration);
                pushed++;
            }

            // 실제로 한 마리라도 밀어냈을 때만 쿨타임이 돕니다(헛손질은 손해가 없습니다).
            if (pushed > 0)
                _cooldownTimer = pushCooldown;
        }
    }
}
