using System.Threading;
using Cysharp.Threading.Tasks;
using Legacy.Scene.InGame.Entity;
using Legacy.Scene.InGame.Entity.Interface;
using Legacy.Scene.InGame.Entity.Spatial;
using UnityEngine;

namespace Legacy.MainGame.Entity.Monster
{
    public class MonsterController : EntityControllerBase<Monster>, IMonsterDeathHandler
    {
        [SerializeField]
        private string prefabName = "Monster";

        [SerializeField]
        [Tooltip("몬스터끼리 근접 판정용 격자 셀 크기. 판정 반경(겹침 보정 1.45)보다 커야 합니다")]
        private float neighborCellSize = 1.6f;

        [SerializeField]
        [Tooltip("광물 회피 판정용 격자 셀 크기. 회피 반경(3.5)보다 커야 합니다")]
        private float obstacleCellSize = 3.6f;

        // 프레임당 한 번 만들어 두고 몬스터들이 조회만 합니다.
        // 전수 비교(4n²)는 500마리에서 210ms였는데, 3x3 셀 조회는 1000마리에서도 1ms 미만입니다.
        private readonly SpatialHashGrid _neighborGrid = new SpatialHashGrid();
        private readonly SpatialHashGrid _obstacleGrid = new SpatialHashGrid();

        // 격자를 만들면서 읽은 위치를 그대로 재사용합니다.
        // (가시성 판정이 GetPosition을 다시 부르면 프레임당 n번 네이티브 호출이 또 생깁니다.)
        private readonly System.Collections.Generic.List<Monster> _snapshot =
            new System.Collections.Generic.List<Monster>();
        private readonly System.Collections.Generic.List<Vector3> _snapshotPositions =
            new System.Collections.Generic.List<Vector3>();

        // Camera.main은 태그 조회가 들어가므로 캐시합니다.
        private Camera _mainCamera;

        public SpatialHashGrid NeighborGrid
        {
            get { return _neighborGrid; }
        }

        public SpatialHashGrid ObstacleGrid
        {
            get { return _obstacleGrid; }
        }
        private IMonsterStatProvider _statProvider;
        private IStageMonsterModifier _stageModifier;
        private IRiskCardMonsterModifier _riskModifier;
        private IFloatingDamagePresenter _damagePresenter;
        private Legacy.Scene.InGame.Entity.Resource.IResourceProvider _resourceProvider;


        // GameStart() 전에는 몬스터가 움직이지 않도록 행동 트리 구동을 막습니다.
        private bool _isBehaviourRunning;

        // 스폰 루프를 중간에 멈췄다가 다시 시작할 수 있도록 자체 취소 토큰을 관리합니다.
        private CancellationTokenSource _spawnCts;

        // 지금은 임시 구현체로 채워서 사용하고, 실제 시스템(엑셀 데이터 / 카드 시스템)이
        // 완성되면 이 Setup 호출부만 실제 구현체로 바꿔주면 됩니다.
        public void Setup(
            IMonsterStatProvider statProvider = null,
            IStageMonsterModifier stageModifier = null,
            IRiskCardMonsterModifier riskModifier = null,
            IFloatingDamagePresenter damagePresenter = null,
            Legacy.Scene.InGame.Entity.Resource.IResourceProvider resourceProvider = null)
        {
            _statProvider = statProvider ?? new TempMonsterStatProvider();
            _stageModifier = stageModifier ?? new TempStageMonsterModifier();
            _riskModifier = riskModifier ?? new TempRiskCardMonsterModifier();
            _damagePresenter = damagePresenter;
            _resourceProvider = resourceProvider;
        }

        // GetNearbyAvoidTargets()가 매 프레임 새 리스트를 만들지 않도록 재사용하는 버퍼입니다.
        private readonly System.Collections.Generic.List<IEntity> _avoidBuffer =
            new System.Collections.Generic.List<IEntity>();

