using System;
using System.Collections.Generic;
using Data;

namespace Scene.StartScene.ViewModel
{
    // 스테이지 목록 한 줄에 그릴 내용. 전부 완성된 문자열입니다.
    public readonly struct StageSelectItem
    {
        public readonly string StageId;
        public readonly string Order;
        public readonly string Name;
        public readonly string Detail;

        public StageSelectItem(string stageId, string order, string name, string detail)
        {
            StageId = stageId;
            Order = order;
            Name = name;
            Detail = detail;
        }
    }

    // 스테이지 선택의 표시용 상태와 커맨드.
    //
    // 어떤 스테이지가 있는지는 StageDataTable이 정하고, 그걸 뭐라고 적을지는
    // 여기서 정합니다. 고른 뒤에 무엇을 할지는 컨트롤러 몫입니다.
    public class StageSelectViewModel
    {
        public event Action<string> Selected;

        public IReadOnlyList<StageSelectItem> Items => _items;

        private readonly List<StageSelectItem> _items = new List<StageSelectItem>();

        // 마리 수는 웨이브 테이블에서 셉니다. StageDataTable에 열로 두면 구성을
        // 고칠 때마다 두 곳을 맞춰야 하고, 어긋나도 아무도 안 알려줍니다.
        public StageSelectViewModel(StageDataTable table, WaveDataTable waves)
        {
            if (table?.Rows == null || waves == null)
                return;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];

                if (row == null)
                    continue;

                _items.Add(new StageSelectItem(
                    row.Id,
                    (i + 1).ToString(),
                    row.Name,
                    $"{row.WaveCount}웨이브 / 몬스터 {waves.SumMonsterCount(row.Id)}"));
            }
        }

        // View의 버튼이 부르는 커맨드입니다.
        public void Select(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            Selected?.Invoke(_items[index].StageId);
        }
    }
}
