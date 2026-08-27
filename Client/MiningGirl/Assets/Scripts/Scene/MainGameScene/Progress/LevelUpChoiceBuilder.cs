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

                _candidates.Add(new LevelUpChoice
                {
                    Type = ELevelUpChoiceType.LevelUpSkill,
                    Skill = owned.Row,
                    Weight = owned.Row.Weight,
                    CurrentLevel = owned.Level,
                });

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

                // 강화는 해당 스킬이 조건 레벨 이상일 때만 나옵니다.
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
