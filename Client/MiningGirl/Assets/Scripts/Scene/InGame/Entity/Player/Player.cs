using System;
using System.Threading;
using System.Collections.Generic;
using BehaviourTree;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Node;
using UnityEngine;

namespace Scene.InGame.Entity.Player
{
    public class Player : EntityBase
    {
        private event Action<Vector3> OnDirectionEvent;
        
        private AttackNode _attackNode;
        private SearchTargetNode _targetSearchNode;

        private Resource.IResourceProvider _resourceProvider;
        private global::MainGame.Bonus.CharacterStatContext _statContext;

        // 선택한 캐릭터 스탯 + 레벨업 보너스를 담은 컨텍스트를 주입합니다.
        public void SetStatContext(global::MainGame.Bonus.CharacterStatContext context)
        {
            _statContext = context;
        }

        [Header("Battle")]
        [SerializeField]
        private float maxHealth = 10f;
        [SerializeField]
        [Tooltip("피격 후 무적 시간(초)")]
        private float invincibleDuration = 2f;
        [SerializeField]
        [Tooltip("체력이 0이 되면 이 시간 동안 이동/채굴을 멈춥니다(초)")]
        private float downDuration = 3f;
        [SerializeField]
        [Range(0.05f, 1f)]
        [Tooltip("쓰러진 뒤 회복될 체력 비율(최대 체력 대비). 0.1이면 10%로 일어납니다.")]
        private float reviveHealthRatio = 0.1f;

        [SerializeField]
        [Range(1f, 4f)]
        [Tooltip("기상 직후 무적 시간 배율. 체력이 적게 회복되는 대신 빠져나갈 시간을 줍니다.")]
        private float reviveInvincibleMultiplier = 2f;

        // 남은 무적 시간 / 남은 다운 시간
        private float _invincibleTimer;
        private float _downTimer;

        private Tween _blinkTween;
        private IPlayerStatusPresenter _statusPresenter;

        // 최대 체력·무적 시간은 캐릭터 데이터에서 옵니다(없으면 인스펙터 값 사용).
        public float MaxHealth => _statContext != null && _statContext.HasStat ? _statContext.GetMaxHealth() : maxHealth;
        private float GetInvincibleDuration() => _statContext != null && _statContext.HasStat ? _statContext.GetInvincibleDuration() : invincibleDuration;
        public float Health => BaseData?.Health ?? 0f;
        public bool IsInvincible => _invincibleTimer > 0f;
        public bool IsDown => _downTimer > 0f;
        // 쓰러진 뒤 회복까지 남은 비율 (게이지는 이 값을 표시합니다)
        public float DownRatio => downDuration <= 0f ? 0f : Mathf.Clamp01(_downTimer / downDuration);
        public float InvincibleRatio => GetInvincibleDuration() <= 0f ? 0f : Mathf.Clamp01(_invincibleTimer / GetInvincibleDuration());
        public float HealthRatio => MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / MaxHealth);

        // 체력/무적 표시를 담당하는 뷰를 주입합니다.
        public void SetStatusPresenter(IPlayerStatusPresenter presenter)
        {
            _statusPresenter = presenter;
            RefreshStatus();
        }

        // 체력을 초기 상태로 되돌립니다.
        public void ResetHealth()
        {
            if (BaseData != null)
            {
                BaseData.MaxHealth = MaxHealth;
                BaseData.Health = MaxHealth;
            }

            _invincibleTimer = 0f;
            _downTimer = 0f;

            StopBlink();
            RefreshStatus();
        }

        // 팝업 등으로 게임이 멈춘 동안 깜빡임 연출도 함께 멈춥니다.
        // (DOTween은 우리가 만든 정지 플래그와 무관하게 계속 돌기 때문에 직접 멈춰야 합니다.)
        private bool _isStatusPaused;

        public void SetStatusPaused(bool paused)
        {
            if (_isStatusPaused == paused)
                return;

            _isStatusPaused = paused;

            if (_blinkTween == null || !_blinkTween.IsActive())
                return;

            if (paused)
                _blinkTween.Pause();
            else
                _blinkTween.Play();
        }

