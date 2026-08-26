using UnityEngine;
using UnityEngine.UI;

namespace Scene.MainGameScene.UI
{
    // 결과 화면의 별 하나.
    //
    // 채움과 빈 칸을 색이 아니라 스프라이트로 가릅니다.
    // 알파를 낮춘 실루엣은 "못 받았다"보다 "꺼져 있다"로 읽혀서, 실패 화면에서 셋 다
    // 빈 별이 뜨면 다시 할 마음이 들지 않습니다. 빈 별은 파낸 홈처럼 그려져 있습니다.
    //
    // 부모가 값을 밀어넣는 순수 표시 컴포넌트라 ViewModel을 따로 두지 않았습니다.
    public class StarIconView : MonoBehaviour
    {
        [SerializeField]
        private Image icon;

        [SerializeField]
        private Sprite filledSprite;

        [SerializeField]
        [Tooltip("못 받은 별. 숨기지 않고 남겨 몇 개를 노릴 수 있었는지 보여 줍니다.")]
        private Sprite emptySprite;

        public void SetFilled(bool filled)
        {
            if (icon == null)
                return;

            var sprite = filled ? filledSprite : emptySprite;

            if (sprite != null)
                icon.sprite = sprite;

            // 아트가 색까지 그려 두었으므로 곱하지 않습니다.
            icon.color = Color.white;
        }
    }
}
