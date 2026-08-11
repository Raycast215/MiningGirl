using System;
using System.Collections.Generic;
using BehaviourTree;
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
        
        // public void SetHandler(IInGameHandler handler)
        // {
        //     _handler = handler;
        // }

        public void InitDirectionEvent(Action<Vector3> onDirectionEvent)
        {
            OnDirectionEvent = null;
            OnDirectionEvent += onDirectionEvent;
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();
            
            // _attackNode = new AttackNode(this);
            // _targetSearchNode = new SearchTargetNode(this, _handler.GetEntityHandler());
            //
            // NodeRunner = new NodeRunner(new SequenceNode(new List<INode>()
            // {
            //     new ActionNode(_targetSearchNode.ProcessNode),
            //     new ActionNode(MoveNode.ProcessNode),
            //     new ActionNode(_attackNode.ProcessNode),
            // }));
            
            IsInitialized = true;
        }
        
        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        public override void Hit(float damage, bool isCritical)
        {
         
        }

        public override void SetDirection(Vector3 direction)
        {
            base.SetDirection(direction);
            OnDirectionEvent?.Invoke(direction);
        }
        
        public override float GetDamage()
        {
            return 0;
            //  return _handler.GetInGameData().GetStatData(EStatType.Damage).Value;
        }

        public override float GetAttackDistance()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.AttackDistance).Value;
        }

        public override float GetAttackDelay()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.AttackDelay).Value;
        }

        public override float GetMoveSpeed()
        {
            return 0;
            //  return _handler.GetInGameData().GetStatData(EStatType.MoveSpeed).Value;
        }

        public override float GetCriDamage()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.CriDamage).Value;
        }

        public override float GetCriRate()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.CriRate).Value;
        }

        public override float GetExtraHitRate()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.ExtraHitRate).Value;
        }

#endregion
    }
}