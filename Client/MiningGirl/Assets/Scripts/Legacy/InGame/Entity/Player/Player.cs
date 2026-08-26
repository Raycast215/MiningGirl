using System;
using System.Threading;
using System.Collections.Generic;
using BehaviourTree;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Legacy.Scene.InGame.Entity.Interface;
using Legacy.Scene.InGame.Entity.Node;
using UnityEngine;

namespace Legacy.Scene.InGame.Entity.Player
{
    public class Player : EntityBase
    {
        private event Action<Vector3> OnDirectionEvent;
        
        private AttackNode _attackNode;
        private SearchTargetNode _targetSearchNode;

        private Resource.IResourceProvider _resourceProvider;
        private global::Legacy.MainGame.Bonus.CharacterStatContext _statContext;

        // 선택한 캐릭터 스탯 + 레벨업 보너스를 담은 컨텍스트를 주입합니다.
        public void SetStatContext(global::Legacy.MainGame.Bonus.CharacterStatContext context)
        {
            _statContext = context;
        }

        [Header("Battle")]
        [SerializeField]
        [Tooltip("피격 후 무적 시간(초)")]
        private float invincibleDuration = 2f;
        // 남은 무적 시간
        private float _invincibleTimer;

        // 실제로 피해를 입은 순간 호출됩니다(무적으로 무시된 경우는 제외).
        // 실패 판정은 스태미나 하나로 통일되어 있어, 이 콜백이 스태미나를 깎습니다.
        private Action _onDamaged;

        private Tween _blinkTween;

        // 무적 시간은 캐릭터 데이터에서 옵니다(없으면 인스펙터 값 사용).
        private float GetInvincibleDuration() => _statContext != null && _statContext.HasStat ? _statContext.GetInvincibleDuration() : invincibleDuration;
        public bool IsInvincible => _invincibleTimer > 0f;

        public void SetDamagedHandler(Action handler)
        {
            _onDamaged = handler;
        }
        public float InvincibleRatio => GetInvincibleDuration() <= 0f ? 0f : Mathf.Clamp01(_invincibleTimer / GetInvincibleDuration());

        // 무적·깜빡임을 초기 상태로 되돌립니다(스테이지 시작·재시작).
        public void ResetStatus()
        {
            _invincibleTimer = 0f;

            StopBlink();
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

        // 매 프레임 무적/다운 시간을 흘려보냅니다. PlayerController.Update에서 호출합니다.
        public void UpdateStatus(float deltaTime)
        {
            // 카드 버프 지속시간도 함께 흘려보냅니다(정지 중에는 호출되지 않음).
            _statContext?.Buffs?.Update(deltaTime);

            if (_invincibleTimer > 0f)
            {
                _invincibleTimer = Mathf.Max(0f, _invincibleTimer - deltaTime);

                if (_invincibleTimer <= 0f)
                    StopBlink();
            }
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

#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();

            // 가장 가까운 광물을 찾아 이동하고, 사거리에 들면 채굴하는 행동 트리입니다.
            // MoveNode는 도달 전엔 Running을 돌려 시퀀스를 멈추므로 AttackNode는 도착 후에만 실행됩니다.
            _targetSearchNode = new SearchTargetNode(this, _resourceProvider);
            _attackNode = new AttackNode(this);

            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>
            {
                new ActionNode(_targetSearchNode.ProcessNode),
                new ActionNode(MoveNode.ProcessNode),
                new ActionNode(_attackNode.ProcessNode),
            }));

            IsInitialized = true;
        }
        
        public override IReadOnlyList<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        // 피격 — 실패 판정은 스태미나가 맡습니다.
        // 여기서는 무적 시간을 걸고 깜빡임만 보여주며,
        // 실제 소모는 _onDamaged 콜백을 받는 쪽(MainGameUIController)이 처리합니다.
        public override void Hit(float damage, bool isCritical, bool isExtraHit = false)
        {
            // 무적 중에는 피해를 받지 않습니다.
            if (IsInvincible)
                return;

            _onDamaged?.Invoke();

            _invincibleTimer = GetInvincibleDuration();

            PlayBlink();
        }

        // 죽었거나 무적인 동안에는 몬스터가 공격 대상으로 삼지 않습니다.
        // 무적인 동안에는 몬스터가 공격 대상으로 삼지 않습니다.
        public override bool IsAttackable()
        {
            return GetActiveState() && !IsInvincible;
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
