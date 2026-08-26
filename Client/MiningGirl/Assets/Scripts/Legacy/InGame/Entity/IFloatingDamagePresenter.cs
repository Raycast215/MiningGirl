using UnityEngine;

namespace Legacy.MainGame.Entity
{
    // 데미지 수치를 화면에 띄우는 표현(플로팅 데미지)에 대한 추상화.
    // 몬스터는 이 인터페이스만 알고, 실제 구현(DamageController)에는 의존하지 않습니다.
    // (IMonsterStatProvider 등과 동일한 주입 패턴)
    public interface IFloatingDamagePresenter
    {
        void Show(int damage, Vector2 position, bool isCritical = false);
    }
}
