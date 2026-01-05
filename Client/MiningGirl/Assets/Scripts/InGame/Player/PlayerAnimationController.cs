using UnityEngine;

namespace InGame.Player
{
    public enum EPlayerAnimationType
    {
        Idle_Up,
        Idle_Down,
        Attack_Up,
        Attack_Down,
    }

    public enum EPlayerState
    {
        Idle,
        Move,
        Attack
    }

    public enum EPlayerDirection
    {
        Up,
        Down
    }

    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] 
        private EPlayerAnimationType type;
        [SerializeField]
        private Animator animator;
    
        private void OnEnable()
        {
            animator ??= GetComponent<Animator>();
            animator.Play($"{type}", 0, 0);
        }

        public void SetAnimation(EPlayerState state, EPlayerDirection direction)
        {
            animator ??= GetComponent<Animator>();
            animator.Play($"{state}_{direction}", 0, 0);
        }
    }
}