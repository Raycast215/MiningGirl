using System;
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

        [SerializeField]
        [Tooltip("카드로 조준됐을 때 머리 위 표시를 얼마나 띄울지(월드 단위)")]
        private float targetMarkHeight = 1f;

        // 조준 표시 자체는 엔티티가 가지지 않고 TargetMarkController가 풀로 발급합니다.
        // 여기서는 "얼마나 위에 띄울지"만 알려줍니다(광물은 낮고 몬스터는 조금 높습니다).
        public float TargetMarkHeight => targetMarkHeight;
        
        protected NodeRunner NodeRunner;
        protected MoveNode MoveNode;
        protected IEntity Target;
    
#region IEntity

public string GetId()
{
    return "";
}

public void SetId(string id)
{
    
}

public void SetActiveObject(bool isActive)
{
    gameObject.SetActive(isActive);
}

public void SetReleaseCallback(Action<IEntity> callback)
{
    
}

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
            return Target;
        }
    
        public void SetTarget(IEntity iEntity)
        {
            Target = iEntity;
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

        // 기본적으로 모든 엔티티는 공격 대상이 됩니다. 필요하면 파생 클래스에서 막습니다.
        // 이동을 즉시 멈춥니다.
        // MovePosition으로 움직이기 때문에 노드 실행만 멈추면 리지드바디에 남은 속도로 계속 미끄러집니다.
        public void StopMove()
        {
            if (rigidBody == null)
                return;

            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }

        public virtual bool IsAttackable()
        {
            return GetActiveState();
        }
        
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