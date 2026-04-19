using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using InGame.temp.System.FloatingDamage;
using Manager;
using UnityEngine;
using Scene.InGame.Entity.Enemy;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Player;
using Scene.InGame.Entity.Resource;
using Scene.InGame.UI;
using Unity.Cinemachine;

namespace Scene.InGame
{
    public interface IEntityHandler
    {
        public IEntity GetPlayer();
        public IEnumerable<IEntity> GetEnemyList();
        public IEnumerable<IEntity> GetResourceList();
    }

    public interface IInGameHandler
    {
        public IEntityHandler GetEntityHandler();
        public IInGameUIHandler GetUIHandler();
        public void ShowDamageFloatingText(int damage, Vector3 targetPos, bool isCritical = false);
        public void CameraAnimation();
    }
    
    public class MainGame : GameInitializer, IInGameHandler, IEntityHandler
    {
        [Header("UI")]
        [SerializeField]
        private InGameUI inGameUI;

        [SerializeField] 
        private CinemachineCamera cam;
        
        [Header("Entity Controller")]
        [SerializeField]
        private ResourceController resourceController;
        [SerializeField]
        private EnemyController enemyController;
        [SerializeField]
        private PlayerController playerController;
        [SerializeField]
        private DamageController damageController;
        
        private void Start()
        {
            InitAsync().Forget();
        }

        private async UniTaskVoid InitAsync()
        {
            inGameUI.InitAsync().Forget();
            await UniTask.WaitUntil(() => inGameUI.IsInitialized);
            
            resourceController.InitAsync(this).Forget();
            await UniTask.WaitUntil(() => resourceController.IsInitialized);
            
            playerController.InitAsync(this).Forget();
            await UniTask.WaitUntil(() => playerController.IsInitialized);
            cam.Follow = GetPlayer().GetTransform();
            
            enemyController.Init(this);
            await UniTask.WaitUntil(() => enemyController.IsInitialized);
            
            damageController.InitAsync().Forget();
            await UniTask.WaitUntil(() => damageController.IsInitialized);
            
            inGameUI.GameReady();
            CoverUIManager.Instance.CoverUI.Hide().Forget();
            await UniTask.WaitForSeconds(0.5f);
            
            inGameUI.GameStart();
            IsInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!IsInitialized)
                return;
            
            enemyController.UpdateEntity();
            playerController.UpdateEntity();
        }

#region IEntityHandler

        public IEntity GetPlayer()
        {
            return playerController.ActivateList.FirstOrDefault();
        }

        public IEnumerable<IEntity> GetEnemyList()
        {
            return enemyController.ActivateList;
        }

        public IEnumerable<IEntity> GetResourceList()
        {
            return resourceController.ActivateList;
        }

#endregion

#region IInGameHandler

        public IEntityHandler GetEntityHandler()
        {
            return this;
        }

        public IInGameUIHandler GetUIHandler()
        {
            return inGameUI;
        }
        
        public void ShowDamageFloatingText(int damage, Vector3 targetPos, bool isCritical = false)
        {
            damageController.Damage(damage, targetPos, isCritical);
        }

        public async void CameraAnimation()
        {
            if (PlayerPrefs.GetInt("IsCriticalShow") == 0)
                return;
            
            float start = 12f;
            float zoomTarget = 8.0f;   // 살짝 오버슈트
            float endTarget = 12f;

            float durationIn = 0.15f;  // 빠르게 줌인
            float durationOut = 0.8f;  // 천천히 복귀

            float slowTimeScale = 0.2f;

            float t = 0f;

            // 🔻 TimeScale 줄이기
            // Time.timeScale = slowTimeScale;
            // Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // 1. 확 줌인
            while (t < durationIn)
            {
                t += Time.unscaledDeltaTime; // 중요: TimeScale 영향 안 받게

                float normalized = t / durationIn;
                float eased = 1 - Mathf.Pow(1 - normalized, 4); // 강한 EaseOut

                cam.Lens.OrthographicSize = Mathf.Lerp(start, zoomTarget, eased);

                await UniTask.Yield();
            }

            cam.Lens.OrthographicSize = zoomTarget;

            // 2. 천천히 복귀 + TimeScale 복원
            t = 0f;

            while (t < durationOut)
            {
                t += Time.unscaledDeltaTime;

                float normalized = t / durationOut;

                // 카메라 EaseOut
                float easedZoom = 1 - (1 - normalized) * (1 - normalized);

                // TimeScale도 같이 복원
                float easedTime = Mathf.Lerp(slowTimeScale, 1f, normalized);

                cam.Lens.OrthographicSize = Mathf.Lerp(zoomTarget, endTarget, easedZoom);

                // Time.timeScale = easedTime;
                // Time.fixedDeltaTime = 0.02f * Time.timeScale;

                await UniTask.Yield();
            }

            // 마무리 정리
            cam.Lens.OrthographicSize = endTarget;
            // Time.timeScale = 1f;
            // Time.fixedDeltaTime = 0.02f;
        }

#endregion
    }
}