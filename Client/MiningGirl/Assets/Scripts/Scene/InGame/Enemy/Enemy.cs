using System;
using BehaviourTree;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Node;
using UnityEngine;

namespace Scene.InGame.Enemy
{
    public class Enemy : GameMonoInitializer, IEntityObject
    {
        private event Action<IEntityObject> OnReleased;
        
        [SerializeField]
        private Rigidbody rigidBody;
        [SerializeField]
        protected SpriteRenderer spriteRenderer;
        
        protected NodeRunner _nodeRunner;
        protected MoveNode _moveNode;
        private string _id;
        
        public void Init()
        {
            if (IsInitialized)
                return;
            
            //_moveNode = new MoveNode(rigidBody, this);
            
            IsInitialized = true;
        }
        
 #region IEntityObject

        public string GetId()
        {
            return _id;
        }

        public void SetId(string id)
        {
            _id = id;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetActiveObject(bool isActive)
        {
           gameObject.SetActive(isActive);
        }

        public void SetReleaseCallback(Action<IEntityObject> callback)
        {
            OnReleased = null;
            OnReleased += callback;
        }

#endregion
    }
}