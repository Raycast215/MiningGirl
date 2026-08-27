using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 조준 계산. 스킬 발사와 연쇄가 같은 식을 씁니다.
    //
    // 두 군데서 각자 구현하면 한쪽만 고쳤을 때 연쇄만 빗나가기 시작하고,
    // 그건 눈으로 잘 안 잡힙니다.
    public static class SkillAiming
    {
        // 대상이 도착할 지점을 돌려줍니다.
        //
        // 현재 위치를 조준하면 비행 시간 동안 대상이 내려간 만큼 빗나갑니다. 조준선이
        // 비스듬할수록, 대상이 빠를수록 크게 벌어집니다. 몬스터가 등속 직선 하강이라
        // 근사가 아니라 해가 정확히 나옵니다.
        //
        //   대상은 t초 뒤 P + (0, -v·t)에 있고, 발사체는 그때까지 s·t만큼 날아갑니다.
        //   |P + (0, -v·t) - M| = s·t 를 t에 대해 풀면 2차방정식이 됩니다.
        //
        //   (s² - v²)·t² + 2·Dy·v·t - |D|² = 0        (D = P - M)
        //
        // s > v 이면 상수항이 음수라 양의 해가 하나만 나옵니다.
        public static Vector3 PredictAimPoint(Vector3 origin, MonsterUnit target, float projectileSpeed)
        {
            if (target == null)
                return origin + Vector3.up;

            var position = target.Position;
            var moveSpeed = target.MoveSpeed;

            if (moveSpeed <= 0f)
                return position;

            var a = projectileSpeed * projectileSpeed - moveSpeed * moveSpeed;

            // 발사체가 대상보다 느리면 따라잡지 못합니다. 그때는 현재 위치를 조준합니다.
            if (a <= 0.0001f)
                return position;

            var delta = position - origin;
            var b = 2f * delta.y * moveSpeed;
            var c = -delta.sqrMagnitude;

            var discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
                return position;

            var time = (-b + Mathf.Sqrt(discriminant)) / (2f * a);

            if (time <= 0f)
                return position;

            return position + new Vector3(0f, -moveSpeed * time, 0f);
        }
    }
}
