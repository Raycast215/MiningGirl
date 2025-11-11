using UnityEngine;

public interface IHit
{
    public void Damage();
    public Vector3 GetPosition();
    public Vector2 GetAnchoredPosition();
    public bool GetActiveState();
}