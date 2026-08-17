using System;
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
        [SerializeField]
        [Tooltip("레벨업 보너스와 보상 지급을 관리합니다")]
        private LevelUpController levelUpController;
        [SerializeField]
        [Tooltip("터치 공격 입력 (팝업 중 정지용)")]
        private Scene.InGame.Entity.Touch.TouchEntityController touchController;
        [SerializeField]
        [Tooltip("손패 카드 (드래그 앤 드롭)")]
        private MainGame.Card.CardHandController cardHandController;
        [SerializeField]
        [Tooltip("일시정지 테스트 버튼의 라벨 (선택)")]
        private TMPro.TMP_Text pauseButtonText;
        
        // Next()에서 재시작할 때 재사용하기 위해 플레이어 엔티티를 보관합니다.
        private IEntity _playerEntity;

        public async UniTask InitAsync()
        {
            uIController.InitAsync(() => ShowUpgradeThen(true, () => Next())).Forget();

            // 보상/보너스 지급 경로를 UI 컨트롤러에 연결합니다.
            levelUpController.Init(uIController.AddGold);

            // 광물을 캘 때마다 채굴 진행도(클리어 조건)를 올립니다.
            levelUpController.SetResourceMinedHandler(() => uIController.AddMinedCount());

            // 카드 버프 표시 시작
            uIController.InitBuffList(levelUpController.StatContext.Buffs);

            // 손패 카드 초기화
            if (cardHandController != null)
            {
                // 스킬 카드가 효과를 실행할 때 필요한 것들을 묶어 넘깁니다.
                var skillContext = new MainGame.Card.SkillCardContext(
                    getPlayer: () => _playerEntity,
                    getMonsters: () => monsterController.ActivateList.ConvertAll(m => (Scene.InGame.Entity.Interface.IEntity)m),
                    buffs: levelUpController.StatContext.Buffs,
                    healPlayerByRatio: playerController.HealPlayerByRatio,
                    camera: Camera.main,
                    addCost: amount => uIController.AddCost(amount),
                    spawnSpecialResource: SpawnSpecialResource);

                cardHandController.Init(uIController.CanAffordCost, uIController.TrySpendCost, skillContext);
            }
            await UniTask.WaitUntil(() => uIController.IsInitialized);
            
            damageController.InitAsync();
            await UniTask.WaitUntil(() => damageController.IsInitialized);

            // 광물 컨트롤러를 먼저 준비합니다 — 플레이어가 이 컨트롤러를 광물 공급자(IResourceProvider)로
            // 주입받아 가장 가까운 광물을 탐색하기 때문에, 플레이어 초기화보다 앞서 준비되어야 합니다.
            resourceController.Setup(damagePresenter: damageController, rewardHandler: levelUpController);
            resourceController.InitControllerAsync().Forget();
            await UniTask.WaitUntil(() => resourceController.IsInitialized);

            // 플레이어 — 광물 공급자를 주입해서 행동 트리(가장 가까운 광물로 이동)를 구성합니다.
            playerController.InitAsync(resourceController, levelUpController.StatContext).Forget();
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

            // 캐릭터를 아직 고르지 않았다면(=씬 첫 시작) 선택 팝업부터 띄웁니다.
            // 재시작(Next)에서는 이미 고른 캐릭터와 강화 상태를 그대로 씁니다.
            if (!levelUpController.HasCharacter)
            {
                uIController.ShowCharacterSelect(row =>
                {
                    levelUpController.SetCharacter(row);
                    CoverUIManager.Instance.CoverUI.Hide(() => GameStart().Forget()).Forget();
                });
            }
            else
            {
                CoverUIManager.Instance.CoverUI.Hide(() => GameStart().Forget()).Forget();
            }
            
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

            // 죽으면 같은 스테이지를 다시 시작합니다.
            playerController.SetDeadHandler(() => RestartStage());

            // 피격 시 스태미나 소모, 스태미나가 바닥나면 스테이지 재시작
            playerController.SetDamagedHandler(() => uIController.ConsumeStaminaByHit());
            uIController.SetStaminaEmptyHandler(() => RestartStage());

            // 강화 팝업: 번 골드를 스테이지 사이에 쓰는 창구
            upgradePopup?.Init(
                getGold: () => uIController.Gold,
                trySpendGold: uIController.TrySpendGold,
                getLevel: row => levelUpController.BonusState.GetLevel(row.Id.ToString()),
                onPurchase: row => levelUpController.ApplyBonus(row));

            // 강화로 올린 스태미나 보정치를 연결합니다.
            uIController.SetStaminaBonusProvider(() =>
            {
                var bonus = levelUpController.BonusState;

                return (bonus.MaxStaminaAdd, bonus.MaxStaminaMultiplier,
                    bonus.MiningStaminaCostReduce, bonus.HitStaminaCostReduce);
            });

            // 터치 사거리 판정 기준(캐릭터 위치)을 넘겨줍니다.
            if (touchController != null)
                touchController.SetPlayerPositionProvider(() => _playerEntity != null ? _playerEntity.GetPosition() : Vector3.zero);

            // 밀치기 대상(활성 몬스터) 조회를 넘겨줍니다.
            if (touchController != null)
                touchController.SetMonsterProvider(() =>
                    monsterController.ActivateList.ConvertAll(m => (Scene.InGame.Entity.Interface.IEntity)m));

            // 새 스테이지에서는 밀치기를 바로 쓸 수 있게 합니다.
            if (touchController != null)
                touchController.ResetCooldown();

            // 새 스테이지가 시작됐으니 재시작 잠금을 풉니다.
            _isRestarting = false;
            _isStageEnding = false;


        // 손패를 좌측부터 순차로 깔아줍니다.
            if (cardHandController != null)
                cardHandController.StartHand();
        }

        // '특수 광물' 카드용 — 지정한 자리에 광물을 소환하고 바로 그것을 캐러 가게 합니다.
        private void SpawnSpecialResource(Vector3 position)
        {
            SpawnSpecialResourceAsync(position).Forget();
        }

        private async UniTaskVoid SpawnSpecialResourceAsync(Vector3 position)
        {
            var resource = await resourceController.Spawn(position);

            if (resource == null)
            {
                Debug.LogWarning("[Card] 특수 광물을 소환하지 못했습니다.");
                return;
            }

            // 소환한 광물을 즉시 채굴 타겟으로 지정합니다.
            if (_playerEntity is Player player)
                player.SetTarget(resource);
        }

        // 테스트 버튼용 일시정지 토글.
        public void OnClickTogglePause()
        {
            if (!IsInitialized)
                return;

            _isManualPaused = !_isManualPaused;
            SetGamePaused(_isManualPaused);
            UpdatePauseButtonText();
        }

        private void UpdatePauseButtonText()
        {
            if (pauseButtonText != null)
                pauseButtonText.text = _isManualPaused ? "▶" : "II";
        }

        // 팝업이 떠 있는 동안 타이머와 모든 엔티티의 행동을 멈춥니다.
        private void SetGamePaused(bool paused)
        {
            uIController.SetPaused(paused);
            playerController.SetBehaviourPaused(paused);
            monsterController.SetBehaviourPaused(paused);
            resourceController.SetBehaviourPaused(paused);

            if (touchController != null)
                touchController.SetPaused(paused);

            // 떠 있는 플로팅 데미지 연출도 함께 멈춥니다.
            if (damageController != null)
                damageController.SetPaused(paused);

            // 정지 중에는 카드도 만질 수 없게 합니다.
            if (cardHandController != null)
                cardHandController.SetPaused(paused);

            // 카드로 소환된 불덩이도 함께 멈춥니다(지속시간도 같이 멈춤).
            MainGame.Card.Effects.FireBallObject.SetPausedAll(paused);
        }

        // 다음 스테이지로 넘어갈 때(또는 Reset 버튼) 호출됩니다.
        // 지금까지 스폰된 몬스터/광물을 모두 풀로 되돌린 뒤, 처음부터 다시 시작합니다.
        // 캐릭터가 죽으면 같은 스테이지를 처음부터 다시 합니다.
        // 레벨·경험치·골드·강화는 런 단위로 유지되므로 재도전할수록 유리해집니다.
        public void RestartStage()
        {
            if (_isRestarting)
                return;

            _isRestarting = true;

            // 실패해도 번 골드로 강화할 수 있습니다.
            ShowUpgradeThen(false, () => Next(false));
        }

        // 스테이지가 끝나면 강화 팝업을 띄우고, 닫은 뒤에 다음 동작을 이어갑니다.
        // 살 수 있는 항목이 하나도 없으면 팝업을 건너뜁니다(빈 창을 닫게 하지 않기 위함).
        private void ShowUpgradeThen(bool isCleared, System.Action next)
        {
            if (_isStageEnding)
                return;

            _isStageEnding = true;

            if (upgradePopup == null || !upgradePopup.HasAnyAffordable(uIController.Stage))
            {
                next?.Invoke();

                return;
            }

            SetGamePaused(true);

            upgradePopup.Show(uIController.Stage, isCleared, () =>
            {
                SetGamePaused(false);

                next?.Invoke();
            });
        }

        [SerializeField]
        private MainGame.UI.UpgradePopup upgradePopup;

        // 테스트 버튼으로 건 수동 일시정지 상태
        private bool _isManualPaused;

        private bool _isRestarting;

        // 스테이지가 끝나는 중인지. 클리어와 스태미나 소진이 같은 프레임에 겹치면
        // 팝업이 두 번 뜨거나 결과가 뒤집히므로 첫 번째만 받습니다.
        private bool _isStageEnding;

        public void Next(bool advanceStage = true)
        {
            if (!IsInitialized)
                return;

            CoverUIManager.Instance.CoverUI.Show(() => 
            {
                // 화면이 덮인 동안 손패를 감춥니다(밝아질 때 이전 카드가 보이지 않도록).
                if (cardHandController != null)
                    cardHandController.HideHand();

                // 스폰 루프 중지 + 활성 몬스터 전부 풀로 반환
                monsterController.StopSpawn();

                // 화면에 떠 있는 플로팅 데미지도 모두 풀로 정리
                damageController.Clear();

                // 카드로 소환된 불덩이도 정리(정지 상태도 함께 해제)
                MainGame.Card.Effects.FireBallObject.ClearAll();

                // 광물도 모두 풀로 정리
                resourceController.StopSpawn();

                // 플레이어 행동 정지 + 진행 중이던 채굴/타겟 초기화
                // (방금 풀로 되돌린 광물을 계속 때리는 것을 방지합니다.)
                // 재시작 시 수동 일시정지는 해제합니다.
                _isManualPaused = false;

                UpdatePauseButtonText();

                playerController.StopBehaviour();

                // 체력과 무적/다운 상태도 초기화합니다.
                playerController.ResetPlayerHealth();

                // 광물만 다시 깔아둡니다. 실제 진행 재개는 아래 GameStart()에서 합니다.
                resourceController.SpawnInitialLayout(Vector3.zero);
            
                WarpPlayer(Vector3.zero);
            
                uIController.SetTime(advanceStage);
                
                // 캐릭터를 아직 고르지 않았다면(=씬 첫 시작) 선택 팝업부터 띄웁니다.
            // 재시작(Next)에서는 이미 고른 캐릭터와 강화 상태를 그대로 씁니다.
            if (!levelUpController.HasCharacter)
            {
                uIController.ShowCharacterSelect(row =>
                {
                    levelUpController.SetCharacter(row);
                    CoverUIManager.Instance.CoverUI.Hide(() => GameStart().Forget()).Forget();
                });
            }
            else
            {
                CoverUIManager.Instance.CoverUI.Hide(() => GameStart().Forget()).Forget();
            }
            }).Forget();
        }
    }
}
