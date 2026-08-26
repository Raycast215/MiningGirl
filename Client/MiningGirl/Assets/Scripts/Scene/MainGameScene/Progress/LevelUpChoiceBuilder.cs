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
        private readonly SkillInventory _inventory;

        private readonly List<LevelUpChoice> _candidates = new List<LevelUpChoice>();

        public LevelUpChoiceBuilder(SkillDataTable skillTable, SkillUpgradeDataTable upgradeTable, SkillInventory inventory)
        {
            _skillTable = skillTable;
            _upgradeTable = upgradeTable;
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
            }
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

                _candidates.Add(new LevelUpChoice
                {
                    Type = ELevelUpChoiceType.UpgradeSkill,
                    Skill = owned.Row,
                    Upgrade = upgrade,
                    Weight = upgrade.Weight,
                    CurrentLevel = owned.Level,
                    StackedUpgradeCount = owned.GetUpgradeCount(upgrade.UpgradeType),
                });
            }
        }
    }
}
