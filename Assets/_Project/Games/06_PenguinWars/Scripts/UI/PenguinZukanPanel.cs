using System.Collections.Generic;
using MiniGame.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// タイトルから開くキャラずかん（仕様書 §2.0）。マスはカタログから実行時に作るので、
    /// キャラを足しても Generate PenguinWars Units だけで並ぶ（Scene に50マス焼き込むと、キャラの追加のたびに Rebuild が要るため）
    /// </summary>
    public class PenguinZukanPanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("非表示のひな形。キャラの数だけ複製する")]
        [SerializeField] private ZukanCell _cellTemplate;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private ZukanDetailPanel _detailPanel;
        [SerializeField] private Button _closeButton;
        [Tooltip("隣のマスと歩くタイミングをずらすコマ数")]
        [SerializeField] private float _walkPhaseStep = 0.37f;

        private bool _isBuilt;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
        }

        public void Show()
        {
            BuildCellsOnce();
            _detailPanel.Hide();
            gameObject.SetActive(true);
            _scroll.verticalNormalizedPosition = 1f;
        }

        public void Hide() => gameObject.SetActive(false);

        private void BuildCellsOnce()
        {
            if (_isBuilt) return;
            _isBuilt = true;

            List<PenguinUnitData> units = SortedUnits();
            for (int i = 0; i < units.Count; i++)
            {
                ZukanCell cell = Instantiate(_cellTemplate, _cellTemplate.transform.parent);
                cell.Show(units[i], i * _walkPhaseStep, _detailPanel.Show);
            }
        }

        /// <summary>カタログの並びは生成順なので、No 順に並べ直して見つけやすくする</summary>
        private List<PenguinUnitData> SortedUnits()
        {
            var units = new List<PenguinUnitData>();
            foreach (PenguinUnitData unit in _catalog.Units)
            {
                if (unit != null) units.Add(unit);
            }
            units.Sort((a, b) => a.No.CompareTo(b.No));
            return units;
        }

        private void Close()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
            Hide();
        }
    }
}
