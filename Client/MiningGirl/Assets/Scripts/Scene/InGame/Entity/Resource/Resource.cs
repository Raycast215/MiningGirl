using System.Collections.Generic;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MainGame.Entity;
using Scene.InGame.Entity.Data;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity.Resource
{
    public class Resource : EntityBase
    {
        private static readonly int AddColorFade = Shader.PropertyToID(ShaderFade);
        private const string ShaderFade = "_AddColorFade";

        private ResourceController _owner;
        private IFloatingDamagePresenter _damagePresenter;
        private IResourceDepletedHandler _depletedHandler;

        private int _stoneReward;
        private int _expReward;

        private Material _material;
        private Tween _shakeTween;
        private CancellationTokenSource _fadeCts;
        private CancellationTokenSource _hitStopCts;

        [Header("Critical Hit Stop")]
        [SerializeField]
        [Tooltip("크리티컬 시 느려지는 정도(1이면 슬로우 없음)")]
        private float hitStopScale = 0.35f;
        [SerializeField]
        [Tooltip("히트스톱 지속 시간(실제 시간 기준, 초)")]
        private float hitStopDuration = 0.06f;

        private void Start()
        {
            _material = spriteRenderer.material;
            _material.SetFloat(AddColorFade, 0.0f);
        }

        // ResourceController.Spawn()에서 호출됩니다. 스폰(재사용) 시점마다 상태를 다시 세팅합니다.
        public void Setup(
            ResourceController owner,
            float maxHp,
            int stoneReward,
            int expReward,
            IFloatingDamagePresenter damagePresenter = null,
            IResourceDepletedHandler depletedHandler = null)
        {
            _owner = owner;
            _damagePresenter = damagePresenter;
            _depletedHandler = depletedHandler;
            _stoneReward = stoneReward;
            _expReward = expReward;

            BaseData = new EntityData
            {
                MaxHealth = maxHp,
                Health = maxHp,
                MoveSpeed = 0,
                MoveToMinDistance = 0,
                AttackDelay = 0
            };

            // 이전 사용(풀 재사용) 시 남아있을 수 있는 트윈/페이드를 정리하고 원래 상태로 되돌립니다.
            _shakeTween?.Kill();
            _shakeTween = null;
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = null;

            if (_material != null)
                _material.SetFloat(AddColorFade, 0.0f);

                        spriteRenderer.transform.localPosition = Vector3.zero;

        }

        private void DamageFinish()
        {
            if (BaseData.Health > 0)
                return;
            _depletedHandler?.OnResourceDepleted(this);
        }

        private async UniTask FadeMaterialAsync(CancellationToken token)
        {
            const float duration = 0.2f;
            var time = 0f;

            const float start = 0.2f;
            const float end = 0.0f;

            while (time < duration)
            {
                if (token.IsCancellationRequested)
                    return;

                time += Time.deltaTime;
                var t = time / duration;

                var value = Mathf.Lerp(start, end, t);
                _material.SetFloat(AddColorFade, value);

                await UniTask.Yield(cancellationToken: token).SuppressCancellationThrow();
            }

            if (!token.IsCancellationRequested)
                _material.SetFloat(AddColorFade, end);
        }

        // 크리티컬 히트스톱.
        //
        // 주의: 예전에는 Invoke(nameof(Effect), 0.1f)로 복구했는데,
        // Invoke는 '스케일된 시간'을 쓰기 때문에 timeScale이 0.2면 실제로는 0.5초가 걸렸습니다.
        // 크리티컬이 자주 터지면 슬로우가 계속 이어져 게임이 느려진 것처럼 보였습니다.
        // 그래서 실제 시간(ignoreTimeScale) 기준으로 정확히 복구합니다.
        private async UniTaskVoid PlayHitStop()
        {
            _hitStopCts?.Cancel();
            _hitStopCts?.Dispose();
            _hitStopCts = new CancellationTokenSource();

            var token = _hitStopCts.Token;

            Time.timeScale = hitStopScale;

            try
            {
                await UniTask.WaitForSeconds(hitStopDuration, ignoreTimeScale: true, cancellationToken: token);
            }
            catch (Exception _)
            {
                return;
            }

            Time.timeScale = 1.0f;
        }

        private void OnDisable()
        {
            // 풀로 돌아갈 때 슬로우가 걸린 채 남지 않도록 정리합니다.
            _hitStopCts?.Cancel();
            _hitStopCts?.Dispose();
            _hitStopCts = null;

            if (!Mathf.Approximately(Time.timeScale, 1.0f))
                Time.timeScale = 1.0f;
        }

#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();

            // 광물은 제자리에 고정되어 있고 이동하지 않으므로, 이동 노드를 붙이지 않습니다.
            // (NodeRunner를 만들지 않으면 EntityControllerBase.UpdateEntity()의 UpdateNode() 호출이
            //  NodeRunner?.OperateNode()에서 조용히 스킵되어 매 프레임 처리 비용이 들지 않습니다.)
            IsInitialized = true;
        }

        // 광물은 몰려서 밀어낼 필요가 없어(움직이지 않음) 근접 목록을 쓰지 않습니다.
        public override IReadOnlyList<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        public override void Hit(float damage, bool isCritical, bool isExtraHit = false)
        {
            if (BaseData.Health <= 0)
                return;

            _damagePresenter?.Show(Mathf.RoundToInt(damage), GetPosition(), isCritical);

            // 크리티컬 시 짧은 히트스톱(슬로우). 실제 시간 기준으로 복구합니다.
            if (isCritical)
                PlayHitStop().Forget();

            BaseData.Health -= damage;

            // 채굴 '시도' 1회. 다 캤는지와 무관하게 여기서 스태미나가 나갑니다.
            // 단, 추가타는 곡괭이를 한 번 더 휘두른 게 아니라 같은 한 번에 딸려온 덤이므로
            // 시도로 세지 않습니다. (추가타 확률 강화가 곧 스태미나 효율이 됩니다.)
            if (!isExtraHit)
                _owner?.NotifyMiningAttempt();

            _shakeTween?.Kill();
            _shakeTween = spriteRenderer.transform.DOShakePosition(0.1f, new Vector3(0.2f, 0f, 0f))
                .SetRelative(true)
                .OnComplete(DamageFinish);

            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = new CancellationTokenSource();
            FadeMaterialAsync(_fadeCts.Token).Forget();

            if (BaseData.Health <= 0)
                _owner?.NotifyReward(_stoneReward, _expReward);
        }

        public override float GetDamage() => 0f;

        // 광물은 공격하지 않으므로(고정, 비전투) 관련 스탯은 모두 안전한 기본값을 반환합니다.
        public override float GetAttackDistance() => 0f;
        public override float GetAttackDelay() => 0f;
        public override float GetMoveSpeed() => 0f;
        public override float GetCriDamage() => 0f;
        public override float GetCriRate() => 0f;
        public override float GetExtraHitRate() => 0f;

#endregion
    }
}
