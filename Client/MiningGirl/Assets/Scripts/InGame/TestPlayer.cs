using System;
using System.Collections.Generic;
using System.Linq;
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
    
    [SerializeField]
    private Animator animator;
    
    private int _level;
    private CalcPlayerStat _stat;
    private MoveForward _moveComponent;
    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;
    private IInGameHandler _handler;
    private NodeRunner _nodeRunner;
    private IHit _target;
    
    public void Init(IInGameHandler handler, Action<int, Vector2, bool> onHit)
    {
        OnHit = null;
        OnHit += onHit;
        
        _handler = handler;
        _rigidbody ??= GetComponent<Rigidbody2D>();
        _spriteRenderer ??= GetComponent<SpriteRenderer>();
        _moveComponent = new MoveForward(_rigidbody);

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
        while (_handler.GetEnemyList() != null && _handler.GetEnemyList().Count != 0)
        {
            var playerPos = transform.position;
            var nearEnemy = _handler.GetEnemyList()
                .Where(x => x.GetActiveState()) // 오브젝트가 켜져있고,
                .OrderBy(x => (x.GetPosition() - playerPos).sqrMagnitude) // 가장 근접한 대상
                .FirstOrDefault();

            // 검색 결과가 없다면 일단 대기후 넘김.
            if (nearEnemy == null)
            {
                _target = null;
                await UniTask.WaitForSeconds(1.0f);
                continue;
            }
            
            _target = nearEnemy;
            await UniTask.WaitForSeconds(2.0f);
        }
    }

    private NodeState MoveNode()
    {
        if (_target == null)
            return NodeState.Failure;
        
        var currentPlayerPos = transform.position;
        var enemyPos = _target.GetPosition();
        var dist = Vector3.Distance(currentPlayerPos, enemyPos);

        // 충분히 가까워졌으면 멈춤
        if (dist <= 1.5f)
            return NodeState.Success;

        var dirVec = (enemyPos - currentPlayerPos).normalized;

        _moveComponent.Move(5.0f);
        _moveComponent.SetMoveVec(dirVec);
        SetDirection(dirVec);
        animator.Play("Idle", 0, 0);
        Debug.Log("Move");
        return NodeState.Running;
    }

    private bool _isPlaying;
    
    private NodeState AttackNode()
    {
        if (_target == null)
            return NodeState.Failure;
        
        if (_isPlaying)
            return NodeState.Running;
        
        AttackNodeAsync().Forget();
        return NodeState.Running;
    }
    
    private async UniTaskVoid AttackNodeAsync()
    {
        _isPlaying = true;

        _moveComponent.Move(0f);
        _moveComponent.SetMoveVec(Vector3.zero);

        Debug.Log("Ready");
        animator.Play("Ready", 0, 0);
        
        await UniTask.WaitForSeconds(0.5f);

        Debug.Log("Hit");
        animator.Play("Hit", 0, 0);
        _target.Damage();
        // OnHit?.Invoke(1, _target.GetAnchoredPosition(), false);
        
        await UniTask.WaitForSeconds(0.5f);
        
        _isPlaying = false;
    }
    
    private async void Start()
    {
        // await UniTask.WaitUntil(() => IsInitialized);
        //
        // while (true)
        // {
        //     Ready();
        //     await UniTask.WaitForSeconds(_stat.Speed);
        //     
        //     // 기본 공격
        //     Hit();
        //     _target.Damage();
        //
        //     var add = Random.Range(0, 3);
        //     
        //     // 추가타
        //     if (add == 0)
        //     {
        //         Hit(true);
        //         _target.Damage();
        //     }
        //     
        //     await UniTask.WaitForSeconds(0.1f);
        // }
    }

    private void SetDirection(Vector2 dir)
    {
        _spriteRenderer.flipX = dir.x switch
        {
            > 0 => false,
            < 0 => true,
            _ => _spriteRenderer.flipX
        };
    }

    public void SetLevel(int level)
    {
        _level = level;
    }

    private void Idle()
    {
        animator.Play("Idle", 0, 0);
    }
    
    private void Ready()
    {
        animator.Play("Ready", 0, 0);
    }

    private void Hit(bool isAdd = false)
    {
        animator.Play("Hit", 0, 0);

        // Debug.Log(_level);
        // _stat = new CalcPlayerStat(_level, Row);
        //
        // OnHit?.Invoke((int)_stat.Damage, _target.GetPosition(), isAdd);
    }
}