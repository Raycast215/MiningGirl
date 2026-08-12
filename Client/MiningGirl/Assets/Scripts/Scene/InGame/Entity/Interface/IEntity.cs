using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.InGame.Entity.Interface
{
    public interface IEntityObject
    {
        string GetId();
        void SetId(string id);
        
        Vector3 GetPosition();
        void SetPosition(Vector3 position);
        
        void SetActiveObject(bool isActive);
        void SetReleaseCallback(Action<IEntityObject> callback);
    }
    
    public interface IEntity
    {
        string GetId();
        void SetId(string id);
        void SetActiveObject(bool isActive);
        void SetReleaseCallback(Action<IEntity> callback);
        
        
        
        
        public UniTaskVoid InitAsync();
        public void UpdateNode();
        
        public Transform GetTransform();
        public IEntity GetTarget();
        public void SetTarget(IEntity iEntity);
        
        public Vector3 GetPosition();
        public void SetPosition(Vector3 position);
     
        public void SetDirection(Vector3 direction);

        public bool GetActiveState();

        public IEnumerable<IEntity> GetNearCheckEntities();
        public void Hit(float damage, bool isCritical);

        // 지금 공격 대상이 될 수 있는지. (예: 플레이어가 쓰러져 있으면 몬스터가 때리지 않습니다)
        public bool IsAttackable();
        
        
        public float GetDamage();
        public float GetAttackDelay();
        public float GetMoveSpeed();
        public float GetCriDamage();
        public float GetCriRate();
        public float GetExtraHitRate();
        public float GetAttackDistance();
    }
}