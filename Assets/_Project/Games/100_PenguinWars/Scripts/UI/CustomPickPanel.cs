using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ドラフトの後に、じぶんペンギンの3枠から1体を選ぶ画面。カードの作りはドラフトと同じ（DraftCard）。
    /// 時間切れは前回選んだ枠を自分で選ぶ（ホスト・ゲストとも自分の端末で決めて、ゲストはホストへ送る）。
    /// 残り秒は表示だけで、ホストが試合へ進むかどうかは GameManager が猶予を足して判断する
    /// </summary>
    public class CustomPickPanel : MonoBehaviour
    {
        private const string ChooseMessage = "つれていく じぶんペンギンを えらんでください";
        private const string WaitingMessage = "相手を待っています...";
        private const string StatsFormat = "体力 {0}　攻撃 {1}";

        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private DraftCard[] _cards;
        [Tooltip("カードの絵を歩かせる（DraftCard の絵と同じ Image を動かす）")]
        [SerializeField] private UnitSpriteAnimator[] _walkers;
        [SerializeField] private Text[] _statsLabels;

        // カードの絵のために作ったデータ。カタログには登録しない（対戦用の登録は試合の初期化で両者分まとめて行う）
        private readonly List<PenguinUnitData> _units = new List<PenguinUnitData>();
        private CustomUnitPresets _presets;
        private float _remaining;
        private bool _hasPicked;

        /// <summary>選んだ（または時間切れで決まった）じぶんペンギン。1回の表示で1回だけ</summary>
        public event Action<CustomUnitDefinition> Picked;

        private void Awake()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                int slot = i;
                _cards[i].Bind(() => Pick(slot));
            }
        }

        /// <summary>作成画面で直した内容が反映されるよう、開くたびにセーブから読み直す</summary>
        public void Show(float pickTime)
        {
            Release();
            _presets = CustomUnitSave.Load();
            _remaining = pickTime;
            _hasPicked = false;
            _statusLabel.text = ChooseMessage;
            for (int i = 0; i < _cards.Length; i++) ShowCard(i);
            RefreshTime();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Release();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            RefreshTime();
            if (_remaining <= 0f) Pick(_presets.LastPickedSlot);
        }

        private void ShowCard(int slot)
        {
            // 自分のキャラとして見せるので、出撃ボタンと同じ左陣営（青）の No で作る
            PenguinUnitData data = CustomUnitFactory.Create(_presets.Slots[slot], CustomUnitRules.LeftNo);
            _units.Add(data);
            _cards[slot].Show(data.No, data);
            // 3体が揃って足踏みすると機械的に見えるので、枠ごとにコマをずらす
            _walkers[slot].Play(data.GetSprites(Side.Left), UnitSpriteAnimator.WalkFrames, slot);
            UnitStats stats = data.ToStats();
            _statsLabels[slot].text = string.Format(StatsFormat, stats.MaxHp, stats.Attack);
        }

        private void Pick(int slot)
        {
            if (_hasPicked) return;

            _hasPicked = true;
            for (int i = 0; i < _cards.Length; i++) _cards[i].SetState(false, i == slot);
            _statusLabel.text = WaitingMessage;
            // 次の対戦で時間切れになったときも、同じ枠が選ばれるように覚えておく
            _presets.LastPickedSlot = slot;
            CustomUnitSave.Save(_presets);
            Picked?.Invoke(_presets.Slots[slot].Clone());
        }

        /// <summary>カードの絵は Texture2D を作っているので、閉じたら捨てる</summary>
        private void Release()
        {
            // シーンを閉じるときは子が先に消えていることがあるので確かめる
            foreach (UnitSpriteAnimator walker in _walkers)
            {
                if (walker != null) walker.Clear();
            }
            foreach (PenguinUnitData data in _units)
            {
                if (data != null) data.DestroyRuntime();
            }
            _units.Clear();
        }

        private void OnDestroy() => Release();

        /// <summary>切り上げにして、0.5秒残っているのに 0 と出ないようにする</summary>
        private void RefreshTime()
        {
            _timeLabel.text = $"のこり {Mathf.CeilToInt(_remaining)}";
        }
    }
}
