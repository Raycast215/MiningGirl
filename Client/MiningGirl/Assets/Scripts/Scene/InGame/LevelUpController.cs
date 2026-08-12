using System;
using Data;
using MainGame.Bonus;
using MainGame.Entity;
using Scene.InGame.Entity.Resource;
using UnityEngine;

namespace Scene.InGame
{
    // 레벨업 보너스와 보상(경험치/골드) 흐름을 전담하는 컨트롤러.
    //
    // 역할:
    //  - 획득한 보너스 누적 상태(LevelUpBonusState) 보관
    //  - 몬스터 처치 / 광물 채굴 보상을 받아 보너스를 얹어 지급
    //  - 선택한 보너스의 즉시 효과(골드/경험치) 실행
    //
    // 표시(UI)는 직접 하지 않고 MainGameUIController에 위임합니다.
    public class LevelUpController : MonoBehaviour, IExpRewardHandler, IResourceRewardHandler
    {
        // 이번 런에서 획득한 보너스 누적 상태
        private readonly LevelUpBonusState _bonusState = new LevelUpBonusState();
        public LevelUpBonusState BonusState => _bonusState;

        // 선택한 캐릭터 기본 스탯 + 보너스를 합쳐 최종 스탯을 계산합니다.
        private CharacterStatContext _statContext;
        public CharacterStatContext StatContext => _statContext ??= new CharacterStatContext(_bonusState);

        // 이미 캐릭터를 골랐는지 (재시작 시 다시 묻지 않기 위한 판단)
        public bool HasCharacter => StatContext.HasStat;

        // 캐릭터 선택 시 호출합니다. 재시작 때는 호출하지 않아 선택과 강화가 유지됩니다.
        public void SetCharacter(CharacterStatDataRow row)
        {
            StatContext.SetCharacter(row);
            Debug.Log($"[CharacterSelect] 적용 — Id={row?.Id}");
        }

        private Action<int> _onGoldGranted;
        private Action<int> _onExpGranted;

        // onGoldGranted / onExpGranted: 실제 지급을 수행할 대상(UI 컨트롤러)을 연결합니다.
        public void Init(Action<int> onGoldGranted, Action<int> onExpGranted)
        {
            _onGoldGranted = onGoldGranted;
            _onExpGranted = onExpGranted;
        }

        // 런을 처음부터 다시 시작할 때 보너스를 모두 비웁니다.
        // (스테이지 재시작 Next에서는 호출하지 않습니다 — 보너스는 런 전체에서 유지됩니다.)
        public void ResetBonus()
        {
            _bonusState.Reset();
        }

        // 선택한 보너스를 적용합니다.
        // 즉시 효과는 바로 지급하고, 나머지는 누적 상태에 기록되어 스탯 계산에 반영됩니다.
        public void ApplyBonus(LevelUpBonusSkillDataTableRow row)
        {
            if (row == null)
                return;

            // 어떤 효과인지는 테이블의 EffectType으로 판단합니다.
            switch (row.EffectType)
            {
                case ELevelUpBonusEffectType.InstantGold:
                    _onGoldGranted?.Invoke(Mathf.RoundToInt(row.EffectValue));
                    break;

                case ELevelUpBonusEffectType.InstantExp:
                    _onExpGranted?.Invoke(Mathf.RoundToInt(row.EffectValue));
                    break;
            }

            _bonusState.Acquire(row);

            Debug.Log($"[LevelUpBonus] 적용 — Id={row.Id} ({row.Name}) 레벨={_bonusState.GetLevel(row.Id)}");
        }

#region 보상 수신

        // IExpRewardHandler 구현 — 몬스터 처치 시 호출됩니다.
        public void OnExpGained(int amount)
        {
            // 적 처치 보너스 골드가 있으면 함께 지급합니다.
            if (_bonusState.MonsterKillGoldAdd > 0)
                _onGoldGranted?.Invoke(_bonusState.MonsterKillGoldAdd);

            _onExpGranted?.Invoke(amount);
        }

        // IResourceRewardHandler 구현 — 광물을 다 캐면 호출됩니다.
        public void OnResourceMined(int stoneReward, int expReward)
        {
            _onGoldGranted?.Invoke(stoneReward + _bonusState.ResourceMineGoldAdd);
        }

#endregion
    }
}
