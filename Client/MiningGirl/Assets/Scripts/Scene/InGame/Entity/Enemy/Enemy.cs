using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTree;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Node;
using Color = UnityEngine.Color;

namespace Scene.InGame.Entity.Enemy
{
    public class Enemy : EntityBase
    {
        private event Action<IEntity> OnReturned;
        private IInGameHandler _handler;
        private AttackNode _attackNode;
        private Tween _posTween;
        private Tween _colorTween;
        
        public void SetHandler(IInGameHandler handler, Action<IEntity> onReturned)
        {
            _handler = handler;
            
            OnReturned = null;
            OnReturned += onReturned;
        }

        private void DamageFinish()
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

            if (BaseData.Health > 0) 
                return;
            
            _handler.GetInGameData().AddItemCount(EItemType.Gold, ConstData.BaseIncreaseGold);
            _handler.GetUIHandler().AddGoldCount(ConstData.BaseIncreaseGold);
            
            _handler.GetInGameData().AddItemCount(EItemType.Exp, 1);
            _handler.GetUIHandler().AddExpCount(1);
            
            OnReturned?.Invoke(this);
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();

            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

            _attackNode = new AttackNode(this);
            
            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>()
            {
                new ActionNode(MoveNode.ProcessNode),
                new ActionNode(_attackNode.ProcessNode),
            }));
                    
            IsInitialized = true;
        }

        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            var ret = _handler.GetEntityHandler().GetEnemyList().Where(x => (Enemy)x != this);
            
            return ret;
        }

        public override void Hit(float damage, bool isCritical)
        {
            if (BaseData.Health <= 0)
                return;
            
            var player = _handler.GetEntityHandler().GetPlayer();
            var playerPos = player.GetPosition();
            var myPos = transform.position;
            var vec = (playerPos - myPos).normalized;

            BaseData.Health -= damage;
            _handler.ShowDamageFloatingText((int)damage, transform.position);
            
            if (_posTween != null)
            {
                _posTween.Kill();
                _posTween = null;
            }

            _posTween = transform.DOMove(myPos - vec, 0.2f);
            
            if (_colorTween != null)
            {
                _colorTween.Kill();
                _colorTween = null;
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            }
            
            _colorTween = spriteRenderer
                .DOColor(new Color(1f, 0f, 0f, 1f), 0.2f)
                .OnComplete(DamageFinish);
        }

        public override float GetDamage()
        {
            return 1;
        }

        public override float GetAttackDistance()
        {
            return 3;
        }

        public override float GetAttackDelay()
        {
            throw new NotImplementedException();
        }

        public override float GetMoveSpeed()
        {
            return BaseData.MoveSpeed;
        }

        public override float GetCriDamage()
        {
            throw new NotImplementedException();
        }

        public override float GetCriRate()
        {
            throw new NotImplementedException();
        }

        public override float GetExtraHitRate()
        {
            throw new NotImplementedException();
        }

#endregion
    }
}