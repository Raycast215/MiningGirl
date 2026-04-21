using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Data;
using Scene.InGame.UI.Cursor;
using UnityEngine;

namespace Scene.InGame.Entity.Player
{
    public class PlayerController : EntityControllerBase<Player>
    {
        [SerializeField]
        private CursorUI cursor;
        
        public async UniTaskVoid InitAsync(IInGameHandler handler)
        {
            InitAsync("Player", 1).Forget();
            await UniTask.WaitUntil(() => IsInitialized);

            var ins = Get();
            
            ins.BaseData = new EntityData
            {
                MoveSpeed = 1,
                MoveToMinDistance = 1,
                AttackDelay = 60,
            };
            
            ins.SetHandler(handler);
            ins.InitDirectionEvent(cursor.SetDirection);
            ins.InitAsync().Forget();
            ins.SetPosition(Vector3.zero);
            ins.gameObject.SetActive(true);
            
            cursor.Set(ins.transform);
        }
    }
}