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
        private static readonly int AddColorFade = Shader.PropertyToID(ShaderFade);
        private const string ShaderFade = "_AddColorFade";
        
        private event Action<IEntity> OnReturned;
        private IInGameHandler _handler;
        private Material _material;

        private void Start()
        {
            _material = spriteRenderer.material;
            _material.SetFloat(AddColorFade, 0.0f);
        }
        
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
        
        private async UniTask FadeMaterialAsync()
        {
            float duration = 0.2f;
            float time = 0f;

            float start = 0.2f;
            float end = 0.0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;

                float value = Mathf.Lerp(start, end, t);
                _material.SetFloat(AddColorFade, value);

                await UniTask.Yield();
            }

            _material.SetFloat(AddColorFade, end);
        }

        private void Effect()
        {
            Time.timeScale = 1.0f;
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

        public override void Hit(float damage, bool isCritical)
        {
            _handler.ShowDamageFloatingText((int)damage, transform.position, isCritical);
            
            if (BaseData.Health <= 0)
                return;
            
            if (isCritical)
            {
                _handler.CameraAnimation();
                Time.timeScale = 0.2f;
            }
            
            Invoke(nameof(Effect), 0.1f);
            
            BaseData.Health -= damage;
            
            spriteRenderer.transform.DOShakePosition(0.1f, new Vector3(0.2f, 0f, 0f))
                .SetRelative(true)
                .OnComplete(DamageFinish);

            FadeMaterialAsync().Forget();
        }

        public override float GetDamage()
        {
            return 0;
        }

        public override float GetAttackDistance()
        {
            throw new NotImplementedException();
        }

        public override float GetAttackDelay()
        {
            throw new NotImplementedException();
        }

        public override float GetMoveSpeed()
        {
            return 0;
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
