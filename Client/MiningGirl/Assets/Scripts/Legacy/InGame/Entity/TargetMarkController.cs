using System.Collections.Generic;
using Pool;
using Legacy.Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Legacy.Scene.InGame.Entity
{
    // 조준 대상 표시를 한 곳에서 빌려주고 돌려받습니다.
    //
    // 표시는 엔티티의 자식이 아니라 이 오브젝트 밑에 모여 있고,
    // 대상이 움직이면 매 프레임 따라갑니다.
    public class TargetMarkController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("머리 위에 띄울 표시 프리팹")]
        private TargetMarkView markPrefab;

        [SerializeField]
        [Tooltip("미리 만들어 둘 개수. 한 번에 표시되는 대상 수보다 넉넉하면 됩니다")]
        private int capacity = 8;

        private Pooling<TargetMarkView> _pool;

        // 지금 표시 중인 대상 → 빌려준 표시
        private readonly Dictionary<IEntity, TargetMarkView> _marks = new Dictionary<IEntity, TargetMarkView>();

        // 순회 도중에 지우면 컬렉션이 바뀌므로, 지울 대상을 모았다가 처리합니다.
        private readonly List<IEntity> _removeBuffer = new List<IEntity>();

        private void Awake()
        {
            if (markPrefab == null)
            {
                Debug.LogWarning("[TargetMark] 표시 프리팹이 지정되지 않았습니다.");

                return;
            }

            _pool = new Pooling<TargetMarkView>(markPrefab, Mathf.Max(1, capacity), transform);
            _pool.Pool();
        }

        // 이 대상에 표시를 붙입니다. 이미 붙어 있으면 아무것도 하지 않습니다.
        public void Show(IEntity entity)
        {
            if (_pool == null || entity == null || _marks.ContainsKey(entity))
                return;

            var mark = _pool.Get();

            _marks.Add(entity, mark);

            Place(entity, mark);
        }

        // 표시를 떼어 풀로 돌려보냅니다.
        public void Hide(IEntity entity)
        {
            if (_pool == null || entity == null)
                return;

            if (!_marks.TryGetValue(entity, out var mark))
                return;

            _marks.Remove(entity);

            _pool.Return(mark);
        }

        // 드래그가 끝나거나 판이 리셋될 때 전부 회수합니다.
        public void Clear()
        {
            if (_pool == null)
                return;

            foreach (var pair in _marks)
                _pool.Return(pair.Value);

            _marks.Clear();
        }

        // 대상이 움직이므로 매 프레임 따라갑니다.
        // 표시가 하나도 없으면 곧바로 빠져나가서 평소 비용은 없습니다.
        private void LateUpdate()
        {
            if (_marks.Count == 0)
                return;

            _removeBuffer.Clear();

            foreach (var pair in _marks)
            {
                // 조준 중에 죽거나 풀로 돌아간 대상은 표시도 함께 회수합니다.
                // (예전에는 엔티티가 스스로 껐지만, 이제 표시가 엔티티 밖에 있습니다.)
                if (!pair.Key.GetActiveState())
                {
                    _removeBuffer.Add(pair.Key);

                    continue;
                }

                Place(pair.Key, pair.Value);
            }

            for (var i = 0; i < _removeBuffer.Count; i++)
                Hide(_removeBuffer[i]);
        }

        // 띄우는 높이는 엔티티마다 다릅니다(광물은 낮고 몬스터는 조금 높습니다).
        private static void Place(IEntity entity, TargetMarkView mark)
        {
            var entityBase = entity as EntityBase;
            var height = entityBase != null ? entityBase.TargetMarkHeight : 1f;

            mark.Follow(entity.GetPosition(), height);
        }
    }
}
