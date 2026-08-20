using System.Collections.Generic;
using Data;
using Scene.InGame.Entity.Interface;

namespace MainGame.Card
{
    // 드래그 중 '지금 놓으면 누가 맞는지'를 미리 보여줄 수 있는 스킬.
    //
    // 카드를 놓은 자리를 기준으로 대상을 고르는 공격 스킬만 구현합니다.
    // CardHandController가 드래그 중 이 목록을 받아 대상 머리 위에 표시를 띄웁니다.
    //
    // 주의: 여기서 돌려주는 대상은 Execute가 실제로 때릴 대상과 같아야 합니다.
    // (미리보기와 결과가 어긋나면 조준이라는 행위 자체가 의미를 잃습니다.)
    public interface ITargetPreviewEffect
    {
        // 지금 카드 위치 기준으로 적중할 대상들. 없으면 빈 목록.
                IReadOnlyList<IEntity> CollectTargets(SkillCardContext context, SkillCardDataTableRow row);

        // 조준 표시용 사거리(월드 유닛). 시트에 값이 없을 때의 기본값까지 반영된 최종 값입니다.
        float GetPreviewRange(SkillCardDataTableRow row);
    }
}
