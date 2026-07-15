using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scene.InGame.UI.Growth.Stat
{
    public class StatGrowthInfoUIController : GameMonoInitializer
    {
        [SerializeField]
        private List<StatGrowthInfoUI> uiList;

        private Dictionary<EStatType, StatLogicBase> _dic;
        
        public void Init(IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataHandler)
        {
            _dic = new Dictionary<EStatType, StatLogicBase>
            {
                { EStatType.Damage, new DamageStatLogic(uiList[0], inGameUIHandler, inGameDataHandler) },
                { EStatType.AttackDelay, new AttackDelayStatLogic(uiList[1], inGameUIHandler, inGameDataHandler) },
                { EStatType.CriDamage, new CriStatLogic(uiList[2], inGameUIHandler, inGameDataHandler) },
                { EStatType.CriRate, new CriRateStatLogic(uiList[3], inGameUIHandler, inGameDataHandler) },
                { EStatType.ExtraHitRate, new ExtraHitRateStatLogic(uiList[4], inGameUIHandler, inGameDataHandler) },
            };
        }

        public void RefreshUI()
        {
            foreach (var data in _dic)
            {
                data.Value.RefreshUI();
            }
        }
    }
}