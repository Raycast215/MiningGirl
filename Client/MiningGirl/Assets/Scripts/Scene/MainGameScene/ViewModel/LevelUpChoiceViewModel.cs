using System;
using System.Collections.Generic;
using Data;
using Scene.MainGameScene.Progress;

namespace Scene.MainGameScene.ViewModel
{
    // 3택 카드 한 장에 그릴 내용. 전부 완성된 문자열입니다.
    // 카드 하단에 뜨는 강화스킬 조건 한 줄.
    public readonly struct MasteryProgressItem
    {
        // 진행도 숫자만 담습니다. 종류는 아이콘이 말하므로 이름을 붙이지 않습니다 -
        // "위력 강화" 카드에 "위력"이라고 또 적으면 같은 말이 두 번 나옵니다.
        public readonly string Text;

        // 아이콘을 고르는 열쇠. 어떤 그림을 쓸지는 View가 정합니다.
        public readonly ESkillUpgradeType Type;

        // 이 카드를 고르면 올라가는 쪽인지. 강조 표현은 View가 정합니다.
        public readonly bool IsAdvancing;

        public readonly bool IsMet;

        public MasteryProgressItem(string text, ESkillUpgradeType type, bool isAdvancing, bool isMet)
        {
            Text = text;
            Type = type;
            IsAdvancing = isAdvancing;
            IsMet = isMet;
        }
    }

    public readonly struct LevelUpChoiceItem
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string Detail;
        public readonly string IconAssetId;
        public readonly bool IsNew;

        // 강화스킬 자체를 고르는 카드인지. 런당 한 번뿐이라 다르게 보여야 합니다.
        public readonly bool IsMastery;

        // 강화스킬 조건 진행도. 없으면 null입니다.
        public readonly string MasteryHintName;
        public readonly string MasteryHintIconAssetId;
        public readonly MasteryProgressItem[] MasteryProgress;

