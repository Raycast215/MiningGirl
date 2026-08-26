using System;
using System.Collections.Generic;
using Data;
using Scene.MainGameScene.Progress;

namespace Scene.MainGameScene.ViewModel
{
    // 3택 카드 한 장에 그릴 내용. 전부 완성된 문자열입니다.
    public readonly struct LevelUpChoiceItem
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string Detail;
        public readonly string IconAssetId;
        public readonly bool IsNew;

        public LevelUpChoiceItem(string title, string subtitle, string detail, string iconAssetId, bool isNew)
        {
            Title = title;
            Subtitle = subtitle;
            Detail = detail;
            IconAssetId = iconAssetId;
            IsNew = isNew;
        }
    }

    // 레벨업 3택의 표시용 상태와 커맨드.
    //
    // 무엇을 고를 수 있는지는 LevelUpChoiceBuilder(Model)가 정하고,
    // 그걸 뭐라고 적을지는 여기서 정합니다.
    public class LevelUpChoiceViewModel
    {
        // 컨트롤러가 구독합니다. 고른 결과를 실제로 적용하는 건 컨트롤러 몫입니다.
        public event Action<LevelUpChoice> Selected;

        public ObservableProperty<bool> IsVisible { get; } = new ObservableProperty<bool>();
        public ObservableProperty<string> HeaderText { get; } = new ObservableProperty<string>(string.Empty);

        // 카드 내용이 갈릴 때마다 올라갑니다.
        public ObservableProperty<int> ItemRevision { get; } = new ObservableProperty<int>();

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
            _choices.Clear();
            _items.Clear();

            foreach (var choice in choices)
            {
                _choices.Add(choice);
                _items.Add(BuildItem(choice));
            }

            HeaderText.Value = $"LEVEL {level}";
            ItemRevision.Value = ++_revision;
            IsVisible.Value = true;
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
                        false);
                }
            }
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
