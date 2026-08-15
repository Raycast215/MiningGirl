using System.Collections.Generic;
using UnityEngine;

namespace MainGame.Bonus
{
    // 카드로 걸리는 '일시 버프'를 관리합니다.
    //
    // 레벨업 보너스(LevelUpBonusState)는 런 내내 유지되는 영구 성장이고,
    // 이쪽은 몇 초만 유지되는 임시 효과라 계층을 나눴습니다.
    // 최종 스탯 = (기본값 + 영구 Add) x 영구 Mul x 임시 배율
    public class TemporaryBuffState
    {
        // 버프 종류. 카드가 늘어나면 여기에 추가합니다.
        public enum EBuffType
        {
            MoveSpeed,
            MiningSpeed,
            GoldGain,
            ExpGain,
        }

        private class Entry
        {
            public float Multiplier;
            public float RemainTime;
        }

        private readonly Dictionary<EBuffType, Entry> _entries = new Dictionary<EBuffType, Entry>();

        // percent: 10이면 10% 증가 / duration: 지속시간(초)
        public void Apply(EBuffType type, float percent, float duration)
        {
            if (duration <= 0f)
                return;

            var multiplier = 1f + percent * 0.01f;

            // 같은 버프를 다시 걸면 배율은 더 강한 쪽, 시간은 새로 갱신합니다.
            if (_entries.TryGetValue(type, out var entry))
            {
                entry.Multiplier = Mathf.Max(entry.Multiplier, multiplier);
                entry.RemainTime = Mathf.Max(entry.RemainTime, duration);
                return;
            }

            _entries[type] = new Entry { Multiplier = multiplier, RemainTime = duration };
        }

        public float GetMultiplier(EBuffType type)
        {
            return _entries.TryGetValue(type, out var entry) ? entry.Multiplier : 1f;
        }

        // 남은 지속시간(초). 없으면 0.
        public float GetRemainTime(EBuffType type)
        {
            return _entries.TryGetValue(type, out var entry) ? Mathf.Max(0f, entry.RemainTime) : 0f;
        }

        // 지금 걸려 있는 버프들을 남은 시간이 긴 순서로 돌려줍니다(표시용).
        public void CollectActive(List<KeyValuePair<EBuffType, float>> buffer)
        {
            buffer.Clear();

            foreach (var kv in _entries)
                buffer.Add(new KeyValuePair<EBuffType, float>(kv.Key, kv.Value.RemainTime));

            buffer.Sort((a, b) => b.Value.CompareTo(a.Value));
        }

        public bool IsActive(EBuffType type)
        {
            return _entries.ContainsKey(type);
        }

        // 남은 시간이 0이 되면 자동으로 풀립니다. 매 프레임 호출합니다.
        public void Update(float deltaTime)
        {
            if (_entries.Count == 0)
                return;

            List<EBuffType> expired = null;

            foreach (var kv in _entries)
            {
                kv.Value.RemainTime -= deltaTime;

                if (kv.Value.RemainTime > 0f)
                    continue;

                expired ??= new List<EBuffType>();
                expired.Add(kv.Key);
            }

            if (expired == null)
                return;

            foreach (var type in expired)
                _entries.Remove(type);
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
