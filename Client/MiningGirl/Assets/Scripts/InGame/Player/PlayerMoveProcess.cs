using BehaviourTree;
using InGame.System;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerMoveProcess
    {
        private MoveForward _moveComponent;
        private IUnitInfoHandler _handler;
        private IPlayerMoveHandler _moveHandler;

        public PlayerMoveProcess(Rigidbody2D rigidbody2D, IUnitInfoHandler handler, IPlayerMoveHandler moveHandler)
        {
            _handler = handler;
            _moveHandler = moveHandler;
            _moveComponent = new MoveForward(rigidbody2D);
        }

        public NodeState ProcessNode()
        {
            var target = _handler.GetPlayerTarget();
            
            if (target == null || !target.GetActiveState())
                return NodeState.Failure;
        
            var currentPlayerPos = _handler.GetPlayerTransform().position;
            var enemyPos = target.GetPosition();
            var dist = Vector3.Distance(currentPlayerPos, enemyPos);

            if (dist <= 2.0f)
            {
                _moveComponent.Move(0.0f);
                return NodeState.Success;
            }

            if (_moveHandler.GetAttackPlayState() && !_moveHandler.GetAttackDoneState())
            {
                _moveComponent.Move(0.0f);
                return NodeState.Success;
            }
            
            var dirVec = (enemyPos - currentPlayerPos).normalized;

            _moveComponent.Move(3.0f);
            _moveComponent.SetMoveVec(dirVec);
            _moveHandler.SetDirection(dirVec);
            _moveHandler.SetAnimation(EPlayerState.Idle, dirVec.y < 0 
                ? EPlayerDirection.Down 
                : EPlayerDirection.Up);

            return NodeState.Running;
        }
    }
}