#nullable enable
using System;
using System.Collections.Generic;

namespace Data
{
    [Serializable]
    [DataFile("CharacterDataTable")]
    public class CharacterDataTableRow : DataTableRowBase
    {
        public string Name { get; set; }
        public string? Desc { get; set; }

        // 화면 하단 타워의 최대 체력. 이 값이 0이 되면 스테이지 실패입니다.
        // 캐릭터 자신은 피격 대상이 아니라 공격 스탯이 없습니다.
        public int TowerMaxHealth { get; set; }

        // 이 캐릭터가 들고 시작하는 고유 스킬. SkillDataTable의 Id입니다.
        public string? StartSkillId { get; set; }

        // 홈에서 해금하는 비용. 0이면 기본 보유입니다.
        public int UnlockPrice { get; set; }

        public string? AssetId { get; set; }
    }

    public class CharacterDataTable : DataTableBase<CharacterDataTableRow>
    {
        public CharacterDataTable(IReadOnlyList<CharacterDataTableRow> rows) : base(rows)
        {
        }
    }
}
