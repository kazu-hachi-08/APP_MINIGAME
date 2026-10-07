using System;
using MiniGame.Common.UI;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの作成画面（タイトルから開く）。3枠を切り替え、みため／のうりょくのタブで編集して「ほぞん」する。
    /// 編集中は枠のコピーを触り、「ほぞん」するまでセーブには書かない（途中で「もどる」しても壊れないように）
    /// </summary>
    public class CustomUnitPanel : MonoBehaviour
    {
        private const string ConfirmTitle = "ほぞんしますか？";
        private const string ConfirmMessage = "かえたところが まだ ほぞんされていません";
        private const string ConfirmSave = "ほぞん";
        private const string ConfirmDiscard = "すてる";

        [Tooltip("1〜3枠。並びが枠の番号")]
        [SerializeField] private Button[] _slotButtons;
        [SerializeField] private Button _lookTabButton;
        [SerializeField] private Button _statsTabButton;
        [SerializeField] private GameObject _lookTabRoot;
        [SerializeField] private GameObject _statsTabRoot;
        [SerializeField] private CustomLookTab _lookTab;
        [SerializeField] private CustomStatsTab _statsTab;
        [SerializeField] private CustomUnitPreview _preview;
        [SerializeField] private Text _statsLabel;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _backButton;
        [Tooltip("選んでいる枠・タブのボタンの色")]
        [SerializeField] private Color _selectedColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _normalColor = new Color(0.18f, 0.26f, 0.4f, 0.95f);

        private CustomUnitPresets _presets;
        private CustomUnitDefinition _editing;
        private int _slot;
        private bool _isDirty;
        // 絵の大きさは役割（大型かどうか）で変わるので、役割が変わったときだけプレビューを作り直す
        private UnitRole _previewRole;

        private void Awake()
        {
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                int slot = i;
                _slotButtons[i].onClick.AddListener(() => OnSlotClicked(slot));
            }
            _lookTabButton.onClick.AddListener(() => SelectTab(true));
            _statsTabButton.onClick.AddListener(() => SelectTab(false));
            _saveButton.onClick.AddListener(OnSaveClicked);
            _backButton.onClick.AddListener(() => ConfirmIfDirty(Close));
            _lookTab.LookChanged += OnLookChanged;
            _lookTab.NameChanged += MarkDirty;
            _statsTab.Changed += OnStatsChanged;
        }

        public void Show()
        {
            // 開くたびに読み直す（対戦で選んだ枠の記録など、ほかの画面が書いた分を消さないように）
            _presets = CustomUnitSave.Load();
            LoadSlot(0);
            SelectTab(true);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _preview.Release();
            gameObject.SetActive(false);
        }

        private void OnSlotClicked(int slot)
        {
            if (slot == _slot) return;

            PenguinUiSound.Click();
            ConfirmIfDirty(() => LoadSlot(slot));
        }

        private void LoadSlot(int slot)
        {
            _slot = slot;
            _editing = _presets.Slots[slot].Clone();
            _lookTab.Bind(_editing);
            _statsTab.Bind(_editing);
            RefreshPreview();
            SetDirty(false);
            for (int i = 0; i < _slotButtons.Length; i++) _slotButtons[i].image.color = i == slot ? _selectedColor : _normalColor;
        }

        private void SelectTab(bool isLook)
        {
            if (gameObject.activeInHierarchy) PenguinUiSound.Click();
            _lookTabRoot.SetActive(isLook);
            _statsTabRoot.SetActive(!isLook);
            _lookTabButton.image.color = isLook ? _selectedColor : _normalColor;
            _statsTabButton.image.color = isLook ? _normalColor : _selectedColor;
        }

        private void OnLookChanged()
        {
            RefreshPreview();
            MarkDirty();
        }

        /// <summary>強さが変わっても見た目は変わらないので、絵は役割が変わったときだけ作り直す（Texture2D を毎回作らないように）</summary>
        private void OnStatsChanged()
        {
            if (_editing.Role != _previewRole) RefreshPreview();
            else RefreshStatsLabel();
            MarkDirty();
        }

        private void RefreshPreview()
        {
            _preview.Show(_editing);
            _previewRole = _editing.Role;
            RefreshStatsLabel();
        }

        /// <summary>数値はきまりに合わせて直した定義から出す（保存したら実際にこの数値になる）</summary>
        private void RefreshStatsLabel()
        {
            CustomUnitDefinition sanitized = CustomUnitRules.Sanitize(_editing);
            UnitStats stats = UnitStatFormula.Calculate(CustomUnitRules.ToUnitDefinition(sanitized, CustomUnitRules.LeftNo));
            _statsLabel.text = ZukanDetailPanel.FormatStats(stats);
        }

        private void MarkDirty() => SetDirty(true);

        /// <summary>変えていないときは「ほぞん」を押せなくして、保存済みかどうかを見て分かるようにする</summary>
        private void SetDirty(bool isDirty)
        {
            _isDirty = isDirty;
            _saveButton.interactable = isDirty;
        }

        private void OnSaveClicked()
        {
            PenguinUiSound.Click();
            Save();
        }

        /// <summary>空の名前は「じぶんペンギン」になるなど、きまりで直した結果を画面にも戻す</summary>
        private void Save()
        {
            _presets.SetSlot(_slot, _editing);
            CustomUnitSave.Save(_presets);
            LoadSlot(_slot);
        }

        private void ConfirmIfDirty(Action next)
        {
            // 共通ダイアログが無い場面（テスト用の Scene など）では、確認できないので変更を捨てて進む
            if (!_isDirty || !UIManager.HasInstance)
            {
                next();
                return;
            }

            UIManager.Instance.ShowConfirmDialog(ConfirmTitle, ConfirmMessage,
                onConfirm: () => { Save(); next(); },
                onCancel: next,
                confirmText: ConfirmSave,
                cancelText: ConfirmDiscard);
        }

        private void Close()
        {
            PenguinUiSound.Click();
            Hide();
        }
    }
}
