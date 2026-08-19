using System.Collections.Generic;
using UnityEngine;

namespace MainGame.Entity.Monster
{
    // 임시 구현체 — 추후 엑셀 시트 기반 데이터로 교체 예정입니다.
    // 지금은 코드 상의 딕셔너리로 몬스터별 기본 스탯을 대신합니다.
    public class TempMonsterStatProvider : IMonsterStatProvider
    {
        private readonly Dictionary<string, MonsterBaseStat> _table = new()
        {
            ["Slime"] = new MonsterBaseStat
            {
                MonsterId = "Slime",
                Hp = 3f,
                Damage = 1f,
                MoveSpeed = 0.75f, // 기존 1.5f에서 절반으로 낮춤
                AttackDelay = 1f,
                AttackDistance = 1.2f, // 캐릭터와 너무 겹쳐 보이지 않도록 여유 확보
                GoldReward = 1,
            },
        };

        public MonsterBaseStat GetBaseStat(string monsterId)
        {
            if (_table.TryGetValue(monsterId, out var stat))
                return stat;

            Debug.LogWarning($"[TempMonsterStatProvider] '{monsterId}' 몬스터 데이터가 없어 기본값을 반환합니다.");

            return new MonsterBaseStat
            {
                MonsterId = monsterId,
                Hp = 3f,
                Damage = 1f,
                MoveSpeed = 1f,
                AttackDelay = 1f,
                AttackDistance = 1.2f,
                GoldReward = 1,
            };
        }
    }
}
