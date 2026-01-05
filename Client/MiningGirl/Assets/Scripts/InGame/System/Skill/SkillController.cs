using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using InGame.System.Skill.Logic;
using InGame.System.Skill.UI;
using Manager;
using UnityEngine;

namespace InGame.System.Skill
{
    public class GameSettingData
    {
        public int HandCount { get; set; }
        public int MaxCost  { get; set; }
        public float CostUpdateTime { get; set; }
        public int CostIncreaseCount { get; set; }
    }

    public interface ISkillDataHandler
    {
        SkillDataRowTable GetSkillData();
        void ExecuteSkillEffect(SkillDataRowTable data);
    }
    
    public class SkillController : GameInitializer, ISkillDataHandler, IDisposable
    {
        [SerializeField]
        private SkillUIController skillUIController;

        private GameSettingData _gameSettingData;
        private bool _isGameStarted;
        private CancellationTokenSource _cts;
        private SkillCardDrawController _cardDrawController;
        private IInGameHandler _inGameHandler;
        
        public async void Init(IInGameHandler inGameHandler)
        {
            IsInitialized = false;
            _isGameStarted = false;
            
            _inGameHandler = inGameHandler;
            
            _gameSettingData = new GameSettingData
            {
                MaxCost = 10,
                CostUpdateTime = 2.0f,
                CostIncreaseCount = 1,
            };

            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);
            
            _cardDrawController = new SkillCardDrawController();
            skillUIController.Init(this, _gameSettingData.MaxCost);
            
            IsInitialized = true;
        }

        public void StopProcess()
        {
            _isGameStarted = false;
            Dispose();
        }
        
        public void Appear()
        {
            skillUIController.AppearUI().Forget();
        }

        public async UniTaskVoid ExecuteSkillPointGauge()
        {
            _isGameStarted = true;
            Dispose();
            _cts ??= new CancellationTokenSource();
            
            try
            {
                while (_isGameStarted)
                {
                    await UniTask.WaitForSeconds(_gameSettingData.CostUpdateTime, cancellationToken: _cts.Token);
                    
                    // 2초마다 스킬 포인트 1 회복
                    skillUIController.GetSkillPointGaugeUIHandler().UpdateGaugeUI(_gameSettingData.CostIncreaseCount);
                }
            }
            catch (OperationCanceledException)
            {
                _isGameStarted = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _isGameStarted = false;
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

#region ISkillDataHandler

        public SkillDataRowTable GetSkillData()
        {
            var next = _cardDrawController.GetSkillData();

            while (next.StartDrawCount >= _cardDrawController.DrawCount)
            {
                next = _cardDrawController.GetSkillData();
            }
            
            return next;
        }

        public void ExecuteSkillEffect(SkillDataRowTable data)
        {
            foreach (var effectID in data.EffectList)
            {
                var effectTable = DataTableManager.Instance.SkillEffectDataTable.GetRow(effectID);
                var effectType = effectTable.EffectType;
                
                switch (effectType)
                {
                    case ESkillEffectType.TargetHit:
                        new TargetHit(data, effectTable, _inGameHandler.GetPlayerTarget(), _inGameHandler);
                        break;
                
                    case ESkillEffectType.IncreaseCost:
                        new IncreaseCost(data, skillUIController.GetSkillPointGaugeUIHandler().UpdateGaugeUI);
                        break;
                
                    case ESkillEffectType.Draw:
                        break;
                    
                    case ESkillEffectType.RangeAll:
                        // new RangeAll(data, effectTable, _inGameHandler);
                        RangeAll.Execute(data, effectTable, _inGameHandler).Forget();
                        break;
                }
            }
        }

#endregion
        
#region IDisposable

        public void Dispose()
        {
            if (_cts == null)
                return;
            
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

#endregion
    }
}