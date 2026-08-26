namespace Scene.MainGameScene.Battle
{
    // 발사체 한 발을 쏘는 데 필요한 값 묶음.
    //
    // 인자가 여덟 개를 넘어가면서 호출부에서 순서를 헷갈리기 쉬워져 묶었습니다.
    // 스킬 레벨과 강화가 반영된 뒤의 최종 수치입니다.
    public readonly struct ProjectileSpec
    {
        public readonly string EffectAssetId;
        public readonly float Speed;
        public readonly float Damage;
        public readonly int PierceCount;
        public readonly float HitRange;

        public readonly EProjectileMoveType MoveType;
        public readonly float WaveAmplitude;
        public readonly float WaveCycles;

        public ProjectileSpec(
            string effectAssetId,
            float speed,
            float damage,
            int pierceCount,
            float hitRange,
            EProjectileMoveType moveType,
            float waveAmplitude,
            float waveCycles)
        {
            EffectAssetId = effectAssetId;
            Speed = speed;
            Damage = damage;
            PierceCount = pierceCount;
            HitRange = hitRange;
            MoveType = moveType;
            WaveAmplitude = waveAmplitude;
            WaveCycles = waveCycles;
        }
    }
}
