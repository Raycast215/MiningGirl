using System.Collections.Generic;
using BehaviourTree;
using InGame.Player;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace InGame
{
    public interface IPlayerMoveHandler
    {
        public void SetDirection(Vector3 direction);
        public void SetAnimation(EPlayerState state, EPlayerDirection direction);
        public bool GetAttackPlayState();
        public bool GetAttackDoneState();
    }
    
    public class PlayerController : GameInitializer, IPlayerMoveHandler
    {
        public IHit GetTarget => _searchTargetProcess.Target;
        
        [SerializeField]
        private Rigidbody2D rigidBody2D;
        [SerializeField]
        private SpriteRenderer spriteRenderer;
        [SerializeField]
        private PlayerAnimationController animationController;
        
        private NodeRunner _nodeRunner;
        private SearchTargetProcess _searchTargetProcess;
        private PlayerMoveProcess _playerMoveProcess;
        private PlayerAttackProcess _playerAttackProcess;
        private bool _isPlayerStartd;

        public void Initialize(IUnitInfoHandler handler)
        {
            _isPlayerStartd = false;
            _searchTargetProcess = new SearchTargetProcess(handler);
            _playerMoveProcess = new PlayerMoveProcess(rigidBody2D, handler, this);
            _playerAttackProcess = new PlayerAttackProcess(handler, this);
            _nodeRunner = new NodeRunner(new SequenceNode(new List<INode>()
            {
                new ActionNode(_playerMoveProcess.ProcessNode),
                new ActionNode(_playerAttackProcess.ProcessNode),
            }));
        }
        
        public void Process()
        {
            _isPlayerStartd = true;
            _searchTargetProcess.Process().Forget();
        }

        public void Stop()
        {
            _isPlayerStartd = false;
            _searchTargetProcess.Dispose();
        }
        
        private void Update()
        {
            if (!_isPlayerStartd)
                return;
            
            _nodeRunner?.OperateNode();
        }

#region IPlayerMoveHandler

        public void SetDirection(Vector3 direction)
        {
            spriteRenderer.flipX = direction.x switch
            {
                > 0 => false,
                < 0 => true,
                _ => spriteRenderer.flipX
            };
        }

        public void SetAnimation(EPlayerState state, EPlayerDirection direction)
        {
            animationController.SetAnimation(state, direction);
        }

        public bool GetAttackPlayState()
        {
            return _playerAttackProcess.IsPlaying;
        }

        public bool GetAttackDoneState()
        {
            return _playerAttackProcess.IsAttackDone;
        }

#endregion
    }
}