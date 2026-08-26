using Data;

namespace Scene.MainGameScene.Progress
{
    public enum ELevelUpChoiceType
    {
        AcquireSkill, // 미보유 스킬을 Lv.1로
        LevelUpSkill, // 보유 스킬의 레벨 +1
        UpgradeSkill, // 보유 스킬(Lv.2+)에 강화 하나
    }

    // 3택에 올라가는 선택지 하나. 무엇을 고르는지만 담습니다.
    //
    // 화면에 뭐라고 적을지는 ViewModel이 정합니다. 규칙 계층이 표시 문구를 들고 있으면
    // 문구를 고칠 때마다 게임 로직 파일을 건드리게 됩니다.
    public class LevelUpChoice
    {
        public ELevelUpChoiceType Type { get; set; }

        public SkillDataTableRow Skill { get; set; }

        // Type이 UpgradeSkill일 때만 채워집니다.
        public SkillUpgradeDataTableRow Upgrade { get; set; }

        public int Weight { get; set; }

        // 이 선택지를 만들 때의 스킬 레벨. 카드에 "Lv.2 → Lv.3"을 적는 데 씁니다.
        public int CurrentLevel { get; set; }

        // 강화가 이미 몇 번 쌓였는지.
        public int StackedUpgradeCount { get; set; }
    }
}
