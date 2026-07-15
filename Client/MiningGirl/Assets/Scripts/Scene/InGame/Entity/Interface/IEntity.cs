using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.InGame.Entity.Interface
{
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
        
        
        public float GetDamage();
        public float GetAttackDelay();
        public float GetMoveSpeed();
        public float GetCriDamage();
        public float GetCriRate();
        public float GetExtraHitRate();
        public float GetAttackDistance();
    }
}