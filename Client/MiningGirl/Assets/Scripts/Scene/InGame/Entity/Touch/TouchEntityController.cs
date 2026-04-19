using Scene.InGame.Entity.Interface;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scene.InGame.Entity.Touch
{
    public class TouchEntityController : MonoBehaviour
    {
        [SerializeField] 
        private float rayDistance = 100f;
        [SerializeField] 
        private LayerMask targetLayer;

        private void Update()
        {
            // PC + 모바일 공통 입력 처리
            if (IsTouchDown())
            {
                TryTouch();
            }
        }

        private bool IsTouchDown()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Input.GetMouseButtonDown(0);
#else
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
        }

        private Vector2 GetTouchPosition()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Input.mousePosition;
#else
            return Input.GetTouch(0).position;
#endif
        }

        private void TryTouch()
        {
            // UI 위 터치 방지
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 touchPos = GetTouchPosition();

            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, targetLayer))
            {
                HandleHit(hit);
            }
        }

        private void HandleHit(RaycastHit hit)
        {
            var hitObj = hit.collider.gameObject;
            var entity = hitObj.GetComponent<IEntity>();
            
            if (entity != null)
            {
                OnTouchEntity(entity);
                return;
            }

            // 기타 오브젝트 처리
            OnTouchObject(hitObj);
        }

        private void OnTouchEntity(IEntity entity)
        {
            Debug.Log("엔티티 터치");

            entity.Damage(1);
        }

        private void OnTouchObject(GameObject obj)
        {
            Debug.Log("일반 오브젝트 터치");
        }
    }
}
