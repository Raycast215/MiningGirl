using System.Collections.Generic;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 카드가 월드에 소환하는 효과 오브젝트의 공통 뼈대.
    //
    // 정지·정리를 효과 종류마다 따로 만들면 새 카드를 추가할 때마다
    // InGameController의 두 곳(SetGamePaused / PrepareNextStage)에 호출을 더해야 하고,
    // 한 곳만 빠뜨려도 '정지 중에 혼자 움직이는' 또는 '스테이지가 넘어가도 남는' 효과가 생깁니다.
    // 그래서 등록·정지·정리를 여기 한 곳으로 모았습니다.
    public abstract class SkillEffectObjectBase : MonoBehaviour
    {
        // 스테이지 정리 때 한꺼번에 없애기 위한 목록
        private static readonly List<SkillEffectObjectBase> Actives = new List<SkillEffectObjectBase>();

        // 게임이 멈춘 동안에는 효과도 멈춥니다.
        // 지속시간도 함께 멈춰야 정지한 시간만큼 손해 보지 않습니다.
        protected static bool IsPausedAll { get; private set; }

        public static void SetPausedAll(bool paused)
        {
            IsPausedAll = paused;
        }

        // 스테이지 재시작·전환 시 남아있는 효과를 모두 정리합니다.
        public static void ClearAll()
        {
            IsPausedAll = false;

            for (var i = Actives.Count - 1; i >= 0; i--)
            {
                if (Actives[i] != null)
                    Destroy(Actives[i].gameObject);
            }

            Actives.Clear();
        }

        protected virtual void OnEnable()
        {
            if (!Actives.Contains(this))
                Actives.Add(this);
        }

        protected virtual void OnDisable()
        {
            Actives.Remove(this);
        }
    }
}
