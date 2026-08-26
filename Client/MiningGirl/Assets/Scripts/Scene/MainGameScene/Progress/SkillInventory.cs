using System.Collections.Generic;
using Data;

namespace Scene.MainGameScene.Progress
{
    // 이 런에서 들고 있는 스킬들. 스테이지를 나가면 통째로 사라집니다.
    public class SkillInventory
    {
        public IReadOnlyList<SkillState> Skills => _skills;

        public int SlotMax { get; }

        public bool HasFreeSlot => _skills.Count < SlotMax;

        private readonly List<SkillState> _skills = new List<SkillState>();

        public SkillInventory(int slotMax)
        {
            SlotMax = slotMax < 1 ? 1 : slotMax;
        }

        public SkillState Add(SkillDataTableRow row)
        {
            if (row == null)
                return null;

            var existing = Find(row.Id);

            if (existing != null)
                return existing;

            var state = new SkillState(row);
            _skills.Add(state);

            return state;
        }

        public SkillState Find(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;

            for (var i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].Row.Id == skillId)
                    return _skills[i];
            }

            return null;
        }

        public bool Has(string skillId)
        {
            return Find(skillId) != null;
        }
    }
}
