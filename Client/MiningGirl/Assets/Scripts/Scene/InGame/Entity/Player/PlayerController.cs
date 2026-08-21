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
            if (ins == null)
                return;

            
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

            ins.ResetStatus();
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

        // 피격 시 호출할 콜백을 활성 플레이어에게 전달합니다.
        public void SetDamagedHandler(System.Action handler)
        {
            foreach (var player in ActivateList)
                player.SetDamagedHandler(handler);
        }

        // 스테이지 시작 시 무적·깜빡임 상태를 초기화합니다.
        public void ResetPlayerStatus()
        {
            if (ActivateList == null)
                return;

            foreach (var player in ActivateList)
                player.ResetStatus();
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

            UpdateEntity();
        }
    }
}
