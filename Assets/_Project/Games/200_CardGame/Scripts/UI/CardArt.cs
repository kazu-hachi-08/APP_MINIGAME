#nullable enable
using System.Collections.Generic;
using CardGame.Core.Definitions;
using UnityEngine;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// 画像素材の読み込みとキャッシュ。無ければ null(呼び出し側が代わりの表示にする)。
    /// - イラスト:      Resources/CardArt/&lt;カードID&gt;(ハースストーン風。旧い写実の絵は art-archive/)
    /// - カードの部品:  Resources/HsParts/…(台紙・額・リボン・文章欄・宝石)
    /// - リーダー:      Resources/Leaders/leader_&lt;class&gt;
    /// - クラス紋章:    Resources/Emblems/emblem_&lt;class&gt;
    /// - キーワード:    Resources/Keywords/kw_&lt;keyword&gt;
    /// </summary>
    public static class CardArt
    {
        private static readonly Dictionary<string, Sprite?> Cache = new();

        public static Sprite? Get(string cardId) => Load("CardArt/" + cardId);

        /// <summary>カードの部品(oval_thin, oval_ring, ribbon, textbox, spell_frame, gem_cost, gem_attack, gem_health)。</summary>
        public static Sprite? Hs(string part) => Load("HsParts/" + part);

        /// <summary>クラス別の台紙。</summary>
        public static Sprite? HsBody(CardClass cls) => Load("HsParts/body_" + cls.ToString().ToLowerInvariant()) ?? Load("HsParts/body_neutral");

        public static Sprite? Leader(CardClass cls) => Load("Leaders/leader_" + cls.ToString().ToLowerInvariant()) ?? Load("Leaders/leader_neutral");

        public static Sprite? Emblem(CardClass cls)
            => Load("Emblems/emblem_" + cls.ToString().ToLowerInvariant()) ?? Load("Emblems/emblem_neutral");

        public static Sprite? Keyword(Core.Definitions.Keyword kw) => Load("Keywords/kw_" + kw.ToString().ToLowerInvariant());

        private static Sprite? Load(string path)
        {
            if (Cache.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>(path);
            Cache[path] = s;
            return s;
        }
    }
}