        // 몬스터가 이동 중 피해야 할 장애물(광물) 목록을 반환합니다.
        // 몬스터끼리의 겹침 보정과는 별개로, MoveNode의 장애물 회피 파라미터(더 넓은 반경/강한 힘)가 적용됩니다.
        public System.Collections.Generic.IReadOnlyList<IEntity> GetObstacles()
        {
            _avoidBuffer.Clear();

            var resources = _resourceProvider?.GetActiveResources();
            if (resources != null)
            {
                for (var i = 0; i < resources.Count; i++)
                    _avoidBuffer.Add(resources[i]);
            }

            return _avoidBuffer;
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
        // 몬스터를 처치했을 때 골드를 지급할 대상(InGameController가 넘겨줍니다).
        private System.Action<int> _onMonsterKilled;

        public void SetKilledHandler(System.Action<int> handler)
        {
            _onMonsterKilled = handler;
        }

        public async UniTask<Monster> Spawn(string monsterId, Vector3 position, IEntity target, int stageIndex)
        {
            var baseStat = _statProvider.GetBaseStat(monsterId);

            var monster = await Get();
            if (monster == null)
                return null;

            monster.Setup(this, baseStat, _stageModifier, _riskModifier, stageIndex, target, _damagePresenter, this);
            monster.SetKilledHandler(_onMonsterKilled);
            monster.SetPosition(position);
            monster.SetActiveObject(true);

            if (!monster.IsInitialized)
                monster.InitAsync().Forget();

            return monster;
        }

        [Header("Test Spawn")]
        [SerializeField]
        [Tooltip("화면 절반 크기 대비 비율로 여백을 둡니다. 예: 0.5 = 화면 절반 크기의 50%만큼 화면 밖에서 스폰. " +
                 "절대 유닛 값 대신 비율을 쓰는 이유: 화면 비율(세로로 좁은 폰 vs 정사각형에 가까운 태블릿)이 달라도 " +
                 "좌우/상하 진입 타이밍이 기기와 무관하게 일관되게 느껴지도록 하기 위함입니다.")]
        private float testSpawnOffscreenMarginRatio = 0.5f;
        [SerializeField]
        private float testSpawnInterval = 2f;
        [SerializeField]
        private int testMaxSpawnCount = 30;   // 상수 테이블이 없을 때만 쓰이는 폴백
        [SerializeField]
        [Tooltip("상수 테이블이 없을 때 쓸, 스테이지마다 늘어나는 몬스터 수")]
        private int spawnCountPerStage = 5;

        // 스폰 간격(게임 상수 테이블, 없으면 인스펙터 값)
        private float GetSpawnInterval()
        {
            var table = Manager.DataTableManager.Instance?.GameConstantDataTable;

            return table != null ? table.GetValue(EGameConstantType.MonsterSpawnInterval, testSpawnInterval) : testSpawnInterval;
        }

        // 강화로 올린 최대 소환 수 보너스. InGameController가 넣어줍니다.
        private System.Func<int> _maxCountBonusProvider;

        public void SetMaxCountBonusProvider(System.Func<int> provider)
        {
            _maxCountBonusProvider = provider;
        }

        // 지금 몇 번째 스테이지인지(1부터). 스테이지가 오를수록 몬스터가 더 많이 나옵니다.
        private System.Func<int> _stageProvider;

        public void SetStageProvider(System.Func<int> provider)
        {
            _stageProvider = provider;
        }

        // 몬스터 최대 소환 수 = 기본값 + 스테이지 가산 + 강화 보너스.
        // 스폰 간격마다 한 마리씩 이 수까지 채우고, 죽어서 빈자리가 생기면 다시 채웁니다.
        private int GetMaxSpawnCount()
        {
            var table = Manager.DataTableManager.Instance?.GameConstantDataTable;
            var count = table != null ? table.GetInt(EGameConstantType.MonsterSpawnCount, testMaxSpawnCount) : testMaxSpawnCount;

            // 스테이지 1은 가산 0. 그래서 (스테이지 - 1)을 곱합니다.
            var stage = _stageProvider != null ? _stageProvider.Invoke() : 1;
            var perStage = table != null ? table.GetInt(EGameConstantType.MonsterSpawnCountPerStage, spawnCountPerStage) : spawnCountPerStage;

            count += Mathf.Max(0, stage - 1) * perStage;

            if (_maxCountBonusProvider != null)
                count += _maxCountBonusProvider.Invoke();

            return Mathf.Max(0, count);
        }

        // 테스트용 스폰 루프 — 현재 살아있는 몬스터 수를 확인해서, 최대치 미만이면
        // testSpawnInterval 간격으로 계속 채워 넣습니다. 몬스터가 죽어 풀에 반환되면 빈 자리가 생기고,
        // 이 루프가 그 자리를 다시 채우므로 상시 일정 수의 몬스터가 유지됩니다.
        public void ExecuteTestSpawn(IEntity target, int stageIndex)
        {
            // 이전 루프가 돌고 있다면 정리하고 새로 시작합니다.
            StopSpawn();

            // 스폰 시작 = 몬스터 이동/공격 시작
            _isBehaviourRunning = true;
            _spawnCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            SpawnLoop(target, stageIndex, _spawnCts.Token).Forget();
        }

        // 현재 살아있는 몬스터 수를 확인해서, 최대치 미만이면
        // testSpawnInterval 간격으로 계속 채워 넣습니다. 몬스터가 죽어 풀에 반환되면 빈 자리가 생기고,
        // 이 루프가 그 자리를 다시 채우므로 상시 일정 수의 몬스터가 유지됩니다.
        // 토큰이 취소되면(=StopSpawn / 오브젝트 파괴) 루프가 안전하게 종료됩니다.
        private async UniTaskVoid SpawnLoop(IEntity target, int stageIndex, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // 먼저 기다린 뒤 스폰합니다. 시작하자마자 한 마리가 나오지 않고
                // 첫 몬스터도 간격만큼 지난 뒤에 등장합니다.
                await UniTask.WaitForSeconds(GetSpawnInterval(), cancellationToken: token);

                if (!_isBehaviourRunning || ActivateList == null || ActivateList.Count >= GetMaxSpawnCount())
                    continue;

                var pos = GetRandomOffscreenPosition(target.GetPosition());

                await Spawn("Slime", pos, target, stageIndex);
            }
        }

