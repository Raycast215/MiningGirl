using System.Linq;
using Cysharp.Threading.Tasks;
using MainGame.Entity.Monster;
using Manager;
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
        
        [Header("Entity")]
        [SerializeField]
        private PlayerController playerController;
        [SerializeField]
        private MonsterController monsterController;
        
        private void Start()
        {
            if (!IsInitialized)
            {
                InitAsync().Forget();
            }
            
            CoverUIManager.Instance.CoverUI.Hide().Forget();
        }

        private async UniTask InitAsync()
        {
            playerController.InitAsync(null).Forget();
            await UniTask.WaitUntil(() => playerController.IsInitialized);

            var playerEntity = playerController.ActivateList.First();
            cam.Follow = playerEntity.GetTransform();

            monsterController.Setup();
            monsterController.InitControllerAsync().Forget();
            await UniTask.WaitUntil(() => monsterController.IsInitialized);

            // 테스트 스폰 — 초기 풀 10개, 2초마다 1마리씩 최대 30마리까지 플레이어 주변에 스폰합니다.
            // (완료를 기다리지 않고 백그라운드로 돌립니다 — 60초짜리 루프라 await하면 초기화가 그만큼 늦어집니다.)
            monsterController.ExecuteTestSpawn(playerEntity, 0).Forget();

            IsInitialized = true;
        }
    }
}