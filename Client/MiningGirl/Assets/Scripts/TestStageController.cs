using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using InGame;
using InGame.System;
using InGame.System.FloatingDamage;
using UnityEngine;
using UnityEngine.Serialization;

public class TestStageController : GameInitializer
{
   [Header("Timer")]
   [SerializeField] 
   private float time;
   [SerializeField]
   private Timer timer;
   
   [Header("Damage Floating")]
   [SerializeField]
   private FloatingDamageController floatingDamageController;
   
   [Header("UI")]
   [SerializeField]
   private StatViewer statViewer;
   
   [SerializeField] 
   private TestPlayer player;
   [SerializeField] 
   private TestRock rock;
   
   private async void Start()
   {
      // 작업해야할거
      // 데이터 로드
      // 테스트 스테이지 초기화
      // 
      
      
      
      floatingDamageController.InitAsync().Forget();
      await UniTask.WaitUntil(() => floatingDamageController.IsInitialized);

      var load = await AddressableSheetsDataManager.LoadLabelAsync("DataTable");
      await UniTask.WaitUntil(() => load == ELoadResponseType.Success);
      
      var playerStatRow = AddressableSheetsDataManager.GetAll<PlayerStatTable>();

      // foreach (var d in playerStatRow)
      // {
      //    Debug.Log(d.Str);
      //    Debug.Log(d.Dex);
      //    Debug.Log(d.Luk);
      // }

      var playerRow = playerStatRow.FirstOrDefault();
      
      statViewer.Set(playerRow);
      statViewer.OnLevelUpdated += player.SetLevel;
      
      player.Init(playerRow, rock, floatingDamageController.Damage);
      
      timer.Init(time, null);
      timer.Execute().Forget();


      for (int i = 1; i <= 10; i++)
      {
         var stat = new CalcPlayerStat(i, playerRow);
         
         Debug.Log($"Str: {stat.Str}, Dex: {stat.Dex}, Luk: {stat.Luk}");
      }
   }
}