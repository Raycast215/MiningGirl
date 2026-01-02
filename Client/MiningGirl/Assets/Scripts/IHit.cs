using UnityEngine;

public interface IHit
{
    public void Damage(float damage);
    public Vector3 GetPosition();
    public bool GetActiveState();
    public Transform GetTransform();
}