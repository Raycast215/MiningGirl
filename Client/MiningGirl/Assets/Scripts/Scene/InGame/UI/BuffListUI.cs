using System.Collections.Generic;
using MainGame.Bonus;
using UnityEngine;

namespace MainGame.UI
{
    // 지금 걸려 있는 카드 버프를 스테이지 표시 아래에 세로로 나열합니다.
    // 남은 시간이 긴 순서로 정렬되고, 만료되면 자동으로 사라집니다.
    public class BuffListUI : GameMonoInitializer
    {
        [SerializeField]
        [Tooltip("버프 항목 프리팹")]
        private BuffIconView iconPrefab;

        [SerializeField]
        [Tooltip("항목이 생성될 부모 (VerticalLayoutGroup)")]
        private Transform iconRoot;

        [Header("Colors")]
        [SerializeField]
        private Color moveSpeedColor = new Color(0.45f, 0.78f, 1f);
        [SerializeField]
        private Color miningSpeedColor = new Color(1f, 0.72f, 0.30f);
        [SerializeField]
        private Color goldGainColor = new Color(1f, 0.85f, 0.35f);
        [SerializeField]
        private Color expGainColor = new Color(0.55f, 0.90f, 0.45f);

        private readonly List<BuffIconView> _icons = new List<BuffIconView>();
        private readonly List<KeyValuePair<TemporaryBuffState.EBuffType, float>> _buffer =
            new List<KeyValuePair<TemporaryBuffState.EBuffType, float>>();

        private TemporaryBuffState _buffs;

        public void Init(TemporaryBuffState buffs)
        {
            _buffs = buffs;

            HideAll();

            IsInitialized = true;
        }

        private void Update()
        {
            if (!IsInitialized || _buffs == null)
                return;

            _buffs.CollectActive(_buffer);

            // 부족한 만큼만 새로 만들고 나머지는 재사용합니다.
            while (_icons.Count < _buffer.Count)
            {
                if (iconPrefab == null || iconRoot == null)
                    return;

                _icons.Add(Instantiate(iconPrefab, iconRoot));
            }

            for (var i = 0; i < _icons.Count; i++)
            {
                if (i >= _buffer.Count)
                {
                    _icons[i].SetVisible(false);
                    continue;
                }

                var entry = _buffer[i];

                _icons[i].SetVisible(true);
                _icons[i].SetData(null, GetColor(entry.Key), entry.Value);
            }
        }

        private Color GetColor(TemporaryBuffState.EBuffType type)
        {
            switch (type)
            {
                case TemporaryBuffState.EBuffType.MoveSpeed: return moveSpeedColor;
                case TemporaryBuffState.EBuffType.MiningSpeed: return miningSpeedColor;
                case TemporaryBuffState.EBuffType.GoldGain: return goldGainColor;
                case TemporaryBuffState.EBuffType.ExpGain: return expGainColor;
            }

            return Color.white;
        }

        private void HideAll()
        {
            foreach (var icon in _icons)
                icon.SetVisible(false);
        }
    }
}
