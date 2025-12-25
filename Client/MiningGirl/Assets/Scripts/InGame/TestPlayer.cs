using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using Data;
using InGame;
using InGame.System;
using UnityEngine;

public class TestPlayer : GameInitializer
{
    private event Action<int, Vector2, bool> OnHit;

    public PlayerStatTable Row { get; private set; }
    
    public IHit Target { get; private set; }
    
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private Rigidbody2D rigidBody2D;
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    
    private CalcPlayerStat _stat;
    private MoveForward _moveComponent;
    private IInGameHandler _handler;
    private NodeRunner _nodeRunner;
    private CancellationTokenSource _cts;
    private CancellationTokenSource _checkCts;
    
    public void Init(IInGameHandler handler, Action<int, Vector2, bool> onHit)
    {
        OnHit = null;
        OnHit += onHit;
        
        _handler = handler;
        _moveComponent = new MoveForward(rigidBody2D);
        _checkCts ??= new CancellationTokenSource();
        
        _nodeRunner = new NodeRunner( new SequenceNode(new List<INode>()
        {
            new ActionNode(MoveNode),
            new ActionNode(AttackNode),
        }));
    }

    private void Update()
    {
        _nodeRunner?.OperateNode();
    }

    public async UniTaskVoid ExecuteFindEnemy()
    {
        while (true)
        {
            var enemyList = _handler.GetEnemyList();

            if (enemyList == null || enemyList.Count == 0)
            {
                Target = null;
                await UniTask.WaitForSeconds(0.1f, cancellationToken: _checkCts.Token);
                continue;
            }

            if (_checkCts == null || _checkCts.IsCancellationRequested)
                return;
            
            var playerPos = transform.position;
            var nearEnemy = enemyList
                .Where(x => x.GetActiveState())
                .OrderBy(x => (x.GetPosition() - playerPos).sqrMagnitude)
                .FirstOrDefault();

            Target = nearEnemy;

            await UniTask.WaitForSeconds(0.1f, cancellationToken: _checkCts.Token); // 0.1초마다 재탐색
        }
    }

    private NodeState MoveNode()
    {
        if (Target == null)
            return NodeState.Failure;
        
        var currentPlayerPos = transform.position;
        var enemyPos = Target.GetPosition();
        var dist = Vector3.Distance(currentPlayerPos, enemyPos);
        
        if (dist <= 2.0f)
            return NodeState.Success;

        if (_isPlaying && !_attackDone)
            return NodeState.Success;
        
        var dirVec = (enemyPos - currentPlayerPos).normalized;

        _moveComponent.Move(3.0f);
        _moveComponent.SetMoveVec(dirVec);
        SetDirection(dirVec);
        animator.Play("Idle", 0, 0);
        // Debug.Log("Move");
        return NodeState.Running;
    }

    private bool _isPlaying;
    private bool _attackDone;
    
    private NodeState AttackNode()
    {
        // 타겟이 없거나 비활성 → 공격 취소
        if (!IsValidTarget(Target))
        {
            // 이미 공격 중이면 코루틴/UniTask 취소
            if (_isPlaying)
            {
                _cts?.Cancel();   // 아래 AttackNodeAsync 쪽에서 처리됨
            }
        
            return NodeState.Failure;
        }

        // 애니/공격 로직 진행 중이면 계속 Running
        if (_isPlaying)
            return NodeState.Running;

        // 한 사이클 끝났으면 한 번만 Success
        if (_attackDone)
        {
            _attackDone = false;
            return NodeState.Success;
        }

        // 새 공격 시작
        AttackNodeAsync().Forget();
        return NodeState.Running;
    }
    
    private bool IsValidTarget(IHit target)
    {
        return target != null && target.GetActiveState();
    }
    
    private async UniTaskVoid AttackNodeAsync()
    {
        // 이전 공격 취소 & 정리
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _isPlaying = true;
        _attackDone = false;

        // 공격 시작 시점의 타겟 스냅샷
        var target = Target;

        try
        {
            // 시작부터 타겟이 이미 죽어있으면 바로 종료
            if (!IsValidTarget(target))
                return;

            // 1. 이동 정지
            _moveComponent.Move(0f);
            _moveComponent.SetMoveVec(Vector3.zero);

            // 방향 맞추기
            var dir = (target.GetPosition() - transform.position).normalized;
            SetDirection(dir);

            // 2. 준비 자세
            animator.Play("Ready", 0, 0);
            await UniTask.WaitForSeconds(0.5f, cancellationToken: _cts.Token);

            if (!IsValidTarget(target))
            {
                // animator.Play("Ready", 0, 0);
                return;
            }

            // 3. Hit 포즈로 전환
            animator.Play("Hit", 0, 0);

            // 화면에 Hit가 한 프레임이라도 보이게
            await UniTask.Yield();

            if (!IsValidTarget(target))
            {
                // animator.Play("Ready", 0, 0);
                return;
            }

            // 4. 이 타이밍에 데미지
            target.Damage();

            await UniTask.WaitForSeconds(0.1f, cancellationToken: _cts.Token);

            if (!IsValidTarget(target))
            {
                // animator.Play("Ready", 0, 0);
                return;
            }

            // 5. 다시 Ready + 후딜
            animator.Play("Ready", 0, 0);
            await UniTask.WaitForSeconds(2.0f, cancellationToken: _cts.Token);

            // 여기까지 왔으면 한 사이클 정상 완료
            _attackDone = true;
        }
        catch (OperationCanceledException)
        {
            // 공격 도중 취소 (타겟 사라짐 등) → 조용히 무시해도 됨
            // Debug.Log("Attack canceled");
        }
        catch (Exception e)
        {
            // Debug.LogException(e);
        }
        finally
        {
            _isPlaying = false;
        }
    }

    private void SetDirection(Vector2 dir)
    {
        spriteRenderer.flipX = dir.x switch
        {
            > 0 => false,
            < 0 => true,
            _ => spriteRenderer.flipX
        };
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        _checkCts?.Cancel();
        _checkCts?.Dispose();
        _checkCts = null;
    }
}