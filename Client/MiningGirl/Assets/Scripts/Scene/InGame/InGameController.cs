using System.Linq;
using Cysharp.Threading.Tasks;
using InGame.temp.System.FloatingDamage;
using MainGame;
using MainGame.Entity.Monster;
using Manager;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Player;
using Scene.InGame.Entity.Resource;
using Unity.Cinemachine;
using UnityEngine;

namespace Scene.InGame
{
    public class InGameController : GameMonoInitializer
    {
        [Header("Cam")]
        [SerializeField] 
        private CinemachineCamera cam;
        
        [Header("UI")]
        [SerializeField]
        private MainGameUIController uIController;
        
        [Header("Damage Object")]
        [SerializeField]
        private DamageController damageController;
        
        [Header("Entity")]
        [SerializeField]
        private PlayerController playerController;
        [SerializeField]
        private MonsterController monsterController;
        [SerializeField]
        private ResourceController resourceController;
        
        // Next()에서 재시작할 때 재사용하기 위해 플레이어 엔티티를 보관합니다.
        private IEntity _playerEntity;

        public async UniTask InitAsync()
        {
            uIController.InitAsync(Next).Forget();
            await UniTask.WaitUntil(() => uIController.IsInitialized);
            
            damageController.InitAsync();
            await UniTask.WaitUntil(() => damageController.IsInitialized);

            // 광물 컨트롤러를 먼저 준비합니다 — 플레이어가 이 컨트롤러를 광물 공급자(IResourceProvider)로
            // 주입받아 가장 가까운 광물을 탐색하기 때문에, 플레이어 초기화보다 앞서 준비되어야 합니다.
            resourceController.Setup(damagePresenter: damageController);
            resourceController.InitControllerAsync().Forget();
            await UniTask.WaitUntil(() => resourceController.IsInitialized);

            // 플레이어 — 광물 공급자를 주입해서 행동 트리(가장 가까운 광물로 이동)를 구성합니다.
            playerController.InitAsync(resourceController).Forget();
            await UniTask.WaitUntil(() => playerController.IsInitialized);

            _playerEntity = playerController.ActivateList.First();
            cam.Follow = _playerEntity.GetTransform();
            
            // DamageController를 IFloatingDamagePresenter로 주입 — 몬스터가 피격 시 플로팅 데미지를 띄웁니다.
            // resourceController를 IResourceProvider로 주입 — 몬스터가 이동 중 광물을 비껴가게 합니다.
            monsterController.Setup(damagePresenter: damageController, resourceProvider: resourceController);
            monsterController.InitControllerAsync().Forget();
            await UniTask.WaitUntil(() => monsterController.IsInitialized);

            // 광물만 미리 화면에 깔아둡니다. 실제 게임 진행(스폰 루프/이동/채굴)은 GameStart()에서 시작합니다.
            resourceController.SpawnInitialLayout(Vector3.zero);

            CoverUIManager.Instance.CoverUI.Hide(() => GameStart().Forget()).Forget();
            
            IsInitialized = true;
        }

        // 플레이어를 지정 위치로 즉시 이동시키고, 카메라도 보간 없이 그 자리로 스냅합니다.
        // (그냥 위치만 바꾸면 Cinemachine이 이전 위치에서 부드럽게 따라오면서 이동 과정이 보입니다.)
        private void WarpPlayer(Vector3 position)
        {
            var before = _playerEntity.GetPosition();
            _playerEntity.SetPosition(position);
            var delta = position - before;

            if (cam == null)
                return;

            // 타겟이 순간이동했음을 알려 카메라가 같은 delta만큼 즉시 따라가게 합니다.
            cam.OnTargetObjectWarped(_playerEntity.GetTransform(), delta);

            // 댐핑 등 이전 프레임 상태를 무효화해 다음 갱신에서 보간 없이 자리를 잡게 합니다.
            cam.PreviousStateIsValid = false;
        }

        // 실제 게임 진행을 시작합니다.
        // 이 시점부터 몬스터가 스폰/이동하고, 플레이어가 광물을 탐색·이동·채굴합니다.
        private async UniTaskVoid GameStart()
        {
            await UniTask.WaitForSeconds(0.5f);
            
            uIController.GameStart();

            // 몬스터 스폰 루프 시작 (내부에서 몬스터 이동/공격도 함께 켜집니다)
            monsterController.ExecuteTestSpawn(_playerEntity, 0);

            // 광물 보충 루프 시작 (초기 배치는 InitAsync/Next에서 이미 끝난 상태)
            resourceController.ExecuteSpawn(_playerEntity);

            // 플레이어 행동 트리(광물 탐색 → 이동 → 채굴) 시작
            playerController.StartBehaviour();
        }

        // (테스트 버튼용) 현재 타겟 광물을 제외한 무작위 광물로 플레이어가 이동하게 합니다.
        // 씬의 Test 버튼 onClick에 이 메서드를 연결합니다.
        public void OnClickMoveToRandomResource()
        {
            if (_playerEntity is Player player)
                player.MoveToRandomResource();
        }

        // 다음 스테이지로 넘어갈 때(또는 Reset 버튼) 호출됩니다.
        // 지금까지 스폰된 몬스터/광물을 모두 풀로 되돌린 뒤, 처음부터 다시 시작합니다.
        public void Next()
        {
            if (!IsInitialized)
                return;

            CoverUIManager.Instance.CoverUI.Show(() => 
            {
                // 스폰 루프 중지 + 활성 몬스터 전부 풀로 반환
                monsterController.StopSpawn();

                // 화면에 떠 있는 플로팅 데미지도 모두 풀로 정리
                damageController.Clear();

                // 광물도 모두 풀로 정리
                resourceController.StopSpawn();

                // 플레이어 행동 정지 + 진행 중이던 채굴/타겟 초기화
                // (방금 풀로 되돌린 광물을 계속 때리는 것을 방지합니다.)
                playerController.StopBehaviour();

                // 광물만 다시 깔아둡니다. 실제 진행 재개는 아래 GameStart()에서 합니다.
                resourceController.SpawnInitialLayout(Vector3.zero);
            
                WarpPlayer(Vector3.zero);
            
                uIController.SetTime();
                
                CoverUIManager.Instance.CoverUI.Hide(() => GameStart().Forget()).Forget();
            }).Forget();
        }
    }
}