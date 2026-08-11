using Cysharp.Threading.Tasks;
using MainGame.Entity.Player.Cursor;
using Scene.InGame.Entity.Data;
using Scene.InGame.Entity.Resource;
using UnityEngine;

namespace Scene.InGame.Entity.Player
{
    public class PlayerController : EntityControllerBase<Player>
    {
        [SerializeField]
        private CursorUI cursor;
        
        public async UniTaskVoid InitAsync(IResourceProvider resourceProvider = null)
        {
            if (IsInitialized)
                return;
            
            InitAsync("Player", 1).Forget();
            await UniTask.WaitUntil(() => IsInitialized);

            var ins = await Get();
            
            ins.BaseData = new EntityData
            {
                MoveSpeed = 1,
                MoveToMinDistance = 1,
                AttackDelay = 60,
            };
            
            // 행동 트리(InitAsync 내부)가 구성되기 전에 광물 공급자를 먼저 주입해야
            // SearchTargetNode가 공급자를 제대로 참조합니다.
            ins.SetResourceProvider(resourceProvider);
            ins.InitDirectionEvent(cursor.SetDirection);
            ins.InitAsync().Forget();
            ins.SetPosition(Vector3.zero);
            ins.gameObject.SetActive(true);
            
            cursor.Set(ins.transform);
        }

        public void SetPosition(Vector3 position)
        {
            ActivateList[0].transform.position = position;
        }

        // 플레이어의 행동 트리(광물 탐색 → 이동)를 매 프레임 구동합니다.
        // (MonsterController와 동일하게, 이게 없으면 NodeRunner가 구성만 되고 실행되지 않습니다.)
        private void Update()
        {
            UpdateEntity();
        }
    }
}