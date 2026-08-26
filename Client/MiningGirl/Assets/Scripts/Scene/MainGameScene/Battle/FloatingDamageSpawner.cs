using System.Collections.Generic;
using Pool;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 피해 숫자를 풀에서 꺼내 띄웁니다.
    //
    // 레거시(Legacy/InGame/FloatingDamage/DamageController.cs)에서 가져오면서 자체 Queue
    // 대신 이 프로젝트의 Pooling을 씁니다. 몬스터·발사체가 이미 같은 것을 쓰고 있어,
    // 여기만 다른 방식을 두면 풀이 두 종류가 됩니다.
    public class FloatingDamageSpawner
    {
        // 한 프레임에 여러 발이 맞을 수 있어 처음부터 어느 정도 만들어 둡니다.
        private const int PrewarmCount = 12;

        private readonly Pooling<FloatingDamageText> _pool;
        private readonly List<FloatingDamageText> _showing = new List<FloatingDamageText>();

        public FloatingDamageSpawner(FloatingDamageText prefab, Transform layer)
        {
            _pool = new Pooling<FloatingDamageText>(prefab, PrewarmCount, layer);
            _pool.Pool();
        }

        public void Show(float damage, Vector3 position)
        {
            // 0으로 뜨는 숫자는 맞았는지 안 맞았는지 오히려 헷갈립니다.
            var amount = Mathf.Max(1, Mathf.RoundToInt(damage));
            var text = _pool.Get();

            _showing.Add(text);

            text.Show(amount, position, HandleFinished);
        }

        // 판이 끝나거나 다시 시작할 때 떠 있던 숫자를 걷습니다.
        public void Clear()
        {
            // ForceFinish가 HandleFinished를 부르며 목록을 건드리므로 복사본으로 돕니다.
            var buffer = _showing.ToArray();

            foreach (var text in buffer)
            {
                if (text != null)
                    text.ForceFinish();
            }

            _showing.Clear();
        }

        private void HandleFinished(FloatingDamageText text)
        {
            _showing.Remove(text);
            _pool.Return(text);
        }
    }
}
