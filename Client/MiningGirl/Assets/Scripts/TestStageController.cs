using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestStageController : GameInitializer
{
   [SerializeField] 
   private TestPlayer player;
   [SerializeField] 
   private TestRock rock;

   [Header("Test Prefab")]
   [SerializeField]
   private DamageFloating damageFloating;
   [SerializeField] 
   private int poolCount = 10;
   [SerializeField] 
   private Transform poolHolder;

   private Queue<DamageFloating> _damageFloatingQueue;
   
   
   private async void Start()
   {
      LoadDamageObject();
      
      await UniTask.WaitUntil(() => IsInitialized);
      player.Init(rock, Damage);
   }

   private void LoadDamageObject()
   {
      _damageFloatingQueue ??= new Queue<DamageFloating>();

      for (var i = 0; i < poolCount; i++)
      {
         var ins = Instantiate(damageFloating, poolHolder);
         
         ins.gameObject.SetActive(false);
         _damageFloatingQueue.Enqueue(ins);
      }
      
      IsInitialized = true;
   }

   private void Damage(int damage, Vector2 pos)
   {
      DamageFloating dmg = null;
      
      if (_damageFloatingQueue == null || _damageFloatingQueue.Count == 0)
      {
         damageFloating = Instantiate(damageFloating, transform);
         damageFloating.gameObject.SetActive(false);
      }
      else
      {
         dmg = _damageFloatingQueue.Dequeue();
      }
      
      dmg.Init(damage, pos, PoolRelease);
   }
   
   private void PoolRelease(DamageFloating poolObject)
   {
      poolObject.gameObject.SetActive(false);
      _damageFloatingQueue.Enqueue(poolObject);
   }
}