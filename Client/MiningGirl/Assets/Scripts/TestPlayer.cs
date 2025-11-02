using System;
using Cysharp.Threading.Tasks;
using Data;
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
    
    private async void Start()
    {
        await UniTask.WaitUntil(() => IsInitialized);
        
        while (true)
        {
            Ready();
            await UniTask.WaitForSeconds(_stat.Speed);
            
            // 기본 공격
            Hit();
            _target.Damage();

            var add = Random.Range(0, 3);
            
            // 추가타
            if (add == 0)
            {
                Hit(true);
                _target.Damage();
            }
            
            await UniTask.WaitForSeconds(0.1f);
        }
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