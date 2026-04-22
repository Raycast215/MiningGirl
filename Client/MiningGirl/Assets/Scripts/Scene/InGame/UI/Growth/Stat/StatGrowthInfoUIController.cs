using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scene.InGame.UI.Growth.Stat
{
    public class StatGrowthInfoUIController : GameInitializer
    {
        [SerializeField]
        private List<StatGrowthInfoUI> uiList;

        private Dictionary<EStatType, DamageStatLogicBase> _dic;
        
        public void Init(IInGameDataHandler inGameDataHandler)
        {
            _dic = new Dictionary<EStatType, DamageStatLogicBase>
            {
                { EStatType.Damage, new DamageStatLogic(uiList[0], inGameDataHandler) },
                { EStatType.AttackDelay, new AttackDelayStatLogic(uiList[1], inGameDataHandler) },
                { EStatType.MoveSpeed, new MoveSpeedStatLogic(uiList[2], inGameDataHandler) },
                { EStatType.CriDamage, new CriDamageStatLogic(uiList[3], inGameDataHandler) },
                { EStatType.CriRate, new CriRateStatLogic(uiList[4], inGameDataHandler) },
                { EStatType.ExtraHitRate, new ExtraHitRateStatLogic(uiList[5], inGameDataHandler) },
            };
        }
    }
}