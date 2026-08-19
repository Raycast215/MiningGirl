using System;
using System.Collections.Generic;
using System.IO;
using Manager.Save;
using UnityEngine;

namespace Manager
{
    // 게임 진행 상태를 로컬 JSON 파일로 저장하고 불러옵니다.
    //
    // 저장 시점은 '스테이지가 끝날 때'와 '강화를 살 때'입니다.
    // 플레이 도중에는 저장하지 않습니다 — 중간 상태까지 복원하려면 몬스터 배치와
    // 스태미나까지 맞춰야 해서 복잡해지고, 얻는 것도 적습니다.
    public class GameDataManager : SingletonBase<GameDataManager>
    {
        private const string FileName = "save.json";

        private GameSaveData _data;

        // 기기마다 앱 전용 폴더가 다르므로 persistentDataPath를 씁니다.
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public GameSaveData Data => _data ??= ReadFile();

        public bool HasSave => File.Exists(SavePath);

        private GameSaveData ReadFile()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return new GameSaveData();

                var loaded = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));

                if (loaded == null)
                    return new GameSaveData();

                // 포맷이 바뀌면 옛 저장은 버리고 새로 시작합니다.
                if (loaded.Version != new GameSaveData().Version)
                {
                    Debug.LogWarning($"[Save] 저장 포맷이 달라 초기화합니다. (파일 {loaded.Version})");

                    return new GameSaveData();
                }

                Debug.Log($"[Save] 불러옴 — 스테이지 {loaded.Stage}, 골드 {loaded.Gold}, 강화중={loaded.IsUpgradePhase}");

                return loaded;
            }
            catch (Exception e)
            {
                // 파일이 깨져도 게임은 시작되어야 하므로 새 데이터로 진행합니다.
                Debug.LogError($"[Save] 불러오기 실패 — {e.Message}");

                return new GameSaveData();
            }
        }

        public void Save()
        {
            try
            {
                Data.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 저장 실패 — {e.Message}");
            }
        }

        // 스테이지가 끝났을 때(클리어·실패 모두) 호출합니다.
        // 강화 페이즈로 들어가므로 그 사실도 함께 남깁니다.
        public void SaveStageEnd(int stage, int gold, string characterId,
            IReadOnlyDictionary<string, int> upgrades, bool isCleared)
        {
            Data.Stage = stage;
            Data.Gold = gold;
            Data.CharacterId = characterId ?? string.Empty;
            Data.IsUpgradePhase = true;
            Data.IsUpgradeFromClear = isCleared;

            SetUpgrades(upgrades);
            Save();
        }

        // 강화를 하나 살 때마다 호출합니다. 사자마자 앱이 꺼져도 산 것이 남습니다.
        public void SaveUpgrade(int gold, IReadOnlyDictionary<string, int> upgrades)
        {
            Data.Gold = gold;

            SetUpgrades(upgrades);
            Save();
        }

        // 강화 팝업을 닫고 다음 스테이지로 넘어갈 때 호출합니다.
        public void SaveUpgradePhaseEnd(int nextStage)
        {
            Data.Stage = nextStage;
            Data.IsUpgradePhase = false;

            Save();
        }

        public void SaveCharacter(string characterId)
        {
            Data.CharacterId = characterId ?? string.Empty;

            Save();
        }

        // 저장을 지우고 처음부터 시작합니다.
        // 이 카드를 처음 보는지. 카드 정리 화면의 NEW 표시에 씁니다.
        public bool IsFirstSeenCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return false;

            return !Data.SeenCards.Contains(cardId);
        }

        // 얻어본 카드로 기록합니다.
        public void MarkCardSeen(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || Data.SeenCards.Contains(cardId))
                return;

            Data.SeenCards.Add(cardId);

            Save();
        }

        public void Clear()
        {
            _data = new GameSaveData();

            try
            {
                if (File.Exists(SavePath))
                    File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 삭제 실패 — {e.Message}");
            }
        }

        public Dictionary<string, int> GetUpgradeLevels()
        {
            var result = new Dictionary<string, int>();

            foreach (var entry in Data.Upgrades)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id))
                    continue;

                result[entry.Id] = entry.Level;
            }

            return result;
        }

        private void SetUpgrades(IReadOnlyDictionary<string, int> upgrades)
        {
            Data.Upgrades.Clear();

            if (upgrades == null)
                return;

            foreach (var pair in upgrades)
                Data.Upgrades.Add(new UpgradeLevelEntry(pair.Key, pair.Value));
        }

        // 홈 버튼 등으로 앱이 내려갈 때도 현재 상태를 남깁니다.
        private void OnApplicationPause(bool pause)
        {
            if (pause)
                Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