        // 최대 체력 대비 비율만큼 회복합니다(회복 카드용).
        // 쓰러져 있는 동안에는 회복해도 일어나지 않으므로 무시합니다.
        public void HealByRatio(float ratio)
        {
            if (BaseData == null || ratio <= 0f || IsDown)
                return;

            var amount = MaxHealth * ratio;

            BaseData.Health = Mathf.Min(MaxHealth, BaseData.Health + amount);

            RefreshStatus();
        }

        // 매 프레임 무적/다운 시간을 흘려보냅니다. PlayerController.Update에서 호출합니다.
        public void UpdateStatus(float deltaTime)
        {
            SyncMaxHealth();

            // 카드 버프 지속시간도 함께 흘려보냅니다(정지 중에는 호출되지 않음).
            _statContext?.Buffs?.Update(deltaTime);

            if (_invincibleTimer > 0f)
            {
                _invincibleTimer = Mathf.Max(0f, _invincibleTimer - deltaTime);

                if (_invincibleTimer <= 0f)
                    StopBlink();
            }

            if (_downTimer > 0f)
            {
                _downTimer = Mathf.Max(0f, _downTimer - deltaTime);

                if (_downTimer <= 0f)
                {
                    // 쓰러진 시간이 끝나면 최대 체력의 일정 비율로 일어납니다.
                    // (고정 1로 일어나면 곧바로 다시 쓰러지는 반복이 생겨 비율로 바꿨습니다.)
                    BaseData.Health = Mathf.Max(1f, MaxHealth * reviveHealthRatio);

                    // 체력은 조금만 회복되지만, 그만큼 무적 시간을 길게 줘서 빠져나갈 틈을 만듭니다.
                    _invincibleTimer = GetInvincibleDuration() * reviveInvincibleMultiplier;
                    PlayBlink();
                }
            }

            RefreshStatus();
        }

        // 레벨업으로 최대 체력이 늘어나면 그 증가분만큼 현재 체력도 함께 회복시킵니다.
        private void SyncMaxHealth()
        {
            if (BaseData == null)
                return;

            var max = MaxHealth;
            if (Mathf.Approximately(BaseData.MaxHealth, max))
                return;

            var delta = max - BaseData.MaxHealth;
            BaseData.MaxHealth = max;

            if (delta > 0f)
                BaseData.Health = Mathf.Min(max, BaseData.Health + delta);
            else
                BaseData.Health = Mathf.Min(BaseData.Health, max);
        }

        private void RefreshStatus()
        {
            _statusPresenter?.SetStatus(HealthRatio, DownRatio, IsDown);
        }

        private void PlayBlink()
        {
            StopBlink();

            if (spriteRenderer == null)
                return;

            // 무적 동안 깜빡입니다.
            _blinkTween = spriteRenderer.DOFade(0.25f, 0.12f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear);
        }

        private void StopBlink()
        {
            _blinkTween?.Kill();
            _blinkTween = null;

            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
        }

