using System.Collections.Generic;
using UnityEngine;

namespace InGame.temp.System.FloatingDamage
{
    public class DamageController : GameMonoInitializer, MainGame.Entity.IFloatingDamagePresenter
    {
        [SerializeField]
        private Damage prefab;
        [SerializeField] 
        private int poolCount = 10;
        
        private Queue<Damage> _queue;
        
        public void InitAsync()
        {
            if (IsInitialized)
                return;
            
            LoadDamageObject();
        }
        
        // IFloatingDamagePresenter 구현 — 몬스터 등 외부에서는 이 인터페이스를 통해 호출합니다.
        public void Show(int damage, Vector2 position, bool isCritical = false)
        {
            Damage(damage, position, isCritical);
        }

        public void Damage(int damage, Vector2 pos, bool isCritical = false)
        {
            Damage dmg;
      
            if (_queue == null || _queue.Count == 0)
            {
                dmg = Instantiate(prefab, transform);
                dmg.gameObject.SetActive(false);
            }
            else
            {
                dmg = _queue.Dequeue();
            }
      
            dmg.Init(damage, pos, PoolRelease, isCritical);
        }
        
        private void LoadDamageObject()
        {
            _queue ??= new Queue<Damage>();

            for (var i = 0; i < poolCount; i++)
            {
                var ins = Instantiate(prefab, transform);
         
                ins.gameObject.SetActive(false);
                _queue.Enqueue(ins);
            }
      
            IsInitialized = true;
        }
        
        private void PoolRelease(Damage poolObject)
        {
            poolObject.gameObject.SetActive(false);
            _queue.Enqueue(poolObject);
        }

        // 현재 화면에 떠 있는 데미지 오브젝트를 모두 즉시 풀로 되돌립니다.
        // (Next 등으로 판을 리셋할 때 남아있는 데미지 표시를 정리하는 용도)
        public void Clear()
        {
            var damages = GetComponentsInChildren<Damage>(true);
            foreach (var dmg in damages)
            {
                dmg.ForceRelease();
            }
        }
    }
}