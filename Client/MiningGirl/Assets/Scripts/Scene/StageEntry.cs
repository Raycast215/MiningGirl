namespace Scene
{
    // 스테이지 선택에서 고른 스테이지를 인게임까지 나르는 자리.
    //
    // 씬을 넘길 때 값을 들고 갈 곳이 아직 없어 정적 필드로 둡니다. 세이브가
    // 들어오면 진행 상태와 함께 다뤄야 하므로 그때 옮길 자리입니다.
    //
    // 비어 있으면 인게임이 인스펙터 값(Stage_01)으로 갑니다 - MainGameScene만
    // 단독으로 재생하는 경로가 그대로 살아 있어야 합니다.
    public static class StageEntry
    {
        public static string StageId { get; private set; }

        public static void Select(string stageId)
        {
            StageId = stageId;
        }

        // 한 번 들어가면 지웁니다. 남겨 두면 다음에 스테이지 선택을 건너뛰고
        // 들어왔을 때 지난 선택이 되살아납니다.
        public static string Consume()
        {
            var id = StageId;

            StageId = null;

            return id;
        }
    }
}
