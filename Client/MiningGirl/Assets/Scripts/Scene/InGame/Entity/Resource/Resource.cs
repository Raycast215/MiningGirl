using System;
using System.Collections.Generic;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity.Resource
{
    public class Resource : EntityBase
    {
        private event Action<IEntity> OnReturned;
        private IInGameHandler _handler;
        
        public void SetHandler(IInGameHandler handler, Action<IEntity> onReturned)
        {
            _handler = handler;

            OnReturned = null;
            OnReturned += onReturned;
        }
        
        private void DamageFinish()
        {
            if (!(BaseData.Health <= 0)) 
                return;
            
            OnReturned?.Invoke(this);
            _handler.GetUIHandler().AddStoneCount(1);
            _handler.GetUIHandler().AddExpCount(1.0f);
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();
                    
            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>()
            {
                new ActionNode(MoveNode.ProcessNode),
            }));
                    
            IsInitialized = true;
        }

        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            var ret = _handler.GetEntityHandler().GetResourceList();
            
            return ret;
        }

        public override void Damage(float damage)
        {
            if (BaseData.Health <= 0)
                return;
            
            var isCritical = BaseData.Health == 1;

            if (isCritical)
            {
                _handler.CameraAnimation();
            }
            
            BaseData.Health -= damage;
            _handler.ShowDamageFloatingText((int)damage, transform.position, isCritical);

            transform.DOShakePosition(0.1f, new Vector3(0.2f, 0f, 0f))
                .SetRelative(true)
                .OnComplete(DamageFinish);
        }

#endregion
    }
}