        // 플레이어가 어떤 광물들을 대상으로 삼을지(가장 가까운 것 탐색) 공급자를 주입합니다.
        // InitAsync() 전에 호출되어야 행동 트리 구성 시점에 반영됩니다.
        public void SetResourceProvider(Resource.IResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        public void InitDirectionEvent(Action<Vector3> onDirectionEvent)
        {
            OnDirectionEvent = null;
            OnDirectionEvent += onDirectionEvent;
        }

        // 스테이지 재시작(Next) 등으로 판이 리셋될 때 호출합니다.
        // 진행 중이던 채굴 공격을 취소하고 현재 타겟을 비워, 다음 프레임에 새 광물을 다시 탐색하게 합니다.
        public void ResetBehaviour()
        {
            _attackNode?.Dispose();
            SetTarget(null);
        }

        // (테스트용) 현재 타겟 광물을 제외한 나머지 활성 광물 중 하나를 무작위로 골라 타겟으로 지정합니다.
        // SearchTargetNode는 타겟이 살아있으면 유지하므로, 여기서 강제로 바꾼 타겟 쪽으로 이동하게 됩니다.
        public void MoveToRandomResource()
        {
            var resources = _resourceProvider?.GetActiveResources();
            if (resources == null || resources.Count == 0)
                return;

            var current = GetTarget();

            // 현재 타겟을 제외한 활성 광물 후보를 모읍니다.
            var candidates = new List<IEntity>(resources.Count);
            foreach (var resource in resources)
            {
                if (resource == null || !resource.GetActiveState())
                    continue;
                if (resource == current)
                    continue;

                candidates.Add(resource);
            }

            if (candidates.Count == 0)
                return;

            var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            SetTarget(pick);
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();
            
            // 가장 가까운 광물을 찾아 그 쪽으로 이동하는 행동 트리를 구성합니다.
            // 시퀀스: 타겟(가장 가까운 광물) 탐색 → 그 타겟을 향해 이동.
            // (채굴 공격 노드는 다음 단계에서 이어붙일 예정이라 지금은 이동까지만 연결합니다.)
            _targetSearchNode = new SearchTargetNode(this, _resourceProvider);
            _attackNode = new AttackNode(this);

            // 시퀀스: 타겟(가장 가까운 광물) 탐색 → 그 타겟을 향해 이동 → 사거리 안에 들면 채굴(공격).
            // MoveNode는 도달 전엔 Running을 돌려 시퀀스를 멈추므로, AttackNode는 광물에 도착한 뒤에만 실행됩니다.
            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>
            {
                new ActionNode(_targetSearchNode.ProcessNode),
                new ActionNode(MoveNode.ProcessNode),
                new ActionNode(_attackNode.ProcessNode),
            }));

            IsInitialized = true;
        }
        
        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        public override void Hit(float damage, bool isCritical)
        {
            // 무적 중이거나 이미 쓰러져 있으면 피해를 받지 않습니다.
            if (IsInvincible || IsDown)
                return;

            if (BaseData == null)
                return;

            BaseData.Health -= damage;

            if (BaseData.Health <= 0f)
            {
                // 쓰러짐 — 이동/채굴이 멈추고, downDuration 뒤에 최소 체력으로 일어납니다.
                BaseData.Health = 0f;
                _downTimer = downDuration;
                _invincibleTimer = 0f;

                _attackNode?.Dispose();
                StopMove();
                StopBlink();
            }
            else
            {
                // 피격 — 무적 시간이 붙고 그동안 스프라이트가 깜빡입니다.
                _invincibleTimer = GetInvincibleDuration();
                PlayBlink();
            }

            RefreshStatus();
        }

        // 쓰러져 있거나 무적인 동안에는 몬스터가 공격 대상으로 삼지 않습니다.
        public override bool IsAttackable()
        {
            return GetActiveState() && !IsDown && !IsInvincible;
        }

        public override void SetDirection(Vector3 direction)
        {
            base.SetDirection(direction);
            OnDirectionEvent?.Invoke(direction);
        }
        
        public override float GetDamage()
        {
            // 캐릭터 기본 공격력 + 레벨업 보너스
            return _statContext?.GetDamage() ?? 1f;
        }

        public override float GetAttackDistance()
        {
            // 타겟(광물)에 이 거리 이하로 가까워지면 이동을 멈춥니다(=채굴 사거리).
            return _statContext != null && _statContext.HasStat
                ? _statContext.GetAttackDistance()
                : BaseData?.MoveToMinDistance ?? 0f;
        }

        public override float GetAttackDelay()
        {
            // 채굴 1회 간격(초). 채굴 속도 보너스가 오르면 간격이 짧아집니다.
            return _statContext?.GetAttackDelay() ?? 2f;
        }

        public override float GetMoveSpeed()
        {
            return _statContext?.GetMoveSpeed() ?? BaseData?.MoveSpeed ?? 0f;
        }

        // 치명타 시 추가 배율(0.3이면 1.3배)
        public override float GetCriDamage()
        {
            return _statContext?.GetCriDamage() ?? 0f;
        }

        // 치명타 확률(%)
        public override float GetCriRate()
        {
            return _statContext?.GetCriRate() ?? 0f;
        }

        // 추가타 확률(%) — 한 번의 채굴이 두 번 들어갈 확률
        public override float GetExtraHitRate()
        {
            return _statContext?.GetExtraHitRate() ?? 0f;
        }

#endregion
    }
}