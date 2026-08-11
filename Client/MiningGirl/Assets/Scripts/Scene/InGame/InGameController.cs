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
            monsterController.Setup(damagePresenter: damageController);
            monsterController.InitControllerAsync().Forget();
            await UniTask.WaitUntil(() => monsterController.IsInitialized);

            // 테스트 스폰 — 초기 풀 10개, 2초마다 1마리씩 최대 30마리까지 플레이어 주변에 스폰합니다.
            // (완료를 기다리지 않고 백그라운드로 돌립니다 — 60초짜리 루프라 await하면 초기화가 그만큼 늦어집니다.)
            monsterController.ExecuteTestSpawn(_playerEntity, 0);

            // 광물 초기 배치 후, 이후 캐이는 만큼 주기적으로 보충하는 루프를 시작합니다.
            resourceController.SpawnInitialLayout(Vector3.zero);
            resourceController.ExecuteSpawn(_playerEntity);

            CoverUIManager.Instance.CoverUI.Hide().Forget();
            
            uIController.GameStart();
            
            IsInitialized = true;
        }

        // (테스트 버튼용) 현재 타겟 광물을 제외한 무작위 광물로 플레이어가 이동하게 합니다.
        // 씬의 Test 버튼 onClick에 이 메서드를 연결합니다.
        public void OnClickMoveToRandomResource()
        {
            if (_playerEntity is Scene.InGame.Entity.Player.Player player)
                player.MoveToRandomResource();
        }

        // 다음 스테이지로 넘어갈 때(또는 Reset 버튼) 호출됩니다.
        // 지금까지 스폰된 몬스터/광물을 모두 풀로 되돌린 뒤, 처음부터 다시 시작합니다.
        public void Next()
        {
            if (!IsInitialized)
                return;

            // 스폰 루프 중지 + 활성 몬스터 전부 풀로 반환
            monsterController.StopSpawn();

            // 화면에 떠 있는 플로팅 데미지도 모두 풀로 정리
            damageController.Clear();

            // 광물도 모두 풀로 정리
            resourceController.StopSpawn();

            // 플레이어의 진행 중이던 채굴/타겟을 초기화 — 방금 풀로 되돌린 광물을 계속 때리는 것을 방지합니다.
            if (_playerEntity is Scene.InGame.Entity.Player.Player player)
                player.ResetBehaviour();

            // 스폰을 다시 시작 (내부에서 이전 루프를 정리하고 새로 시작)
            monsterController.ExecuteTestSpawn(_playerEntity, 0);

            resourceController.SpawnInitialLayout(Vector3.zero);
            resourceController.ExecuteSpawn(_playerEntity);
            
            _playerEntity.SetPosition(Vector3.zero);
            
            uIController.SetTime();
            
            CoverUIManager.Instance.CoverUI.Hide(() =>
            {
                uIController.GameStart();
            }).Forget();
        }
    }
}