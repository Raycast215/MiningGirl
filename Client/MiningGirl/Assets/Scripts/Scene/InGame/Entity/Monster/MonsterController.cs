using System.Threading;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace MainGame.Entity.Monster
{
    public class MonsterController : EntityControllerBase<Monster>, IMonsterDeathHandler
    {
        [SerializeField]
        private string prefabName = "Monster";

        private IMonsterStatProvider _statProvider;
        private IStageMonsterModifier _stageModifier;
        private IRiskCardMonsterModifier _riskModifier;
        private IFloatingDamagePresenter _damagePresenter;

        // 스폰 루프를 중간에 멈췄다가 다시 시작할 수 있도록 자체 취소 토큰을 관리합니다.
        private CancellationTokenSource _spawnCts;

        // 지금은 임시 구현체로 채워서 사용하고, 실제 시스템(엑셀 데이터 / 카드 시스템)이
        // 완성되면 이 Setup 호출부만 실제 구현체로 바꿔주면 됩니다.
        public void Setup(
            IMonsterStatProvider statProvider = null,
            IStageMonsterModifier stageModifier = null,
            IRiskCardMonsterModifier riskModifier = null,
            IFloatingDamagePresenter damagePresenter = null)
        {
            _statProvider = statProvider ?? new TempMonsterStatProvider();
            _stageModifier = stageModifier ?? new TempStageMonsterModifier();
            _riskModifier = riskModifier ?? new TempRiskCardMonsterModifier();
            _damagePresenter = damagePresenter;
        }

        public async UniTaskVoid InitControllerAsync()
        {
            if (IsInitialized)
                return;

            if (_statProvider == null)
                Setup();

            InitAsync(prefabName, 10).Forget();
            await UniTask.WaitUntil(() => IsInitialized);
        }

        // 몬스터 1마리를 스폰합니다. target은 보통 0,0에 있는 플레이어입니다.
        public async UniTask<Monster> Spawn(string monsterId, Vector3 position, IEntity target, int stageIndex)
        {
            var baseStat = _statProvider.GetBaseStat(monsterId);

            var monster = await Get();
            monster.Setup(this, baseStat, _stageModifier, _riskModifier, stageIndex, target, _damagePresenter, this);
            monster.SetPosition(position);
            monster.SetActiveObject(true);

            if (!monster.IsInitialized)
                monster.InitAsync().Forget();

            return monster;
        }

        [Header("Test Spawn")]
        [SerializeField]
        private float testSpawnOffscreenMargin = 1f;
        [SerializeField]
        private float testSpawnInterval = 2f;
        [SerializeField]
        private int testMaxSpawnCount = 30;

        // 테스트용 스폰 루프 — 현재 살아있는 몬스터 수를 확인해서, 최대치(testMaxSpawnCount) 미만이면
        // testSpawnInterval 간격으로 계속 채워 넣습니다. 몬스터가 죽어 풀에 반환되면 빈 자리가 생기고,
        // 이 루프가 그 자리를 다시 채우므로 상시 일정 수의 몬스터가 유지됩니다.
        public void ExecuteTestSpawn(IEntity target, int stageIndex)
        {
            // 이전 루프가 돌고 있다면 정리하고 새로 시작합니다.
            StopSpawn();
            _spawnCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            SpawnLoop(target, stageIndex, _spawnCts.Token).Forget();
        }

        // 현재 살아있는 몬스터 수를 확인해서, 최대치(testMaxSpawnCount) 미만이면
        // testSpawnInterval 간격으로 계속 채워 넣습니다. 몬스터가 죽어 풀에 반환되면 빈 자리가 생기고,
        // 이 루프가 그 자리를 다시 채우므로 상시 일정 수의 몬스터가 유지됩니다.
        // 토큰이 취소되면(=StopSpawn / 오브젝트 파괴) 루프가 안전하게 종료됩니다.
        private async UniTaskVoid SpawnLoop(IEntity target, int stageIndex, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (ActivateList != null && ActivateList.Count < testMaxSpawnCount)
                {
                    var pos = GetRandomOffscreenPosition(target.GetPosition());
                    await Spawn("Slime", pos, target, stageIndex);
                }

                await UniTask.WaitForSeconds(testSpawnInterval, cancellationToken: token);
            }
        }

        // 스폰 루프를 멈추고, 지금까지 활성화된 몬스터를 모두 풀로 되돌립니다.
        public void StopSpawn()
        {
            if (_spawnCts != null)
            {
                _spawnCts.Cancel();
                _spawnCts.Dispose();
                _spawnCts = null;
            }

            // 현재 살아있는 몬스터 전부를 풀로 반환합니다.
            Clear();
        }

        // 현재 해상도(카메라 뷰) 바깥쪽 테두리 중 임의의 한 지점에 스폰합니다.
        private Vector3 GetRandomOffscreenPosition(Vector3 center)
        {
            var cam = Camera.main;
            var halfHeight = cam.orthographicSize + testSpawnOffscreenMargin;
            var halfWidth = halfHeight * cam.aspect + testSpawnOffscreenMargin;

            float x, y;

            switch (Random.Range(0, 4))
            {
                case 0: // 위
                    x = Random.Range(-halfWidth, halfWidth);
                    y = halfHeight;
                    break;
                case 1: // 아래
                    x = Random.Range(-halfWidth, halfWidth);
                    y = -halfHeight;
                    break;
                case 2: // 왼쪽
                    x = -halfWidth;
                    y = Random.Range(-halfHeight, halfHeight);
                    break;
                default: // 오른쪽
                    x = halfWidth;
                    y = Random.Range(-halfHeight, halfHeight);
                    break;
            }

            return center + new Vector3(x, y, 0f);
        }

        // IMonsterDeathHandler 구현 — 몬스터가 죽으면 호출되어 풀로 반환합니다.
        public void OnMonsterDeath(Monster monster)
        {
            Return(monster);
        }

        public int GetSpawnCountBonus()
        {
            return _riskModifier?.GetExtraSpawnCount() ?? 0;
        }

        public float GetSpawnIntervalMultiplier()
        {
            var reduceRate = _riskModifier?.GetSpawnIntervalRate() ?? 0f;
            return 1f - reduceRate;
        }

        public bool RollGradeUp()
        {
            var rate = _riskModifier?.GetGradeUpRate() ?? 0f;
            return Random.value < rate;
        }

        private void Update()
        {
            UpdateEntity();
            UpdateMonsterVisibility();
        }

        // 카메라 뷰 밖에 있는 몬스터는 렌더러를 꺼서 그리기 비용을 아낍니다.
        // (스폰 자체를 화면 밖에서 시작하도록 바꿨기 때문에 특히 도움이 됩니다.)
        private void UpdateMonsterVisibility()
        {
            if (ActivateList == null || ActivateList.Count == 0)
                return;

            var cam = Camera.main;
            
            if (cam == null)
                return;

            var halfHeight = cam.orthographicSize;
            var halfWidth = halfHeight * cam.aspect;
            var camPos = cam.transform.position;

            foreach (var monster in ActivateList)
            {
                var pos = monster.GetPosition();
                var isVisible = Mathf.Abs(pos.x - camPos.x) <= halfWidth
                                 && Mathf.Abs(pos.y - camPos.y) <= halfHeight;

                monster.SetRendererVisible(isVisible);
            }
        }
    }
}