using System.Collections.Generic;
using UnityEngine;

public class StageData
{
    public int Index { get; set; }
    public float Time { get; set; }
    public List<string> TargetIdList { get; set; }
    public List<int> TargetCountList { get; set; }
}

namespace InGame.System.Stage
{
    public class StageController : GameInitializer
    {
        
    }
}
