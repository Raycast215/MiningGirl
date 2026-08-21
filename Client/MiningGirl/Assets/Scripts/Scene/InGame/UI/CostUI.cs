using UI.Common;
using UnityEngine;

namespace MainGame.UI
{
    // 코스트 오브를 '표시만' 합니다.
    //
    // 보유 코스트와 회복 루프는 RunState.Cost(CostState)가 들고 있습니다.
    // (예전에는 이 클래스 안에서 UniTask 무한 루프로 회복이 돌았습니다.)
    public class CostUI : GameMonoInitializer
    {
        [SerializeField]
        private CostOrbView orbView;

        public void Init()
        {
            IsInitialized = true;
        }

        // cost: 보유 코스트, regenProgress: 다음 1까지의 진행도(0~1), max: 최대치
        public void SetValue(int cost, float regenProgress, int max, bool immediate = false)
        {
            if (orbView != null)
                orbView.SetValue(cost, regenProgress, max, immediate);
        }
    }
}
