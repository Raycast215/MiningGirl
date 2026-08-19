using System.Collections.Generic;
using Data;
using Manager;
using UnityEngine;

namespace MainGame.Card
{
    // 런 동안 유지되는 덱과, 스테이지마다 도는 드로우 더미 / 버린 더미.
    //
    // 가중치 랜덤이 아니라 '가진 카드에서만 뽑는' 구조라
    // 보유 수량(같은 카드 3장)이 실제 등장 빈도로 이어집니다.
    // 드로우 더미가 비면 버린 더미를 섞어서 되돌립니다.
    public class SkillDeck
    {
        // 런 전체의 덱 구성(스킬 Id 목록). 스테이지가 바뀌어도 유지됩니다.
        private readonly List<string> _cards = new List<string>();

        // 이번 스테이지에서 아직 뽑지 않은 카드
        private readonly List<string> _drawPile = new List<string>();

        // 사용했거나 버린 카드
        private readonly List<string> _discardPile = new List<string>();

        public int DeckCount => _cards.Count;
        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;

        // 기본 덱 데이터로 런을 시작합니다.
        public void InitFromDefaultTable()
        {
            var table = DataTableManager.Instance?.DefaultSkillCardDataTable;

            _cards.Clear();

            if (table != null)
                _cards.AddRange(table.BuildStartingDeck());
            else
                Debug.LogWarning("[Deck] 기본 덱 데이터를 찾지 못했습니다.");

            // 시작 덱 장수가 게임 상수와 어긋나면 알려줍니다.
            // (시트 수정 중 카드를 빠뜨리거나 중복으로 넣는 실수를 잡기 위함)
            var constants = DataTableManager.Instance?.GameConstantDataTable;

            if (constants != null)
            {
                var expected = constants.GetInt(EGameConstantType.CardDeckSize, _cards.Count);

                if (expected != _cards.Count)
                    Debug.LogWarning($"[Deck] 시작 덱이 {_cards.Count}장인데 설정값은 {expected}장입니다.");
            }

            ResetPiles();
        }

        // 스테이지 시작 시 호출 — 모든 카드를 드로우 더미로 되돌리고 섞습니다.
        public void ResetPiles()
        {
            _drawPile.Clear();
            _discardPile.Clear();

            _drawPile.AddRange(_cards);

            Shuffle(_drawPile);
        }

        // 카드 한 장을 뽑습니다. 더 뽑을 수 없으면 null.
        public SkillCardDataTableRow Draw()
        {
            if (_drawPile.Count == 0)
                RefillFromDiscard();

            if (_drawPile.Count == 0)
                return null;

            var last = _drawPile.Count - 1;
            var id = _drawPile[last];

            _drawPile.RemoveAt(last);

            return DataTableManager.Instance?.SkillCardDataTable?.GetRow(id);
        }

        // 사용했거나 버린 카드를 버린 더미로 보냅니다.
        public void Discard(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            _discardPile.Add(skillId);
        }

        // 현재 덱 구성(스킬 Id 목록). 카드 정리 화면에서 씁니다.
        public IReadOnlyList<string> GetDeckCards() => _cards;

        // 덱 전체를 새 구성으로 교체합니다. 정리 화면에서 확정할 때 씁니다.
        public void SetDeckCards(IReadOnlyList<string> cards)
        {
            _cards.Clear();

            if (cards != null)
                _cards.AddRange(cards);

            ResetPiles();
        }

        // 보상용 카드를 가중치로 뽑습니다. 같은 카드가 또 나올 수 있습니다.
        // (카드 종류가 적어 중복을 막으면 금방 뽑을 것이 없어집니다.
        //  중복은 '그 카드가 자주 나오는 덱'이라는 전략이 되기도 합니다.)
        public static List<SkillCardDataTableRow> PickRandomRewards(int count)
        {
            var result = new List<SkillCardDataTableRow>();
            var table = DataTableManager.Instance?.SkillCardDataTable;

            if (table?.Rows == null || count <= 0)
                return result;

            // 가중치가 0 이하인 카드는 보상에서 제외합니다.
            var pool = new List<SkillCardDataTableRow>();
            var totalWeight = 0;

            foreach (var row in table.Rows)
            {
                if (row == null || row.Weight <= 0)
                    continue;

                pool.Add(row);
                totalWeight += row.Weight;
            }

            if (pool.Count == 0 || totalWeight <= 0)
                return result;

            for (var i = 0; i < count; i++)
            {
                var pick = Random.Range(0, totalWeight);

                foreach (var row in pool)
                {
                    pick -= row.Weight;

                    if (pick >= 0)
                        continue;

                    result.Add(row);

                    break;
                }
            }

            return result;
        }

        // 런 도중 카드를 얻었을 때(스테이지 보상 등)
        public void AddCard(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            _cards.Add(skillId);
            _discardPile.Add(skillId);
        }

        // 덱에서 카드를 제거합니다(상점의 카드 제거 등).
        public bool RemoveCard(string skillId)
        {
            if (!_cards.Remove(skillId))
                return false;

            // 더미에 남아있다면 그쪽에서도 빼줍니다.
            if (!_drawPile.Remove(skillId))
                _discardPile.Remove(skillId);

            return true;
        }

        // 드로우 더미가 비면 버린 더미를 섞어서 되돌립니다.
        private void RefillFromDiscard()
        {
            if (_discardPile.Count == 0)
                return;

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();

            Shuffle(_drawPile);
        }

        private static void Shuffle(List<string> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);

                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
