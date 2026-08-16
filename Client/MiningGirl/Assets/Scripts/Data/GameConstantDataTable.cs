#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Data
{
    // 게임 전역 상수 한 줄. 코드에 흩어져 있던 매직 넘버를 여기로 모읍니다.
    //
    // 값은 float 하나로 통일하고, 정수로 쓸 항목은 조회할 때 반올림합니다.
    // (시간·비율처럼 소수가 필요한 값과 개수처럼 정수인 값이 섞여 있어서)
    [Serializable]
    [DataFile("GameConstantDataTable")]
    public class GameConstantDataTableRow : DataTableRowBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EGameConstantType ConstantType { get; set; }

        public float Value { get; set; }

        // 시트에서 알아보기 위한 설명(코드에서는 쓰지 않습니다)
        public string? Desc { get; set; }
    }

    public class GameConstantDataTable : DataTableBase<GameConstantDataTableRow>
    {
        // 타입으로 빠르게 찾기 위한 캐시
        private readonly Dictionary<EGameConstantType, float> _values = new Dictionary<EGameConstantType, float>();

        public GameConstantDataTable(IReadOnlyList<GameConstantDataTableRow> rows) : base(rows)
        {
            if (rows == null)
                return;

            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                _values[row.ConstantType] = row.Value;
            }
        }

        // 값이 없으면 defaultValue를 돌려주고 경고를 남깁니다.
        // (시트에 행을 빼먹었을 때 조용히 0이 되는 걸 막기 위함)
        public float GetValue(EGameConstantType type, float defaultValue = 0f)
        {
            if (_values.TryGetValue(type, out var value))
                return value;

            Debug.LogWarning($"[GameConstant] {type} 값이 시트에 없습니다. 기본값 {defaultValue}을 씁니다.");

            return defaultValue;
        }

        public int GetInt(EGameConstantType type, int defaultValue = 0)
        {
            return Mathf.RoundToInt(GetValue(type, defaultValue));
        }

        public bool Has(EGameConstantType type)
        {
            return _values.ContainsKey(type);
        }
    }
}
