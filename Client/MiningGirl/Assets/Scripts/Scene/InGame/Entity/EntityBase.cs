using System.Collections.Generic;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Data;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Node;
using UnityEngine;

namespace Scene.InGame.Entity
{
    public abstract class EntityBase : GameMonoInitializer, IEntity
    {
        public EntityData BaseData { get; set; }
        
        [SerializeField]
        private Rigidbody rigidBody;
        [SerializeField]
        protected SpriteRenderer spriteRenderer;
        
        protected NodeRunner NodeRunner;
        protected MoveNode MoveNode;
        private IEntity _target;
    
#region IEntity

        public virtual async UniTaskVoid InitAsync()
        {
            MoveNode = new MoveNode(rigidBody, this);
        }

        public void UpdateNode()
        {
            NodeRunner?.OperateNode();
        }
        
        public Transform GetTransform()
        {
            return transform;
        }

        public IEntity GetTarget()
        {
            return _target;
        }
    
        public void SetTarget(IEntity iEntity)
        {
            _target = iEntity;
        }
    
        public Vector3 GetPosition()
        {
            return transform.position;
        }
    
        public void SetPosition(Vector3 position)
        {
            var pos = new Vector3(position.x, position.y, 0);
        
            transform.position = pos;
        }

        public bool GetActiveState()
        {
            return transform.gameObject.activeSelf;
        }

        public virtual void SetDirection(Vector3 direction)
        {
            spriteRenderer.flipX = direction.x switch
            {
                > 0 => false,
                < 0 => true,
                _ => spriteRenderer.flipX
            };
        }
        
        public abstract IEnumerable<IEntity> GetNearCheckEntities();
        public abstract void Hit(float damage, bool isCritical);
        
        public abstract float GetDamage();
        public abstract float GetAttackDistance();
        public abstract float GetAttackDelay();
        public abstract float GetMoveSpeed();
        public abstract float GetCriDamage();
        public abstract float GetCriRate();
        public abstract float GetExtraHitRate();
        
#endregion
    }
}