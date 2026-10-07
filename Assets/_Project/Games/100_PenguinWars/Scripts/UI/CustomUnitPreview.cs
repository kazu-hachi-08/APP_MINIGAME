using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの作成画面の大きいプレビュー。定義が変わるたびに CustomUnitFactory で絵を作り直し、前の絵は捨てる
    /// （◀▶ のたびに Texture2D を作るので、捨てないとメモリが増え続ける）。カタログには登録しない（登録が要るのは対戦だけ）
    /// </summary>
    public class CustomUnitPreview : MonoBehaviour
    {
        [SerializeField] private UnitSpriteAnimator _icon;
        [Tooltip("拡大率1のときの絵の大きさ。UI の Image は PPU を見ないので、大型は拡大率を掛けて大きく見せる")]
        [SerializeField] private float _baseSize = 260f;

        private PenguinUnitData _current;

        /// <summary>作ったデータ（数値の表示に使う）。次に Show するか、この画面が消えると捨てられる</summary>
        public PenguinUnitData Show(CustomUnitDefinition def)
        {
            Release();
            CustomUnitDefinition sanitized = CustomUnitRules.Sanitize(def);
            // 自分のキャラとして見せるので、出撃ボタンと同じ左陣営（青）の No で作る
            _current = CustomUnitFactory.Create(sanitized, CustomUnitRules.LeftNo);

            float size = _baseSize * _current.Look.Scale;
            ((RectTransform)_icon.transform).sizeDelta = new Vector2(size, size);
            _icon.Play(_current.GetSprites(Side.Left), UnitSpriteAnimator.WalkAndAttackFrames);
            return _current;
        }

        /// <summary>閉じたら絵を残しておく理由がないので捨てる</summary>
        public void Release()
        {
            if (_current == null) return;

            _icon.Clear();
            _current.DestroyRuntime();
            _current = null;
        }

        private void OnDestroy() => Release();
    }
}
