using System;
using Cysharp.Threading.Tasks;
using Data;
using InGame.System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TestPlayer : GameInitializer
{
    private event Action<int, Vector2, bool> OnHit;

    public PlayerStatTable Row { get; private set; }
    
    [SerializeField]
    private Animator animator;

    private IHit _target;
    private int _level;
    private CalcPlayerStat _stat;
    private MoveForward _moveComponent;
    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;
    
    private async void Start()
    {
        _rigidbody ??= GetComponent<Rigidbody2D>();
        _spriteRenderer ??= GetComponent<SpriteRenderer>();
        _moveComponent = new MoveForward(_rigidbody);

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

    private void FixedUpdate()
    {
        _moveComponent.SetMoveVec(Vector2.left);
        _moveComponent.Move(1.0f);

        SetDirection(Vector2.left);
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
    
    public void Init(PlayerStatTable row, IHit target, Action<int, Vector2, bool> onHit)
    {
        OnHit = null;
        OnHit += onHit;
        
        Row = row;
        
        _target = target;
        IsInitialized = true;
    }

    public void SetLevel(int level)
    {
        _level = level;
    }

    private void Ready()
    {
        animator.Play("Ready", 0, 0);
    }

    private void Hit(bool isAdd = false)
    {
        animator.Play("Hit", 0, 0);

        // Debug.Log(_level);
        _stat = new CalcPlayerStat(_level, Row);
        
        OnHit?.Invoke((int)_stat.Damage, _target.GetPosition(), isAdd);
    }
}