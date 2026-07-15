using DG.Tweening;
using UnityEngine;

namespace Scene.InGame.UI.Cursor
{
    public class CursorUI : GameMonoInitializer
    {
        [SerializeField] 
        private Transform cursor;

        public void Set(Transform parent)
        {
            transform.SetParent(parent);
        }

        public void SetDirection(Vector3 dir)
        {
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            // transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.DORotateQuaternion(Quaternion.Euler(0, 0, angle), 0.1f);
        }
    }
}
