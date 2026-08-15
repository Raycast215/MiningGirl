using Cysharp.Threading.Tasks;
using MainGame.Entity.Player.Cursor;
using Scene.InGame.Entity.Data;
using Scene.InGame.Entity.Resource;
using UnityEngine;

namespace Scene.InGame.Entity.Player
{
    public class PlayerController : EntityControllerBase<Player>
    {
        [SerializeField]
        private CursorUI cursor;

        // GameStart() 전에는 플레이어가 움직이거나 채굴하지 않도록 행동 트리 구동을 막습니다.
        private bool _isBehaviourRunning;
        
        public async UniTaskVoid InitAsync(IResourceProvider resourceProvider = null, global::MainGame.Bonus.CharacterStatContext statContext = null)
        {
            if (IsInitialized)
                return;
            
            InitAsync("Player", 1).Forget();
            await UniTask.WaitUntil(() => IsInitialized);

            var ins = await Get();
            
            ins.BaseData = new EntityData
            {
                MoveSpeed = 1,
                MoveToMinDistance = 1,
                AttackDelay = 60,
            };
            
            // 행동 트리(InitAsync 내부)가 구성되기 전에 광물 공급자를 먼저 주입해야
            // SearchTargetNode가 공급자를 제대로 참조합니다.
            ins.SetResourceProvider(resourceProvider);
            ins.SetStatContext(statContext);
            // 프리팹 안에 붙어 있는 머리 위 체력바를 상태 표시로 연결합니다.
            var statusView = ins.GetComponentInChildren<global::UI.Common.PlayerStatusBarView>(true);
            if (statusView != null)
                ins.SetStatusPresenter(statusView);

            ins.ResetHealth();
            ins.InitDirectionEvent(cursor.SetDirection);
            ins.InitAsync().Forget();
            ins.SetPosition(Vector3.zero);
            ins.gameObject.SetActive(true);
            
            cursor.Set(ins.transform);
        }

        public void SetPosition(Vector3 position)
        {
            ActivateList[0].transform.position = position;
        }

        // 회복 카드용 — 살아있는 플레이어를 비율만큼 회복시킵니다.
        public void HealPlayerByRatio(float ratio)
        {
            if (ActivateList == null)
                return;

            foreach (var player in ActivateList)
                player.HealByRatio(ratio);
        }

        // 스테이지 재시작 시 체력과 무적/다운 상태를 초기화합니다.
        public void ResetPlayerHealth()
        {
            if (ActivateList == null)
                return;

            foreach (var player in ActivateList)
                player.ResetHealth();
        }

        // 게임 시작 — 이 시점부터 플레이어가 광물을 탐색/이동/채굴합니다.
        public void StartBehaviour()
        {
            _isBehaviourRunning = true;
        }

        // 팝업 등으로 잠시 멈출 때 사용합니다. 타겟/진행 상태는 그대로 유지됩니다.
        public void SetBehaviourPaused(bool paused)
        {
            _isBehaviourRunning = !paused;
        }

        // 게임 정지(리셋 등) — 행동 트리 구동을 멈춥니다.
        public void StopBehaviour()
        {
            _isBehaviourRunning = false;

            foreach (var player in ActivateList)
                player.ResetBehaviour();
        }

        // 플레이어의 행동 트리(광물 탐색 → 이동 → 채굴)를 매 프레임 구동합니다.
        // (MonsterController와 동일하게, 이게 없으면 NodeRunner가 구성만 되고 실행되지 않습니다.)
        private void Update()
        {
            // 초기화 전(풀 생성 전)에는 목록이 없으므로 건너뜁니다.
            if (ActivateList == null)
                return;

            // 정지 중에는 이동뿐 아니라 무적/다운 시간과 깜빡임 연출도 함께 멈춥니다.
            if (!_isBehaviourRunning)
            {
                foreach (var player in ActivateList)
                {
                    player.StopMove();
                    player.SetStatusPaused(true);
                }

                return;
            }

            foreach (var player in ActivateList)
            {
                player.SetStatusPaused(false);
                player.UpdateStatus(Time.deltaTime);
            }

            // 쓰러진 동안에는 이동/채굴을 멈춥니다.
            // (MovePosition 이동이라 남은 속도까지 없애야 미끄러지지 않습니다.)
            foreach (var player in ActivateList)
            {
                if (!player.IsDown)
                    continue;

                player.StopMove();
                return;
            }

            UpdateEntity();
        }
    }
}