using System;
using System.Collections.Generic;

namespace Manager.Save
{
    // 스테이지 한 판의 진행 상태.
    //
    // 앱을 껐다 켜도 "종료한 그 순간"으로 돌아오게 하는 게 목적입니다.
    //
    // 값이 아니라 id를 저장합니다. 데미지 20.74나 폭발 반지름 2.6 같은 계산 결과가
    // 아니라 Skill_FireBolt / Upg_FireBolt_Damage 3회 같은 것만 담습니다. 시트가
    // 바뀌면 최신 값으로 다시 계산되어야 하기 때문입니다 - 밸런스는 최신을 따르는
    // 게 맞고, 그래야 시트를 고칠 때마다 저장이 깨지지 않습니다.
    //
    // 예외는 현재 체력입니다. 비율로 저장하면 시트의 최대 체력이 바뀔 때 체력이
    // 되살아나거나 줄어듭니다. 절대값으로 담고 복원할 때 최대값으로 자릅니다.
    [Serializable]
    public class RunSaveData
    {
        // 저장 구조가 바뀔 때만 올립니다. 시트 값이 바뀌었다고 올리지 마십시오.
        public int SchemaVersion;

        public string StageId;
        public string CharacterId;

        // 진행 시간. 5분 캡 확인과 진입 구간 계측에 씁니다.
        public float Elapsed;
        public float FirstLevelUpTime;

        public WaveSave Wave = new WaveSave();
        public LevelSave Level = new LevelSave();
        public ChoiceSave Choice = new ChoiceSave();

        public float TowerHealth;

        public List<SkillSave> Skills = new List<SkillSave>();
        public List<MonsterSave> Monsters = new List<MonsterSave>();

        // 저장 시각(문제 추적용). 복원 판정에는 쓰지 않습니다.
        public string SavedAt = string.Empty;
    }

    [Serializable]
    public class WaveSave
    {
        // StartDelay / Running / Gap / Finished
        public string Phase;

        public int WaveIndex;
        public float Timer;

        // 이 웨이브에서 몇 마리까지 내보냈는가.
        public int ScheduleIndex;

        // 복원 때 다시 만든 스케줄과 길이가 같은지 대조합니다.
        // 시트의 그 웨이브 행이 바뀌면 길이가 달라지고, 그러면 복원을 중단합니다.
        public int ScheduleCount;
    }

    [Serializable]
    public class LevelSave
    {
        public int Level;

        // 이번 구간에서 번 경험치와 누적 경험치.
        //
        // 예전에는 처치 수로 저장했습니다. 그 저장은 지금 코드로 복원하면
        // 경험치를 처치 수만큼으로 읽어 레벨이 크게 어긋나므로,
        // SchemaVersion을 올려 통째로 버립니다.
        public int ExpInLevel;
        public int TotalExp;

        // 레벨과는 무관합니다. 결과 화면과 기록에만 씁니다.
        public int TotalKills;

        // 아직 고르지 않은 레벨업 횟수. 3택이 밀려 있을 수 있습니다.
        public int PendingLevelUps;
    }

    [Serializable]
    public class ChoiceSave
    {
        public int RerollsLeft;

        // 직전에 보여 준 3택 조합. 같은 카드가 다시 나오지 않게 하는 데 씁니다.
        public List<string> ShownKeys = new List<string>();

        // 3택이 열린 채로 종료했는가.
        public bool PanelOpen;

        // 열려 있었다면 그 세 장. 새로 뽑으면 무료 리롤이 됩니다 -
        // 리롤 10회를 자원으로 쓰는 설계인데 앱을 껐다 켜서 우회하면 안 됩니다.
        public List<string> OpenKeys = new List<string>();
    }

    [Serializable]
    public class SkillSave
    {
        public string SkillId;

        // 강화스킬 Id. 안 골랐으면 빕니다.
        public string MasteryId;

        public float CooldownRemaining;

        // 종류별로 몇 번 넣었는가. 누적된 값이 아니라 횟수입니다.
        //
        // 복원할 때 ApplyUpgrade를 그 횟수만큼 다시 돌리면 합연산·곱연산·종류별
        // 횟수·총 횟수가 전부 되살아납니다. 값을 저장하면 시트의 EffectValue가
        // 바뀌었을 때 옛 값이 굳습니다.
        public List<UpgradeCountSave> UpgradeCounts = new List<UpgradeCountSave>();
    }

    [Serializable]
    public class UpgradeCountSave
    {
        // ESkillUpgradeType의 이름입니다. 정수로 담으면 열거형 순서가 바뀔 때 깨집니다.
        public string Type;
        public int Count;

        public UpgradeCountSave() { }

        public UpgradeCountSave(string type, int count)
        {
            Type = type;
            Count = count;
        }
    }

    [Serializable]
    public class MonsterSave
    {
        public string MonsterId;

        public float X;
        public float Y;

        public float Health;
        public float AttackTimer;

        public float FreezeRemaining;
        public float BurnRemaining;

        // 이것만 예외적으로 값을 담습니다. 때린 스킬의 위력에서 나온 값이라
        // id로 되돌릴 수 없고, 화상을 건 시점의 위력이라 지금과 다를 수도 있습니다.
        // 최대 3초짜리라 영향이 작습니다.
        public float BurnPerSecond;

        public bool HasReachedTower;
    }
}
