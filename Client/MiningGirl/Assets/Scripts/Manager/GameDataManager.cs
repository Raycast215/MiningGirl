using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameData;

namespace Manager
{
    public class GameDataManager : SingletonBase<GameDataManager>
    {
        public GameStageData GameStageData { get; private set; }
        public Dictionary<string, GameItemData> ItemDataDic { get; private set; }
    
        public async UniTaskVoid PreLoadData()
        {
            if (IsInitialized)
                return;
            
            GameStageData = new GameStageData();
            ItemDataDic = new Dictionary<string, GameItemData>();
            
            IsInitialized = true;
        }
    }
}