        // 팝업 등으로 잠시 멈출 때 사용합니다. 스폰과 이동이 모두 멈추고, 활성 몬스터는 유지됩니다.
        public void SetBehaviourPaused(bool paused)
        {
            _isBehaviourRunning = !paused;
        }

        // 스폰 루프를 멈추고, 지금까지 활성화된 몬스터를 모두 풀로 되돌립니다.
        public void StopSpawn()
        {
            _isBehaviourRunning = false;

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
        // 여백은 절대 유닛이 아니라 각 축(가로/세로) 자기 화면 크기의 비율로 계산해서,
        // 화면 비율이 다른 기기(폰/태블릿)에서도 진입 타이밍이 비슷하게 느껴지도록 합니다.
        private Vector3 GetRandomOffscreenPosition(Vector3 center)
        {
            var cam = Camera.main;
            var screenHalfHeight = cam.orthographicSize;
            var screenHalfWidth = screenHalfHeight * cam.aspect;

            var halfHeight = screenHalfHeight * (1f + testSpawnOffscreenMarginRatio);
            var halfWidth = screenHalfWidth * (1f + testSpawnOffscreenMarginRatio);

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

        // 근접 판정용 격자를 프레임당 한 번만 다시 만듭니다.
        //
        // 여기서 각 개체의 위치를 딱 한 번 읽어 배열에 담아둡니다. 예전에는 판정 루프
        // 안쪽에서 IEntity.GetPosition()을 4n²번 불렀는데, 실제 비용의 대부분이 그 호출이었습니다.
        // (156마리 기준 29ms -> 1.4ms)
        private void RebuildGrids()
        {
            _neighborGrid.SetCellSize(neighborCellSize);
            _neighborGrid.Clear();
            _snapshot.Clear();
            _snapshotPositions.Clear();

            var monsters = ActivateList;

            if (monsters != null)
            {
                for (var i = 0; i < monsters.Count; i++)
                {
                    var monster = monsters[i];

                    if (monster == null || !monster.GetActiveState())
                        continue;

                    var pos = monster.GetPosition();
                    pos.z = 0f;

                    _neighborGrid.Add(monster, pos);
                    _snapshot.Add(monster);
                    _snapshotPositions.Add(pos);
                }
            }

            _obstacleGrid.SetCellSize(obstacleCellSize);
            _obstacleGrid.Clear();

            var resources = _resourceProvider == null ? null : _resourceProvider.GetActiveResources();

            if (resources != null)
            {
                for (var i = 0; i < resources.Count; i++)
                {
                    var resource = resources[i];

                    if (resource == null || !resource.GetActiveState())
                        continue;

                    _obstacleGrid.Add(resource, resource.GetPosition());
                }
            }
        }

        private void Update()
        {
            // 정지 중에도 가시성 처리가 같은 스냅샷을 쓰므로 항상 갱신합니다.
            RebuildGrids();

            // GameStart() 전에는 몬스터가 움직이지 않도록 행동 트리 구동을 막습니다.
            // (렌더러 가시성 처리는 정지 중에도 계속 적용합니다.)
            if (_isBehaviourRunning)
            {
                UpdateEntity();
            }
            else if (ActivateList != null)
            {
                // 정지 중에는 남은 속도로 미끄러지지 않도록 멈춥니다.
                foreach (var monster in ActivateList)
                    monster.StopMove();
            }

            UpdateMonsterVisibility();
        }

        // 카메라 뷰 밖에 있는 몬스터는 렌더러를 꺼서 그리기 비용을 아낍니다.
        // (스폰 자체를 화면 밖에서 시작하도록 바꿨기 때문에 특히 도움이 됩니다.)
        private void UpdateMonsterVisibility()
        {
            if (_snapshot.Count == 0)
                return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            var halfHeight = _mainCamera.orthographicSize;
            var halfWidth = halfHeight * _mainCamera.aspect;
            var camPos = _mainCamera.transform.position;

            for (var i = 0; i < _snapshot.Count; i++)
            {
                // RebuildGrids에서 이미 읽어 둔 위치입니다(네이티브 호출 없음).
                var pos = _snapshotPositions[i];
                var isVisible = Mathf.Abs(pos.x - camPos.x) <= halfWidth
                                 && Mathf.Abs(pos.y - camPos.y) <= halfHeight;

                _snapshot[i].SetRendererVisible(isVisible);
            }
        }
    }
}
