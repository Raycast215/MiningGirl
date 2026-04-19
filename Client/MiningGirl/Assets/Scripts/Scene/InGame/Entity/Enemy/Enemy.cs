using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTree;
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
            spriteRenderer.color = new Color(0f, 0f, 0f, 1f);
            MoveNode.SetMoveSpeed(BaseData.MoveSpeed);

            if (!(BaseData.Health <= 0)) 
                return;
            
            OnReturned?.Invoke(this);
            _handler.GetUIHandler().AddGoldCount(1);
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();

            spriteRenderer.color = new Color(0f, 0f, 0f, 1f);
            
            _attackNode = new AttackNode(this)
                .SetDelay(BaseData.AttackDelay);
            
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

        public override void Damage(float damage)
        {
            if (BaseData.Health <= 0)
                return;
            
            var player = _handler.GetEntityHandler().GetPlayer();
            var playerPos = player.GetPosition();
            var myPos = transform.position;
            var vec = (playerPos - myPos).normalized;

            BaseData.Health -= damage;
            MoveNode.SetMoveSpeed(0);
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
                spriteRenderer.color = new Color(0f, 0f, 0f, 1f);
            }
            
            _colorTween = spriteRenderer
                .DOColor(new Color(1f, 0f, 0f, 1f), 0.2f)
                .OnComplete(DamageFinish);
        }

#endregion
    }
}