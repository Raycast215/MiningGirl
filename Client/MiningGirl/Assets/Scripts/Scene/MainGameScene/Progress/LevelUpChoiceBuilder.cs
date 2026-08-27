using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Scene.MainGameScene.Progress
{
    // 3택 후보를 모으고 가중치대로 뽑습니다.
    //
    // 스킬이 3종뿐이어도 선택지가 마르지 않는 건 강화 덕분입니다.
    // 세 스킬이 모두 Lv.2를 넘기면 강화만 12가지고, 강화는 반복해서 쌓입니다.
    public class LevelUpChoiceBuilder
    {
        private readonly SkillDataTable _skillTable;
        private readonly SkillUpgradeDataTable _upgradeTable;
        private readonly SkillMasteryDataTable _masteryTable;
        private readonly SkillInventory _inventory;

        private readonly List<LevelUpChoice> _candidates = new List<LevelUpChoice>();

        // 직전 Draw가 시작할 때 후보가 몇 개였는지.
        //
        // 다시 뽑기 버튼을 켤지 정하는 데 씁니다. 후보가 제시 장수 이하면 다시
        // 뽑아도 같은 카드만 나오므로 횟수만 버리게 됩니다.
        public int LastCandidateCount { get; private set; }

        public LevelUpChoiceBuilder(
            SkillDataTable skillTable,
            SkillUpgradeDataTable upgradeTable,
            SkillMasteryDataTable masteryTable,
            SkillInventory inventory)
        {
            _skillTable = skillTable;
            _upgradeTable = upgradeTable;
            _masteryTable = masteryTable;
            _inventory = inventory;
        }

        public List<LevelUpChoice> Draw(int count)
        {
            CollectCandidates();

            LastCandidateCount = _candidates.Count;

            var picked = new List<LevelUpChoice>();

            // 뽑을 때마다 후보에서 빼면서 반복합니다.
            // 같은 스킬의 레벨업이 두 장 나오면 고를 의미가 없어서입니다.
            while (picked.Count < count && _candidates.Count > 0)
            {
                var total = 0;

                foreach (var candidate in _candidates)
                    total += Mathf.Max(0, candidate.Weight);

                if (total <= 0)
                    break;

                var roll = Random.Range(0, total);
                var index = 0;

                for (var i = 0; i < _candidates.Count; i++)
                {
                    roll -= Mathf.Max(0, _candidates[i].Weight);

                    if (roll >= 0)
                        continue;

                    index = i;

                    break;
                }

                picked.Add(_candidates[index]);
                _candidates.RemoveAt(index);
            }

            return picked;
        }

        // 선택지 하나를 한 줄 문자열로. 같은 조합인지 비교하고 저장에 담는 데 씁니다.
        //
        // 형식을 한 곳에만 둡니다. 중복 방지와 저장이 각자 만들면 둘이 어긋날 때
        // 저장에서 복원한 카드가 "본 적 없는 것"으로 취급됩니다.
        public static string ToKey(LevelUpChoice choice)
        {
            if (choice == null)
                return string.Empty;

            var skillId = choice.Skill != null ? choice.Skill.Id : string.Empty;
            var upgradeId = choice.Upgrade != null ? choice.Upgrade.Id : string.Empty;
            var masteryId = choice.Mastery != null ? choice.Mastery.Id : string.Empty;

            return $"{choice.Type}|{skillId}|{upgradeId}|{masteryId}";
        }

        // 저장에서 3택을 그대로 되살립니다.
        //
        // 다시 뽑지 않는 이유는 그게 무료 리롤이 되기 때문입니다 - 리롤 10회를
        // 자원으로 쓰는 설계인데 앱을 껐다 켜서 우회하면 안 됩니다.
        //
        // 표시용 값(누적 횟수, 강화스킬 진행도)은 저장하지 않고 지금 인벤토리에서
        // 다시 냅니다. 저장된 뒤에 값이 바뀌었어도 화면은 현재를 말해야 합니다.
        // 하나라도 되살리지 못하면 null입니다 - 두 장짜리 3택을 띄우느니 안 띄웁니다.
        public List<LevelUpChoice> RebuildFromKeys(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count == 0)
                return null;

            var rebuilt = new List<LevelUpChoice>();

            for (var i = 0; i < keys.Count; i++)
            {
                var choice = RebuildOne(keys[i]);

                if (choice == null)
                {
                    Debug.LogWarning($"[Save] 3택을 되살리지 못했습니다: {keys[i]}");

                    return null;
                }

                rebuilt.Add(choice);
            }

            return rebuilt;
        }

        private LevelUpChoice RebuildOne(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var parts = key.Split('|');

            if (parts.Length < 4)
                return null;

            ELevelUpChoiceType type;

            if (!System.Enum.TryParse(parts[0], out type))
                return null;

            var skill = _skillTable?.GetRow(parts[1]);

            if (skill == null)
                return null;

            switch (type)
            {
                case ELevelUpChoiceType.AcquireSkill:
                {
                    return new LevelUpChoice
                    {
                        Type = type,
                        Skill = skill,
                        Weight = skill.Weight,
                        CurrentLevel = 0,
                    };
                }

                case ELevelUpChoiceType.Mastery:
                {
                    var mastery = _masteryTable?.FindBySkillId(skill.Id);

                    if (mastery == null || mastery.Id != parts[3])
                        return null;

                    var ownedForMastery = _inventory.Find(skill.Id);

                    return new LevelUpChoice
                    {
                        Type = type,
                        Skill = skill,
                        Mastery = mastery,
                        Weight = mastery.Weight,
                        CurrentLevel = ownedForMastery != null ? ownedForMastery.Level : 1,
                    };
                }

                default:
                {
                    var upgrade = FindUpgradeById(parts[2]);
                    var owned = _inventory.Find(skill.Id);

                    if (upgrade == null || owned == null)
                        return null;

                    var choice = new LevelUpChoice
                    {
                        Type = type,
                        Skill = skill,
                        Upgrade = upgrade,
                        Weight = upgrade.Weight,
                        CurrentLevel = owned.Level,
                        StackedUpgradeCount = owned.GetUpgradeCount(upgrade.UpgradeType),
                    };

                    AttachMasteryProgress(choice, owned, upgrade.UpgradeType);

                    return choice;
                }
            }
        }

        private SkillUpgradeDataTableRow FindUpgradeById(string id)
        {
            if (string.IsNullOrEmpty(id) || _upgradeTable?.Rows == null)
                return null;

            for (var i = 0; i < _upgradeTable.Rows.Count; i++)
            {
                if (_upgradeTable.Rows[i] != null && _upgradeTable.Rows[i].Id == id)
                    return _upgradeTable.Rows[i];
            }

            return null;
        }

        private void CollectCandidates()
        {
            _candidates.Clear();

            if (_skillTable?.Rows == null)
                return;

            foreach (var skill in _skillTable.Rows)
            {
                if (skill == null || skill.Weight <= 0)
                    continue;

                var owned = _inventory.Find(skill.Id);

                if (owned == null)
                {
                    // 슬롯이 차면 신규 획득은 빠집니다.
                    if (!_inventory.HasFreeSlot)
                        continue;

                    _candidates.Add(new LevelUpChoice
                    {
                        Type = ELevelUpChoiceType.AcquireSkill,
                        Skill = skill,
                        Weight = skill.Weight,
                        CurrentLevel = 0,
                    });

                    continue;
                }

                // 이미 가진 스킬은 다시 후보에 넣지 않습니다.
                //
                // 위력을 올리는 경로가 스킬 레벨업과 Damage 강화 둘이라 중복이었습니다.
                // 강화로 일원화하고, 보유 스킬에서는 강화와 강화스킬만 나옵니다.
                CollectUpgrades(owned);
                CollectMastery(owned);
            }
        }

        // 강화스킬은 조건을 채웠고 아직 하나도 안 골랐을 때만 후보에 들어갑니다.
        //
        // 별도 슬롯을 만들지 않고 기존 후보 풀에 섞습니다. 가중치가 높아서
        // 조건을 채운 뒤 한두 번의 레벨업이면 대개 나옵니다.
        private void CollectMastery(SkillState owned)
        {
            if (_masteryTable == null || _inventory.HasMastery)
                return;

            var mastery = _masteryTable.FindBySkillId(owned.Row.Id);

            if (mastery == null || mastery.Weight <= 0)
                return;

            if (!IsMasteryUnlocked(owned, mastery))
                return;

            _candidates.Add(new LevelUpChoice
            {
                Type = ELevelUpChoiceType.Mastery,
                Skill = owned.Row,
                Mastery = mastery,
                Weight = mastery.Weight,
                CurrentLevel = owned.Level,
            });
        }

        private static bool IsMasteryUnlocked(SkillState owned, SkillMasteryDataTableRow mastery)
        {
            return owned.GetUpgradeCount(mastery.RequireUpgradeTypeA) >= mastery.RequireCountA
                && owned.GetUpgradeCount(mastery.RequireUpgradeTypeB) >= mastery.RequireCountB;
        }

        // 이 강화 카드가 강화스킬 조건을 진행시키는지 보고, 진행도를 붙입니다.
        //
        // 조건 두 줄을 모두 담습니다. 위력 3/3을 채우고 "다 됐다"고 생각했는데
        // 발사체가 0/3이면, 알려주려던 표시가 오히려 속이는 셈이 됩니다.
        private void AttachMasteryProgress(LevelUpChoice choice, SkillState owned, ESkillUpgradeType advancing)
        {
            if (_masteryTable == null || _inventory.HasMastery)
                return;

            var mastery = _masteryTable.FindBySkillId(owned.Row.Id);

            if (mastery == null || mastery.Weight <= 0)
                return;

            // 이 카드가 두 조건 중 어느 쪽도 올리지 않으면 표시하지 않습니다.
            if (advancing != mastery.RequireUpgradeTypeA && advancing != mastery.RequireUpgradeTypeB)
                return;

            // 이미 조건을 다 채웠으면 진행도를 띄울 이유가 없습니다.
            if (IsMasteryUnlocked(owned, mastery))
                return;

            choice.MasteryHint = mastery;
            choice.MasteryProgress = new[]
            {
                new MasteryRequirement(
                    mastery.RequireUpgradeTypeA,
                    owned.GetUpgradeCount(mastery.RequireUpgradeTypeA),
                    mastery.RequireCountA,
                    advancing == mastery.RequireUpgradeTypeA),
                new MasteryRequirement(
                    mastery.RequireUpgradeTypeB,
                    owned.GetUpgradeCount(mastery.RequireUpgradeTypeB),
                    mastery.RequireCountB,
                    advancing == mastery.RequireUpgradeTypeB),
            };
        }

        private void CollectUpgrades(SkillState owned)
        {
            if (_upgradeTable?.Rows == null)
                return;

            foreach (var upgrade in _upgradeTable.Rows)
            {
                if (upgrade == null || upgrade.Weight <= 0)
                    continue;

                if (upgrade.SkillId != owned.Row.Id)
                    continue;

                // 강화는 그 스킬의 표시 레벨이 조건 이상일 때만 나옵니다.
                //
                // 표시 레벨이 "획득 1 + 강화 합"으로 바뀌었으므로, 이 값은 이제
                // "강화를 몇 번 넣은 뒤에 열리는가"를 뜻합니다. 1이면 획득 직후부터
                // 열리고, 3이면 다른 강화를 두 번 넣은 뒤에 열립니다.
                if (owned.Level < upgrade.RequireSkillLevel)
                    continue;

                var choice = new LevelUpChoice
                {
                    Type = ELevelUpChoiceType.UpgradeSkill,
                    Skill = owned.Row,
                    Upgrade = upgrade,
                    Weight = upgrade.Weight,
                    CurrentLevel = owned.Level,
                    StackedUpgradeCount = owned.GetUpgradeCount(upgrade.UpgradeType),
                };

                AttachMasteryProgress(choice, owned, upgrade.UpgradeType);

                _candidates.Add(choice);
            }
        }
    }
}
