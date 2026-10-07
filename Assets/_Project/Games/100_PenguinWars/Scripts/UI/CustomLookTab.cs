using System;
using MiniGame.Common.Profile;
using MiniGame.PenguinWars.Art;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの作成画面の「みため」タブ（名前・5部位の◀▶・おまかせ）。
    /// 受け取った定義を直接書き換え、変わったことだけを知らせる（保存するかは CustomUnitPanel が決める）
    /// </summary>
    public class CustomLookTab : MonoBehaviour
    {
        private const string WebPromptMessage = "じぶんペンギンの名前（8文字まで）";

        [SerializeField] private InputField _nameInput;
        [Tooltip("ブラウザ版だけ入力欄の上に被せる。InputField だとブラウザ版で日本語に変換できないため")]
        [SerializeField] private Button _webNameCover;
        [SerializeField] private CustomPartRow[] _rows;
        [SerializeField] private Button _randomButton;

        private CustomUnitDefinition _def;

        /// <summary>見た目が変わった（プレビューを作り直す）</summary>
        public event Action LookChanged;
        /// <summary>名前だけ変わった（絵は変わらないので作り直さない）</summary>
        public event Action NameChanged;

        private void Awake()
        {
            _nameInput.characterLimit = CustomUnitRules.NameMaxLength;
            _nameInput.onValueChanged.AddListener(OnNameEdited);
            _webNameCover.onClick.AddListener(PromptWebName);
            _webNameCover.gameObject.SetActive(WebNamePrompt.IsAvailable);
            _randomButton.onClick.AddListener(Randomize);
            foreach (CustomPartRow row in _rows) row.Changed += () => OnPartChanged(row);
        }

        /// <summary>枠を切り替えたとき・保存して作り直したときに呼ぶ。ここでは Changed を出さない</summary>
        public void Bind(CustomUnitDefinition def)
        {
            _def = def;
            _nameInput.SetTextWithoutNotify(def.Name);
            foreach (CustomPartRow row in _rows) row.SetValue(GetPart(row.Slot));
        }

        private void OnNameEdited(string text)
        {
            if (_def == null) return;

            _def.Name = text;
            NameChanged?.Invoke();
        }

        private void PromptWebName()
        {
            string result = WebNamePrompt.Prompt(WebPromptMessage, _nameInput.text);
            // characterLimit があるので、長すぎる分は InputField が切り詰める
            if (result != null) _nameInput.text = result;
        }

        private void OnPartChanged(CustomPartRow row)
        {
            SetPart(row.Slot, row.Value);
            LookChanged?.Invoke();
        }

        /// <summary>名前・強さは変えない（見た目だけのおまかせ）</summary>
        private void Randomize()
        {
            PenguinUiSound.Click();
            foreach (CustomPartRow row in _rows)
            {
                row.Randomize();
                SetPart(row.Slot, row.Value);
            }
            LookChanged?.Invoke();
        }

        private string GetPart(PenguinPartSlot slot)
        {
            switch (slot)
            {
                case PenguinPartSlot.Body: return _def.Body;
                case PenguinPartSlot.BodyColor: return _def.BodyColor;
                case PenguinPartSlot.Head: return _def.Head;
                case PenguinPartSlot.Hand: return _def.Hand;
                default: return _def.Back;
            }
        }

        private void SetPart(PenguinPartSlot slot, string id)
        {
            switch (slot)
            {
                case PenguinPartSlot.Body: _def.Body = id; break;
                case PenguinPartSlot.BodyColor: _def.BodyColor = id; break;
                case PenguinPartSlot.Head: _def.Head = id; break;
                case PenguinPartSlot.Hand: _def.Hand = id; break;
                default: _def.Back = id; break;
            }
        }
    }
}
