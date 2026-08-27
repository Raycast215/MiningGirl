using Data;

namespace Scene.MainGameScene.Progress
{
    public enum ELevelUpChoiceType
    {
        AcquireSkill, // 미보유 스킬을 얻습니다
        UpgradeSkill, // 보유 스킬에 강화 하나
        Mastery,      // 강화스킬. 조건을 채워야 나오고 런당 한 번만 고를 수 있습니다
    }

    // 강화스킬 조건 한 줄의 진행 상황. 3택 카드 하단에 표시합니다.
    public readonly struct MasteryRequirement
    {
        public readonly ESkillUpgradeType Type;
        public readonly int Current;
        public readonly int Required;

        // 이 카드를 고르면 올라가는 쪽인지. 강조 표시에 씁니다.
        public readonly bool IsAdvancedByThisCard;

        public MasteryRequirement(ESkillUpgradeType type, int current, int required, bool advanced)
        {
            Type = type;
            Current = current;
            Required = required;
            IsAdvancedByThisCard = advanced;
        }

        public bool IsMet => Current >= Required;
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

        // 이 선택지를 만들 때의 표시 레벨(획득 1 + 강화 합). 카드에 적는 데 씁니다.
        public int CurrentLevel { get; set; }

        // 강화가 이미 몇 번 쌓였는지.
        public int StackedUpgradeCount { get; set; }

        // Type이 Mastery일 때만 채워집니다.
        public SkillMasteryDataTableRow Mastery { get; set; }

        // 이 카드가 강화스킬 조건을 진행시킬 때 채워집니다.
        //
        // 조건 두 줄을 모두 담습니다. 하나만 보여주면 나머지 절반을 모른 채로
        // "다 됐다"고 오해하게 됩니다.
        public MasteryRequirement[] MasteryProgress { get; set; }

        // 진행도를 보여줄 강화스킬. MasteryProgress와 함께 채워집니다.
        public SkillMasteryDataTableRow MasteryHint { get; set; }
    }
}
