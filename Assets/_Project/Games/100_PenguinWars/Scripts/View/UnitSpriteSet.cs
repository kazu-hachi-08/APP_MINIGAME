using System;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>アニメのコマ（仕様書 §7.2「歩き2・攻撃2・ノックバック1」）。並び順が UnitSpriteSet の配列の並びと PNG の番号になる</summary>
    public enum PenguinFrame
    {
        Walk0,
        Walk1,
        AttackWindup,
        AttackStrike,
        Knockback,
    }

    /// <summary>
    /// 1キャラ1陣営分のスプライト。中身は Tools > MiniGame > Rebuild PenguinWars が PenguinUnitData に書き込む
    /// </summary>
    [Serializable]
    public class UnitSpriteSet
    {
        public static readonly int FrameCount = Enum.GetValues(typeof(PenguinFrame)).Length;

        [Tooltip("PenguinFrame の順")]
        [SerializeField] private Sprite[] _frames = new Sprite[0];
        [Tooltip("足元から頭のてっぺんまでの高さ（ワールド単位）。HPバーを頭の上に置くため、生成時に絵から測っておく")]
        [SerializeField] private float _headHeight;

        public UnitSpriteSet()
        {
        }

        /// <summary>実行時に作った絵を入れる（じぶんペンギン。RuntimeUnitSprites）</summary>
        public UnitSpriteSet(Sprite[] frames, float headHeight)
        {
            _frames = frames;
            _headHeight = headHeight;
        }

        public float HeadHeight => _headHeight;
        /// <summary>ボタン・編成発表のアイコン。立ち姿の1コマ目を使う</summary>
        public Sprite Icon => Get(PenguinFrame.Walk0);

        public bool IsValid
        {
            get
            {
                if (_frames == null || _frames.Length != FrameCount) return false;
                foreach (Sprite frame in _frames)
                {
                    if (frame == null) return false;
                }
                return true;
            }
        }

        public Sprite Get(PenguinFrame frame)
        {
            int index = (int)frame;
            return _frames != null && index < _frames.Length ? _frames[index] : null;
        }
    }
}
