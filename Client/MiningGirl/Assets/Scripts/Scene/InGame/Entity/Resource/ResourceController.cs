using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MainGame.Entity;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity.Resource
{
    public class ResourceController : EntityControllerBase<Resource>, IResourceDepletedHandler, IResourceProvider
    {
        [SerializeField]
        private string prefabName = "Stone";

        [Header("Reward")]
        [SerializeField]
        private float resourceMaxHp = 100f;
        [SerializeField]
        private int stoneReward = 1;
        [SerializeField]
        private int expReward = 1;

        [Header("Spawn")]
        [SerializeField]
        private int initialCount = 10;
        [SerializeField]
        private int maxCount = 30;
        [SerializeField]
        private float spawnInterval = 2f;
        [SerializeField]
        private float minDistanceBetween = 2f;
        [Tooltip("화면 밖 스폰 시, 화면 절반 크기 대비 얼마나 더 밖으로 나가서 스폰할지 비율입니다. " +
                 "절대 유닛이 아니라 비율을 쓰는 이유는 폰/태블릿 등 화면 비율이 달라도 일관되게 화면 밖에 스폰되도록 하기 위함입니다.")]
        [SerializeField]
        private float offscreenMarginRatio = 0.15f;

        private IFloatingDamagePresenter _damagePresenter;
        private IResourceRewardHandler _rewardHandler;

        // 스폰 루프를 중간에 멈췄다가 다시 시작할 수 있도록 자체 취소 토큰을 관리합니다.
        private CancellationTokenSource _spawnCts;

        // 지금은 재화 시스템이 없어서 null로 두면 보상 알림이 조용히 무시됩니다.
        // 실제 재화/인벤토리 시스템이 생기면 rewardHandler로 구현체를 주입해주면 됩니다.
        public void Setup(
            IFloatingDamagePresenter damagePresenter = null,
            IResourceRewardHandler rewardHandler = null)
        {
            _damagePresenter = damagePresenter;
            _rewardHandler = rewardHandler;
        }

        public async UniTaskVoid InitControllerAsync()
        {
            if (IsInitialized)
                return;

            InitAsync(prefabName, initialCount).Forget();
            await UniTask.WaitUntil(() => IsInitialized);
        }

        // 게임 시작 시점 — 화면 안(카메라 뷰)에 서로 겹치지 않게 초기 광물을 배치합니다.
        public void SpawnInitialLayout(Vector3 center)
        {
            var posList = GetRandomPositionsOnScreen(center, initialCount, minDistanceBetween);

            foreach (var pos in posList)
                Spawn(pos).Forget();
        }

        // 광물 1개를 스폰(또는 풀에서 재사용)합니다.
        public async UniTask<Resource> Spawn(Vector3 position)
        {
            var resource = await Get();
            resource.Setup(this, resourceMaxHp, stoneReward, expReward, _damagePresenter, this);
            resource.SetPosition(position);
            resource.SetActiveObject(true);

            if (!resource.IsInitialized)
                resource.InitAsync().Forget();

            return resource;
        }

        // target(보통 플레이어) 기준으로, 광물이 maxCount 미만이면 spawnInterval 간격으로
        // 화면 밖 랜덤 위치에 하나씩(기존 광물과 겹치지 않게) 채워 넣습니다.
        public void ExecuteSpawn(IEntity target)
        {
            // 이전 루프만 취소하고, 이미 배치된 광물(초기 배치 등)은 그대로 둡니다.
            // (StopSpawn()과 달리 Clear()를 호출하지 않음 — 여기서 Clear()까지 하면
            //  SpawnInitialLayout() 직후 ExecuteSpawn()을 호출할 때 방금 배치한 초기 광물이
            //  곧바로 풀로 되돌아가버리는 문제가 있었습니다.)
            _spawnCts?.Cancel();
            _spawnCts?.Dispose();
            _spawnCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            SpawnLoop(target, _spawnCts.Token).Forget();
        }

        private async UniTaskVoid SpawnLoop(IEntity target, CancellationToken token)
        {
            var minDistanceSqr = minDistanceBetween * minDistanceBetween;

            while (!token.IsCancellationRequested)
            {
                if (ActivateList != null && ActivateList.Count < maxCount)
                {
                    var pos = GetRandomOffscreenPosition(target.GetPosition(), minDistanceSqr);
                    if (pos.HasValue)
                        await Spawn(pos.Value);
                }

                await UniTask.WaitForSeconds(spawnInterval, cancellationToken: token);
            }
        }

        // 스폰 루프를 멈추고, 지금까지 활성화된 광물을 모두 풀로 되돌립니다.
        public void StopSpawn()
        {
            if (_spawnCts != null)
            {
                _spawnCts.Cancel();
                _spawnCts.Dispose();
                _spawnCts = null;
            }

            Clear();
        }

        // IResourceDepletedHandler 구현 — 광물이 다 캐이면 호출되어 풀로 반환합니다.
        public void OnResourceDepleted(Resource resource)
        {
            Return(resource);
        }

        // GetActiveResources()가 매번 새 리스트를 만들지 않도록 재사용하는 버퍼입니다.
        private readonly List<IEntity> _activeResourceBuffer = new List<IEntity>();

        // IResourceProvider 구현 — 현재 활성 광물 목록을 IEntity 형태로 제공합니다.
        // (플레이어의 SearchTargetNode가 가장 가까운 광물을 찾는 데 사용합니다.)
        public IReadOnlyList<IEntity> GetActiveResources()
        {
            _activeResourceBuffer.Clear();

            if (ActivateList != null)
            {
                foreach (var resource in ActivateList)
                    _activeResourceBuffer.Add(resource);
            }

            return _activeResourceBuffer;
        }

        // Resource.Hit()에서 채굴 완료 시 호출됩니다. 실제 지급 여부는 rewardHandler 쪽 책임입니다.
        public void NotifyReward(int stoneAmount, int expAmount)
        {
            _rewardHandler?.OnResourceMined(stoneAmount, expAmount);
        }

        // 화면 안(카메라 뷰 사각형) 임의 위치를 count개, 서로 minDistanceBetweenPoints 이상 떨어지게 생성합니다.
        private List<Vector3> GetRandomPositionsOnScreen(Vector3 center, int count, float minDistanceBetweenPoints, int maxTryPerPoint = 25)
        {
            var positions = new List<Vector3>(Mathf.Max(0, count));

            if (count <= 0)
                return positions;

            var cam = Camera.main;
            if (cam == null)
                return positions;

            var halfHeight = cam.orthographicSize;
            var halfWidth = halfHeight * cam.aspect;

            maxTryPerPoint = Mathf.Max(1, maxTryPerPoint);
            var minDistanceSqr = minDistanceBetweenPoints * minDistanceBetweenPoints;

            for (var i = 0; i < count; i++)
            {
                var placed = false;

                for (var attempt = 0; attempt < maxTryPerPoint; attempt++)
                {
                    var x = Random.Range(-halfWidth, halfWidth);
                    var y = Random.Range(-halfHeight, halfHeight);
                    var candidate = center + new Vector3(x, y, 0f);

                    if (IsFarEnough(candidate, positions, minDistanceSqr))
                    {
                        positions.Add(candidate);
                        placed = true;
                        break;
                    }
                }

                // 자리를 못 찾으면 그냥 스킵합니다 (해당 회차엔 덜 생성될 수 있음).
                if (!placed)
                {
                }
            }

            return positions;
        }

        // 화면 밖(카메라 뷰 바깥 테두리) 임의의 한 지점을, 기존 활성 광물과 겹치지 않는 곳에서 찾습니다.
        // 자리를 못 찾으면 null을 반환합니다(이번 스폰 틱은 스킵).
        private Vector3? GetRandomOffscreenPosition(Vector3 center, float minDistanceSqr, int maxTryPerPoint = 25)
        {
            var cam = Camera.main;
            if (cam == null)
                return null;

            var halfHeight = cam.orthographicSize * (1f + offscreenMarginRatio);
            var halfWidth = halfHeight * cam.aspect;

            for (var attempt = 0; attempt < maxTryPerPoint; attempt++)
            {
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

                var candidate = center + new Vector3(x, y, 0f);

                if (IsFarEnoughFromActive(candidate, minDistanceSqr))
                    return candidate;
            }

            return null;
        }

        // 같은 배치(리스트) 안에서 이미 정한 위치들과 충분히 떨어져 있는지 확인합니다.
        private bool IsFarEnough(Vector3 candidate, List<Vector3> existingPoints, float minDistanceSqr)
        {
            foreach (var vec in existingPoints)
            {
                var diff = candidate - vec;
                diff.z = 0f;

                if (diff.sqrMagnitude < minDistanceSqr)
                    return false;
            }

            return true;
        }

        // 현재 씬에 활성화된 광물들과 충분히 떨어져 있는지 확인합니다.
        private bool IsFarEnoughFromActive(Vector3 candidate, float minDistanceSqr)
        {
            if (ActivateList == null)
                return true;

            foreach (var resource in ActivateList)
            {
                var diff = candidate - resource.GetPosition();
                diff.z = 0f;

                if (diff.sqrMagnitude < minDistanceSqr)
                    return false;
            }

            return true;
        }
    }
}
