using System.Linq;
using Cysharp.Threading.Tasks;
using InGame.temp.System.FloatingDamage;
using MainGame;
using MainGame.Entity.Monster;
using Manager;
using Scene.InGame.Card;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Player;
using Scene.InGame.Entity.Resource;
using Scene.InGame.UI;
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
        private CardHandController cardHandController;
        [SerializeField]
        [Tooltip("일시정지 테스트 버튼의 라벨 (선택)")]
        private TMPro.TMP_Text pauseButtonText;
        
        // Next()에서 재시작할 때 재사용하기 위해 플레이어 엔티티를 보관합니다.
        private IEntity _playerEntity;

        public async UniTask InitAsync()
        {
            uIController.InitAsync(() => ShowUpgradeThen(true, () => Next())).Forget();

            // 저장된 진행을 불러옵니다(스테이지·골드·캐릭터·강화).
            var save = Manager.GameDataManager.Instance;

            if (save != null && save.HasSave)
            {
                // 강화를 먼저 복원해야 스태미나 최대치 계산에 반영됩니다.
                levelUpController.RestoreFromSave(save.Data.CharacterId, save.GetUpgradeLevels());
                uIController.RestoreProgress(save.Data.Stage, save.Data.Gold);

                // 강화 도중에 껐다면 그 화면부터 다시 보여줍니다.
                _resumeUpgradePhase = save.Data.IsUpgradePhase;
                _resumeUpgradeFromClear = save.Data.IsUpgradeFromClear;
            }

            // 보상/보너스 지급 경로를 UI 컨트롤러에 연결합니다.
            levelUpController.Init(uIController.AddGold);

            // 광물을 캘 때마다 채굴 진행도(클리어 조건)를 올립니다.
            levelUpController.SetResourceMinedHandler(() => uIController.AddMinedCount());

            // 몬스터를 잡으면 데이터의 GoldReward에 강화 보너스를 얹어 지급합니다.
            monsterController.SetKilledHandler(gold => levelUpController.OnMonsterKilled(gold));

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
                    healPlayerByRatio: ratio => uIController.RecoverStaminaByRatio(ratio),
                    camera: Camera.main,
                    addCost: amount => uIController.AddCost(amount),
                                        spawnSpecialResource: SpawnSpecialResource,
                    getResources: () => resourceController.GetActiveResources());

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
                    GameDataManager.Instance?.SaveCharacter(row?.Id);

                    StartFirstStage();
                });
            }
            else
            {
                StartFirstStage();
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

        // 씬에 처음 들어왔을 때의 시작 순서.
        //
        // 스테이지 인트로를 화면 덮개 '위에' 먼저 띄우고, 화면이 가려진 뒤에 덮개를 걷습니다.
        // 덮개를 먼저 걷으면 캐릭터 선택 팝업이 사라지면서 인게임 화면이 잠깐 그대로
        // 드러난 뒤에 연출이 덮는 모양이 됩니다.
        private void StartFirstStage()
        {
            // 연출이 끝날 때까지 판은 멈춰 둡니다.
            SetGamePaused(true);

            if (stageMapPopup == null)
            {
                HideCover(() => GameStart().Forget());

                return;
            }

            stageMapPopup.Init();

            var startStage = uIController.Stage;

            // showInstantly: 캐릭터 선택 팝업이 닫히는 그 프레임에 맵이 화면을 덮습니다.
            // 페이드로 들어오면 그 0.3초 동안 인게임 화면이 그대로 보입니다.
            //
            // 스테이지 사이 연출과 달리 칸 이동은 없고, 지금 칸에 자리잡는 모양만 보입니다.
            stageMapPopup.PlayAsync(startStage, startStage, GetMaxStage(), IsCardCleanupStage,
                onComplete: () => GameStart(0f).Forget(),
                onShown: () => HideCover(),
                showInstantly: true).Forget();
        }

        // 화면 덮개를 걷습니다. 이미 걷힌 상태면 바로 다음 동작으로 넘어갑니다.
        // (꺼져 있는 덮개에 Hide를 걸면 페이드가 끝나지 않아 콜백이 오지 않습니다.)
        private void HideCover(System.Action next = null)
        {
            var cover = CoverUIManager.Instance != null ? CoverUIManager.Instance.CoverUI : null;

            if (cover == null || !cover.gameObject.activeSelf)
            {
                next?.Invoke();

                return;
            }

            cover.Hide(next).Forget();
        }

        // 실제 게임 진행을 시작합니다.
        // 이 시점부터 몬스터가 스폰/이동하고, 플레이어가 광물을 탐색·이동·채굴합니다.
        // startDelay: 시작 전에 잠깐 두는 여유 시간(초).
        // 화면이 갑자기 바뀌지 않게 하는 장치라, 이미 긴 연출을 본 뒤에는 0으로 넣습니다.
        private async UniTaskVoid GameStart(float startDelay = 0.5f)
        {
            // 시작 연출이 끝날 때까지는 판이 진행되지 않게 멈춰 둡니다.
            // 스테이지가 끝난 순간부터 여기까지(강화 → 맵 → 카드 정리 → 화면 전환)
            // 계속 멈춰 있는 상태를 이어받아, 연출 뒤에서 몬스터가 먼저 움직이지 않습니다.
            SetGamePaused(true);

            if (startDelay > 0f)
                await UniTask.WaitForSeconds(startDelay);

            // 첫 진입 인트로는 StartFirstStage에서 덮개 위에 먼저 재생합니다.
            
            // 여기서부터 실제 진행입니다.
            SetGamePaused(false);

            uIController.GameStart();

            // 몬스터 스폰 루프 시작 (내부에서 몬스터 이동/공격도 함께 켜집니다)
            monsterController.ExecuteTestSpawn(_playerEntity, 0);

            // 광물 보충 루프 시작 (초기 배치는 InitAsync/Next에서 이미 끝난 상태)
            resourceController.ExecuteSpawn(_playerEntity);

            // 플레이어 행동 트리(광물 탐색 → 이동 → 채굴) 시작
            playerController.StartBehaviour();

            // 죽으면 같은 스테이지를 다시 시작합니다.
            // 실패 조건은 스태미나 하나로 통일했습니다.
            // 체력은 개편 전 시스템이라 사망으로 스테이지를 끝내지 않습니다.
            // (피격 피해는 SetDamagedHandler에서 스태미나로 처리됩니다.)

            // 피격 시 스태미나 소모, 스태미나가 바닥나면 스테이지 재시작
            playerController.SetDamagedHandler(() => uIController.ConsumeStaminaByHit());
            uIController.SetStaminaEmptyHandler(() => RestartStage());

            // 강화 팝업: 번 골드를 스테이지 사이에 쓰는 창구
            stageMapPopup?.Init();
            cardCleanupPopup?.Init();

            upgradePopup?.Init(
                getGold: () => uIController.Gold,
                trySpendGold: uIController.TrySpendGold,
                getLevel: row => levelUpController.BonusState.GetLevel(row.Id.ToString()),
                onPurchase: row =>
                {
                    levelUpController.ApplyBonus(row);

                    // 산 즉시 저장 — 강화 도중 앱이 꺼져도 산 것이 남습니다.
                    GameDataManager.Instance?.SaveUpgrade(
                        uIController.Gold, levelUpController.BonusState.GetAllLevels());
                });

            // 강화 도중에 앱이 꺼졌다면 그 화면부터 다시 보여줍니다.
            // (Init이 끝난 뒤여야 팝업이 골드·레벨을 조회할 수 있습니다.)
            if (_resumeUpgradePhase)
            {
                _resumeUpgradePhase = false;
                _isStageEnding = false;

                ShowUpgradeThen(_resumeUpgradeFromClear, () => Next(_resumeUpgradeFromClear));
            }

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
        // 테스트용 — 저장을 지우고 스테이지 1부터 다시 시작합니다.
        public void OnClickClearSave()
        {
            Manager.GameDataManager.Instance?.Clear();

            Debug.Log("[Save] 저장 삭제 — 씬을 다시 불러옵니다.");

            // 삭제만 하면 이미 메모리에 올라온 진행 상태가 그대로라
            // 씬을 새로 불러와 캐릭터 선택부터 다시 시작합니다.
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        // 테스트용 — 목표 채굴량을 채워 강제로 클리어시킵니다.
        public void OnClickForceClear()
        {
            // 스테이지가 끝나는 중(강화·카드 정리 화면)에는 무시합니다.
            // 연타하면 종료 처리가 겹쳐 다음 스테이지가 멈춥니다.
            if (!IsInitialized || _isStageEnding || _isRestarting)
                return;

            uIController.ForceCompleteMining();
        }

        // 테스트용 — 스태미나를 모두 소모시켜 강제로 실패시킵니다.
        public void OnClickForceFail()
        {
            if (!IsInitialized || _isStageEnding || _isRestarting)
                return;

            uIController.ForceDrainStamina();
        }

        // 이 스테이지를 시작하기 전에 카드 정리를 열지 판단합니다.
        // 주기가 3이면 3·6·9 스테이지에 들어가기 직전에 열립니다.
        private bool IsCardCleanupStage(int stage)
        {
            if (cardCleanupInterval <= 0)
                return false;

            if (stage > GetMaxStage())
                return false;

            return stage % cardCleanupInterval == 0;
        }

        // 맵이 화면을 덮고 있는 동안 카드 정리를 띄우고, 닫힐 때까지 기다립니다.
        // 이렇게 해야 맵이 걷히기 전에 카드 화면이 올라와 인게임이 드러나지 않습니다.
        private Cysharp.Threading.Tasks.UniTask ShowCardCleanupIfNeededAsync(int stage)
        {
            var completion = new Cysharp.Threading.Tasks.UniTaskCompletionSource();

            ShowCardCleanupIfNeeded(stage, () => completion.TrySetResult());

            return completion.Task;
        }

        // 들어갈 스테이지가 카드 정리 주기면 먼저 카드를 고르게 하고,
        // 아니면 그대로 다음 동작으로 넘어갑니다.
        private void ShowCardCleanupIfNeeded(int stage, System.Action next)
        {
            if (!IsCardCleanupStage(stage))
            {
                next?.Invoke();

                return;
            }

            ShowCardCleanupThen(next);
        }

        // 새 카드를 뽑아 덱과 함께 보여주고, 남길 카드로 덱을 교체합니다.
        private void ShowCardCleanupThen(System.Action next)
        {
            var deck = cardHandController?.Deck;

            if (cardCleanupPopup == null || deck == null)
            {
                next?.Invoke();

                return;
            }

            var rewards = MainGame.Card.SkillDeck.PickRandomRewards(cardRewardCount);

            if (rewards.Count == 0)
            {
                next?.Invoke();

                return;
            }

            SetGamePaused(true);

            cardCleanupPopup.Show(deck.GetDeckCards(), rewards, GetDeckSize(), cards =>
            {
                deck.SetDeckCards(cards);

                next?.Invoke();
            });

            // 카드 화면이 화면 전체를 덮으므로 스테이지 맵은 여기서 내립니다.
            // 같은 프레임에 처리하므로 인게임이 드러나는 순간은 없습니다.
            stageMapPopup?.Hide();
        }

        // 유지할 덱 장수. 상수 테이블에서 읽습니다.
        private int GetDeckSize()
        {
            var table = Manager.DataTableManager.Instance?.GameConstantDataTable;

            return table != null ? table.GetInt(EGameConstantType.CardDeckSize, 10) : 10;
        }

        // 마지막 스테이지 번호. 상수 테이블에서 읽고, 없으면 인스펙터 값을 씁니다.
        private int GetMaxStage()
        {
            var table = Manager.DataTableManager.Instance?.GameConstantDataTable;
            var value = table != null ? table.GetValue(EGameConstantType.MaxStage, maxStage) : maxStage;

            return Mathf.Max(1, Mathf.RoundToInt(value));
        }

        // 데모 종료 — 안내를 보여주고 저장을 지운 뒤 시작 씬으로 돌아갑니다.
        private void ShowDemoClear()
        {
            SetGamePaused(true);

            // 마지막 클리어 보상은 지급해 성과 표시가 어색하지 않게 합니다.
            uIController.AddGold(stageClearGold + (uIController.Stage - 1) * stageClearGoldPerStage);

            if (demoClearPopup == null)
            {
                FinishDemo();

                return;
            }

            demoClearPopup.Show(uIController.Stage, uIController.Gold, FinishDemo);
        }

        private void FinishDemo()
        {
            // 런이 끝났으므로 진행 상태를 지웁니다. 다음 실행은 캐릭터 선택부터 시작합니다.
            Manager.GameDataManager.Instance?.Clear();

            UnityEngine.SceneManagement.SceneManager.LoadScene(startSceneName);
        }

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

            // 스테이지가 끝난 순간 바로 멈춥니다.
            // 강화·맵 연출·카드 정리가 이어지는 동안 다음 스테이지가 시작될 때까지
            // 이 정지를 그대로 유지합니다(푸는 곳은 GameStart).
            SetGamePaused(true);

            // 마지막 스테이지를 깼으면 강화 대신 데모 종료로 갑니다.
            // (더 진행할 스테이지가 없어 강화를 해도 쓸 곳이 없습니다.)
            if (isCleared && uIController.Stage >= GetMaxStage())
            {
                ShowDemoClear();

                return;
            }

            // 클리어 보상은 성공했을 때만 줍니다.
            // (실패해도 그 판에서 번 골드는 남으므로, 이 차이가 성공의 값어치가 됩니다.)
            if (isCleared)
                uIController.AddGold(stageClearGold + (uIController.Stage - 1) * stageClearGoldPerStage);

            // 스테이지가 끝난 시점(클리어·실패 모두)에 저장합니다.
            Manager.GameDataManager.Instance?.SaveStageEnd(
                uIController.Stage, uIController.Gold, levelUpController.GetCharacterId(),
                levelUpController.BonusState.GetAllLevels(), isCleared);

            // 팝업은 스테이지가 끝날 때마다 띄웁니다.
            // (살 게 없다고 건너뛰면 강화 창구가 있다는 것 자체를 알 수 없고,
            //  얼마를 더 모아야 하는지도 확인할 수 없습니다.)
            if (upgradePopup == null)
            {
                next?.Invoke();

                return;
            }

            SetGamePaused(true);

            upgradePopup.Show(uIController.Stage, isCleared, () =>
            {
                // 강화 페이즈가 끝났음을 남깁니다.
                // 클리어면 다음 스테이지, 실패면 같은 스테이지를 다시 합니다.
                var nextStage = isCleared ? uIController.Stage + 1 : uIController.Stage;

                Manager.GameDataManager.Instance?.SaveUpgradePhaseEnd(nextStage);

                // 카드 정리는 맵 연출이 끝난 뒤,
                // 다음 스테이지가 시작되기 직전에 뜹니다(Next 참고).

                next?.Invoke();
            });
        }

        [SerializeField]
        private UpgradePopup upgradePopup;

        [SerializeField]
        [Tooltip("마지막 스테이지를 깼을 때 뜨는 데모 종료 안내")]
        private DemoClearPopup demoClearPopup;

        [SerializeField]
        [Tooltip("스테이지 사이에 잠깐 지나가는 맵 연출")]
        private StageMapPopup stageMapPopup;

        [SerializeField]
        [Tooltip("카드 정리 화면(정해진 스테이지마다 뜹니다)")]
        private CardCleanupPopup cardCleanupPopup;

        [SerializeField]
                [Tooltip("카드 정리가 열리는 주기. 3이면 3, 6, 9 스테이지가 시작되기 직전")]
        private int cardCleanupInterval = 3;

        [SerializeField]
        [Tooltip("한 번에 제시할 새 카드 장수")]
        private int cardRewardCount = 3;

        [SerializeField]
        [Tooltip("상수 테이블에 값이 없을 때 쓸 마지막 스테이지 번호")]
        private int maxStage = 10;

        [SerializeField]
        [Tooltip("데모 종료 후 돌아갈 씬 이름")]
        private string startSceneName = "StartScene";

        [SerializeField]
        [Tooltip("스테이지를 클리어했을 때 주는 기본 골드. 실패하면 주지 않습니다.")]
        private int stageClearGold = 100;

        [SerializeField]
        [Tooltip("스테이지가 오를 때마다 클리어 보상에 더해지는 골드")]
        private int stageClearGoldPerStage = 30;

        // 테스트 버튼으로 건 수동 일시정지 상태
        private bool _isManualPaused;

        private bool _isRestarting;

        // 스테이지가 끝나는 중인지. 클리어와 스태미나 소진이 같은 프레임에 겹치면
        // 팝업이 두 번 뜨거나 결과가 뒤집히므로 첫 번째만 받습니다.
        private bool _isStageEnding;

        // 강화 도중 앱이 꺼졌을 때 그 화면부터 다시 시작하기 위한 플래그
        private bool _resumeUpgradePhase;
        private bool _resumeUpgradeFromClear;


        // 맵 연출을 거친 뒤 실제 전환을 진행합니다.
        // 클리어로 다음 칸에 갈 때만 보여줍니다. 실패 재도전은 같은 칸에 머무르므로
        // 같은 연출을 반복해서 보여줄 이유가 없습니다.
        public void Next(bool advanceStage = true)
        {
            if (!IsInitialized)
                return;

            // 맵 연출·카드 정리가 도는 동안에도 판은 멈춰 있어야 합니다.
            // (강화 팝업을 거치지 않고 불리는 경우를 대비해 여기서도 한 번 더 걸어 둡니다.)
            SetGamePaused(true);

            if (advanceStage && stageMapPopup != null)
            {
                var from = uIController.Stage;
                var to = Mathf.Min(from + 1, GetMaxStage());

                // 맵이 화면을 덮고 있는 동안 판을 정리하고 다음 스테이지를 깔아둡니다.
                // 따로 화면 덮개를 또 쓰면 맵이 걷힐 때 다시 어두워져 전환이 두 번 보입니다.
                // 맵 자체가 덮개 역할을 합니다.
                //
                // 그 스테이지가 카드 정리 주기면 맵이 걷힌 뒤 카드를 먼저 고르게 합니다.
                stageMapPopup.PlayAsync(from, to, GetMaxStage(), IsCardCleanupStage,
                    onComplete: () => GameStart(0f).Forget(),
                    onShown: () => PrepareNextStage(true),
                    onBeforeHide: () => ShowCardCleanupIfNeededAsync(to)).Forget();

                return;
            }

            NextInternal(advanceStage);
        }

        // 맵 연출을 거치지 않는 경우(실패 후 같은 스테이지 재도전 등)의 전환.
        // 가려줄 연출이 없으므로 화면 덮개를 씁니다.
        private void NextInternal(bool advanceStage)
        {
            if (!IsInitialized)
                return;

            CoverUIManager.Instance.CoverUI.Show(() =>
            {
                PrepareNextStage(advanceStage);

                // 캐릭터를 아직 고르지 않았다면(=씬 첫 시작) 선택 팝업부터 띄웁니다.
                if (!levelUpController.HasCharacter)
                {
                    uIController.ShowCharacterSelect(row =>
                    {
                        levelUpController.SetCharacter(row);
                        Manager.GameDataManager.Instance?.SaveCharacter(row?.Id);

                        HideCover(() => GameStart().Forget());
                    });
                }
                else
                {
                    HideCover(() => GameStart().Forget());
                }
            }).Forget();
        }

        // 화면이 가려져 있는 동안 할 정리와 다음 스테이지 준비.
        // 지금까지 스폰된 몬스터·광물을 모두 풀로 되돌리고 처음 상태로 다시 깔아둡니다.
        private void PrepareNextStage(bool advanceStage)
        {
            // 강화 팝업은 지금까지 떠 있었습니다. 화면이 가려진 지금 내립니다.
            // (닫기를 누를 때 바로 내리면 인게임이 잠깐 드러납니다 — UpgradePopup.Close 참고)
            upgradePopup?.Hide();

            // 이전 카드가 밝아질 때 보이지 않도록 손패를 감춥니다.
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

            // 재시작 시 수동 일시정지는 해제합니다.
            _isManualPaused = false;

            UpdatePauseButtonText();

            // 플레이어 행동 정지 + 진행 중이던 채굴/타겟 초기화
            // (방금 풀로 되돌린 광물을 계속 때리는 것을 방지합니다.)
            playerController.StopBehaviour();

            // 체력과 무적/다운 상태도 초기화합니다.
            playerController.ResetPlayerHealth();

            // 광물만 다시 깔아둡니다. 실제 진행 재개는 GameStart에서 합니다.
            resourceController.SpawnInitialLayout(Vector3.zero);

            WarpPlayer(Vector3.zero);

            uIController.ResetStage(advanceStage);
        }
    }
}
