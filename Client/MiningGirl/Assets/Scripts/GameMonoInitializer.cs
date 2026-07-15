using UnityEngine;

/// MonoBehaviour를 상속한 Initializer.
public class GameMonoInitializer : MonoBehaviour
{
   public bool IsInitialized { get; protected set; }
}

/// MonoBehaviour를 상속하지 않은 Initializer.
public class GameInitializer
{
   public bool IsInitialized { get; protected set; }
}