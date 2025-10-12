using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestPlayer : GameInitializer
{
    private event Action<int, Vector2> OnHit; 
    
    [SerializeField]
    private Animator animator;

    private IHit _target;
    
    private async void Start()
    {
        await UniTask.WaitUntil(() => IsInitialized);
        
        while (true)
        {
            Ready();
            await UniTask.WaitForSeconds(0.05f);
            Hit();
            _target.Damage();
            await UniTask.WaitForSeconds(0.1f);
        }
    }

    public void Init(IHit target, Action<int, Vector2> onHit)
    {
        OnHit = null;
        OnHit += onHit;
        
        _target = target;
        IsInitialized = true;
    }

    private void Ready()
    {
        animator.Play("Ready", 0, 0);
    }

    private void Hit()
    {
        animator.Play("Hit", 0, 0);
        OnHit?.Invoke(UnityEngine.Random.Range(0, 10000), _target.GetPosition());
    }
}