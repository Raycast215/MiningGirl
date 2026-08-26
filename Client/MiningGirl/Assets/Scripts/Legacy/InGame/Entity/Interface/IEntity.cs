using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Legacy.Scene.InGame.Entity.Interface
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

        public IReadOnlyList<IEntity> GetNearCheckEntities();
        // isExtraHit: 추가타로 들어온 덤 타격입니다.
        // 광물은 이 값을 보고 채굴 '시도' 횟수(=스태미나 소모)에서 제외합니다.
        public void Hit(float damage, bool isCritical, bool isExtraHit = false);

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
