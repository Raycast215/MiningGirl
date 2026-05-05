using System;
using System.Linq;
using Data;
using Manager;
using UnityEngine;

namespace GameData
{
    [Serializable]
    public class GameStageData
    {
        public string SaveKey => "GameStageData";
        
        public string CharacterId { get; set; }
        
        public int ChapterId { get; set; }
        public int StageIndex { get; set; }
        
        public void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return;
            
            var data =  PlayerPrefs.GetString(SaveKey);
            var fromJson = JsonUtility.FromJson<GameStageData>(data);
            
            CharacterId = fromJson.CharacterId;
            ChapterId = fromJson.ChapterId;
            StageIndex = fromJson.StageIndex;
        }
        
        public void Set(string characterId, int chapterId, int stageIndex)
        {
            CharacterId = characterId;
            ChapterId = chapterId;
            StageIndex = stageIndex;
            
            Save();
        }

        public StageInfoTableRow GetRow()
        {
            var row = DataTableManager.Instance.StageInfoTable.Rows
                .FirstOrDefault(x => x.Index == StageIndex);

            return row;
        }
        
        public void NextStage()
        {
            var infoTable = DataTableManager.Instance.StageInfoTable.Rows
                .Where(x => x.IsVisible)
                .OrderBy(x => x.Index)
                .Select(x => x)
                .ToList();
            
            foreach (var row in infoTable)
            {
                if (row.Index <= StageIndex)
                    continue;

                if (row.StageType == EStageType.Boss)
                {
                    StageIndex = 0;
                    Save();
                    break;
                }
                
                StageIndex = row.Index;
                Save();
                break;
            }
        }

        public void Clear()
        {
            
        }
        
        private void Save()
        {
            var toJson = JsonUtility.ToJson(this);
            
            PlayerPrefs.SetString(SaveKey, toJson);
            PlayerPrefs.Save();
            
            Debug.Log("Save Game");
        }
    }
}