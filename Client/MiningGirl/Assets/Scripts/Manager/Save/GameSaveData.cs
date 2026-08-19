using System;
using System.Collections.Generic;

namespace Manager.Save
{
    // 로컬 JSON으로 저장되는 게임 진행 상태.
    // 앱을 껐다 켜도 이 값들로 이어서 시작합니다.
    [Serializable]
    public class GameSaveData
    {
        // 저장 포맷이 바뀌었을 때 옛 파일을 걸러내기 위한 값입니다.
        public int Version = 1;

        // 진행 중인 스테이지 번호(1부터).
        public int Stage = 1;

        // 이번 런에서 누적된 골드.
        public int Gold;

        // 선택한 캐릭터. 비어 있으면 아직 고르지 않은 것으로 봅니다.
        public string CharacterId = string.Empty;

        // 강화 항목별 레벨(스킬 Id → 레벨).
        public List<UpgradeLevelEntry> Upgrades = new List<UpgradeLevelEntry>();

        // 강화 페이즈 도중에 앱을 껐는지.
        // true면 다음 실행에서 강화 팝업부터 다시 띄웁니다.
        public bool IsUpgradePhase;

        // 강화 페이즈에 들어간 이유(클리어인지 실패인지). 팝업 문구가 달라집니다.
        public bool IsUpgradeFromClear;

        // 저장 시각(문제 추적용).
        public string SavedAt = string.Empty;

        public bool HasCharacter => !string.IsNullOrEmpty(CharacterId);
    }

    // JsonUtility는 Dictionary를 직렬화하지 못해 리스트로 풀어서 저장합니다.
    [Serializable]
    public class UpgradeLevelEntry
    {
        public string Id;
        public int Level;

        public UpgradeLevelEntry() { }

        public UpgradeLevelEntry(string id, int level)
        {
            Id = id;
            Level = level;
        }
    }
}