        public LevelUpChoiceItem(
            string title,
            string subtitle,
            string detail,
            string iconAssetId,
            bool isNew,
            bool isMastery = false,
            string masteryHintName = null,
            string masteryHintIconAssetId = null,
            MasteryProgressItem[] masteryProgress = null)
        {
            Title = title;
            Subtitle = subtitle;
            Detail = detail;
            IconAssetId = iconAssetId;
            IsNew = isNew;
            IsMastery = isMastery;
            MasteryHintName = masteryHintName;
            MasteryHintIconAssetId = masteryHintIconAssetId;
            MasteryProgress = masteryProgress;
        }
    }

    // 다시 뽑기 버튼이 꺼지는 이유. 표현은 View가 정합니다.
    public enum ERerollBlockReason
    {
        None,           // 누를 수 있음
        Exhausted,      // 남은 횟수 0
        NotEnoughPool,  // 후보가 제시 장수 이하라 다시 뽑아도 같은 카드만 나옴
    }

    // 레벨업 3택의 표시용 상태와 커맨드.
    //
    // 무엇을 고를 수 있는지는 LevelUpChoiceBuilder(Model)가 정하고,
    // 그걸 뭐라고 적을지는 여기서 정합니다.
    public class LevelUpChoiceViewModel
    {
        // 컨트롤러가 구독합니다. 고른 결과를 실제로 적용하는 건 컨트롤러 몫입니다.
        public event Action<LevelUpChoice> Selected;

        // 다시 뽑기를 눌렀을 때. 실제로 다시 뽑는 건 컨트롤러 몫입니다 -
        // 후보를 만드는 규칙은 Model에 있고 ViewModel이 그걸 알 이유가 없습니다.
        public event Action RerollRequested;

        public ObservableProperty<bool> IsVisible { get; } = new ObservableProperty<bool>();
        public ObservableProperty<string> HeaderText { get; } = new ObservableProperty<string>(string.Empty);

        // 카드 내용이 갈릴 때마다 올라갑니다.
        public ObservableProperty<int> ItemRevision { get; } = new ObservableProperty<int>();

        // 남은 다시 뽑기 횟수. 쓴 횟수가 아니라 남은 횟수입니다.
        public ObservableProperty<int> RemainingRerolls { get; } = new ObservableProperty<int>();

        // 버튼을 누를 수 있는지. 0회여도 버튼을 숨기지 않고 끄기만 합니다 -
        // 사라지면 원래 없는 기능으로 읽힙니다.
        public ObservableProperty<bool> CanReroll { get; } = new ObservableProperty<bool>();

        // 왜 못 누르는지. 횟수 소진과 후보 부족은 구분되어야 합니다.
        public ObservableProperty<ERerollBlockReason> RerollBlockReason { get; } =
            new ObservableProperty<ERerollBlockReason>();

        public IReadOnlyList<LevelUpChoiceItem> Items => _items;

        private readonly List<LevelUpChoiceItem> _items = new List<LevelUpChoiceItem>();
        private readonly List<LevelUpChoice> _choices = new List<LevelUpChoice>();
        private readonly SkillInventory _inventory;

        private int _revision;

        public LevelUpChoiceViewModel(SkillInventory inventory)
        {
            _inventory = inventory;
        }

        public void Show(int level, IReadOnlyList<LevelUpChoice> choices)
        {
            SetChoices(choices);

            HeaderText.Value = $"LEVEL {level}";
            IsVisible.Value = true;
        }

        // 헤더와 표시 상태는 그대로 두고 카드만 갈아 끼웁니다.
        // 다시 뽑기는 같은 레벨업 안에서 일어나므로 창을 다시 여는 게 아닙니다.
        public void Replace(IReadOnlyList<LevelUpChoice> choices)
        {
            SetChoices(choices);
        }

        // 남은 횟수와 누를 수 있는지를 컨트롤러가 정해 넣어 줍니다.
        public void SetRerollState(int remaining, ERerollBlockReason blockReason)
        {
            RemainingRerolls.Value = remaining;
            RerollBlockReason.Value = blockReason;
            CanReroll.Value = blockReason == ERerollBlockReason.None;
        }

        private void SetChoices(IReadOnlyList<LevelUpChoice> choices)
        {
            _choices.Clear();
            _items.Clear();

            foreach (var choice in choices)
            {
                _choices.Add(choice);
                _items.Add(BuildItem(choice));
            }

            ItemRevision.Value = ++_revision;
        }

        // View의 버튼이 부르는 커맨드입니다.
        public void Reroll()
        {
            if (!IsVisible.Value || !CanReroll.Value)
                return;

            RerollRequested?.Invoke();
        }

        public void Hide()
        {
            IsVisible.Value = false;
        }

        // View의 버튼이 부르는 커맨드입니다.
        public void Select(int index)
        {
            // 닫힌 뒤에 들어온 입력은 무시합니다.
            // 카드가 숨겨진 상태에서 클릭이 한 번 더 들어오면 같은 선택이 두 번 적용됩니다.
            if (!IsVisible.Value)
                return;

            if (index < 0 || index >= _choices.Count)
                return;

            var choice = _choices[index];

            Hide();

            Selected?.Invoke(choice);
        }

        private LevelUpChoiceItem BuildItem(LevelUpChoice choice)
        {
            switch (choice.Type)
            {
                case ELevelUpChoiceType.AcquireSkill:
                {
                    var damage = SkillState.ValueAtLevel(choice.Skill.EffectValueList, 1);
                    var cooldown = SkillState.ValueAtLevel(choice.Skill.CooldownList, 1);

                    // 가운뎃점(·)은 NotoSansKR SDF 아틀라스에 없어 네모로 나옵니다.
                    return new LevelUpChoiceItem(
                        choice.Skill.Name,
                        "새로 획득",
                        $"위력 {damage:0} / 쿨 {cooldown:0.0}초",
                        choice.Skill.IconAssetId,
                        true);
                }

                case ELevelUpChoiceType.LevelUpSkill:
                {
                    var owned = _inventory.Find(choice.Skill.Id);
                    var current = owned?.Damage ?? 0f;
                    var next = owned?.GetDamageAtLevel(choice.CurrentLevel + 1) ?? 0f;

                    return new LevelUpChoiceItem(
                        choice.Skill.Name,
                        $"Lv.{choice.CurrentLevel} → Lv.{choice.CurrentLevel + 1}",
                        $"위력 {current:0} → {next:0}",
                        choice.Skill.IconAssetId,
                        false);
                }

                case ELevelUpChoiceType.Mastery:
                {
                    return new LevelUpChoiceItem(
                        choice.Mastery.Name,
                        $"{choice.Skill.Name} 강화스킬",
                        choice.Mastery.Desc ?? string.Empty,
                        choice.Mastery.IconAssetId ?? choice.Skill.IconAssetId,
                        true,
                        true);
                }

                default:
                {
                    var subtitle = choice.StackedUpgradeCount > 0
                        ? $"{choice.Skill.Name} 강화 (누적 {choice.StackedUpgradeCount}회)"
                        : $"{choice.Skill.Name} 강화";

                    return new LevelUpChoiceItem(
                        choice.Upgrade.Name,
                        subtitle,
                        FormatUpgradeDetail(choice.Upgrade),
                        choice.Skill.IconAssetId,
                        false,
                        false,
                        choice.MasteryHint?.Name,
                        choice.MasteryHint?.IconAssetId,
                        BuildMasteryProgress(choice));
                }
            }
        }

        // 조건 두 줄을 문자열로 만듭니다.
        //
        // 하나만 보여주면 나머지 절반을 모른 채 "다 됐다"고 오해합니다.
        // 이 카드가 올리는 쪽은 IsAdvancing으로 표시하고, 강조 표현은 View가 정합니다.
        private static MasteryProgressItem[] BuildMasteryProgress(LevelUpChoice choice)
        {
            var source = choice.MasteryProgress;

            if (source == null || source.Length == 0)
                return null;

            var items = new MasteryProgressItem[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                var requirement = source[i];

                items[i] = new MasteryProgressItem(
                    $"{requirement.Current}/{requirement.Required}",
                    requirement.Type,
                    requirement.IsAdvancedByThisCard,
                    requirement.IsMet);
            }

            return items;
        }

        private static string FormatUpgradeDetail(SkillUpgradeDataTableRow upgrade)
        {
            // 곱연산은 0.2가 아니라 20%로 보여야 읽힙니다.
            var shown = upgrade.ValueType == EEffectValueType.Mul
                ? Math.Round(upgrade.EffectValue * 100f).ToString("0")
                : upgrade.EffectValue.ToString("0.##");

            if (string.IsNullOrEmpty(upgrade.Desc))
                return shown;

            try
            {
                return string.Format(upgrade.Desc, shown);
            }
            catch (FormatException)
            {
                // 시트의 설명문에 중괄호가 잘못 들어가도 3택이 통째로 터지지 않게 감쌉니다.
                UnityEngine.Debug.LogWarning($"[LevelUp] 설명문 서식이 잘못됐습니다: {upgrade.Desc}");

                return upgrade.Desc;
            }
        }
    }
}
