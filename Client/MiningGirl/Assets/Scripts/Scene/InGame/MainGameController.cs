using System.Linq;
using Cysharp.Threading.Tasks;
using InGame.temp.System.FloatingDamage;
using MainGame.Entity.Monster;
using Manager;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace MainGame
{
    public class MainGameController : GameMonoInitializer
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
        
        // Next()에서 재시작할 때 재사용하기 위해 플레이어 엔티티를 보관합니다.
        private IEntity _playerEntity;

        private void Start()
        {
            if (!IsInitialized)
            {
                InitAsync().Forget();
            }
        }

        private async UniTask InitAsync()
        {
            uIController.InitAsync(Next).Forget();
            await UniTask.WaitUntil(() => uIController.IsInitialized);
            
            damageController.InitAsync();
            await UniTask.WaitUntil(() => damageController.IsInitialized);
            
            playerController.InitAsync().Forget();
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

            CoverUIManager.Instance.CoverUI.Hide().Forget();
            
            uIController.GameStart();
            
            IsInitialized = true;
        }

        // 다음 스테이지로 넘어갈 때 호출됩니다.
        // 지금까지 스폰된 몬스터를 모두 풀로 되돌린 뒤, 스폰을 처음부터 다시 시작합니다.
        private void Next()
        {
            if (!IsInitialized)
                return;

            // 스폰 루프 중지 + 활성 몬스터 전부 풀로 반환
            monsterController.StopSpawn();

            // 화면에 떠 있는 플로팅 데미지도 모두 풀로 정리
            damageController.Clear();

            // 스폰을 다시 시작 (내부에서 이전 루프를 정리하고 새로 시작)
            monsterController.ExecuteTestSpawn(_playerEntity, 0);
            
            _playerEntity.SetPosition(Vector3.zero);
            
            uIController.SetTime();
            
            CoverUIManager.Instance.CoverUI.Hide(() =>
            {
                uIController.GameStart();
            }).Forget();
        }
    }
}