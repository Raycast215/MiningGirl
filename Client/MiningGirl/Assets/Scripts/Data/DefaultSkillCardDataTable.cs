#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data
{
    // 스테이지 1 시작 시 들고 시작하는 기본 덱 구성.
    // 한 행이 '어떤 스킬 카드를 몇 장 가지고 시작하는가'를 뜻합니다.
    [Serializable]
    [DataFile("DefaultSkillCardDataTable")]
    public class DefaultSkillCardDataTableRow : DataTableRowBase
    {
        // SkillCardDataTable의 Id를 가리킵니다.
        public string SkillId { get; set; }

        // 시작 시 보유 수량
        public int Count { get; set; }
    }

    public class DefaultSkillCardDataTable : DataTableBase<DefaultSkillCardDataTableRow>
    {
        public DefaultSkillCardDataTable(IReadOnlyList<DefaultSkillCardDataTableRow> rows) : base(rows)
        {
        }

        // 시작 덱을 카드 한 장씩 펼친 목록으로 돌려줍니다.
        // (Count 3이면 같은 SkillId가 3번 들어갑니다.)
        public List<string> BuildStartingDeck()
        {
            var deck = new List<string>();

            if (Rows == null)
                return deck;

            foreach (var row in Rows)
            {
                if (row == null || string.IsNullOrEmpty(row.SkillId))
                    continue;

                for (var i = 0; i < row.Count; i++)
                    deck.Add(row.SkillId);
            }

            return deck;
        }

        // 시작 덱의 총 장수
        public int GetTotalCount()
        {
            var total = 0;

            if (Rows == null)
                return total;

            foreach (var row in Rows)
            {
                if (row == null)
                    continue;

                total += Math.Max(0, row.Count);
            }

            return total;
        }
    }
}
