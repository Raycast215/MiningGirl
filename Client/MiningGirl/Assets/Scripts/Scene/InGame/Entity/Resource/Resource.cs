using System.Collections.Generic;
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

        private void Effect()
        {
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
        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        public override void Hit(float damage, bool isCritical)
        {
            if (BaseData.Health <= 0)
                return;

            _damagePresenter?.Show(Mathf.RoundToInt(damage), GetPosition(), isCritical);

            if (isCritical)
                Time.timeScale = 0.2f;

            Invoke(nameof(Effect), 0.1f);

            BaseData.Health -= damage;

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
