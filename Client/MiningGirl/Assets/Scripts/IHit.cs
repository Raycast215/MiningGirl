using UnityEngine;

public interface IHit
{
    public void Damage();
    public Vector3 GetPosition();
    public bool GetActiveState();
    public Transform GetTransform();
}