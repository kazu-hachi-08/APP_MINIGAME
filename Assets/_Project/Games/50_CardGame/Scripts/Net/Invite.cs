#nullable enable
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CardGame.Unity.Net
{
    /// <summary>届いた招待(部屋コードと、ドラフトの部屋か)。</summary>
    public sealed class PendingInvite
    {
        public string Code { get; }
        public bool Draft { get; }
        public PendingInvite(string code, bool draft) { Code = code; Draft = draft; }
    }

    /// <summary>
    /// 招待リンクとクリップボード(05-online.md「招待リンク」)。
    /// リンクは <c>公開 URL?room=コード[&amp;mode=draft]</c>。ブラウザ版はリンクで開くと、その部屋に自動で参加する。
    /// ブラウザの API は WebGL の jslib(CardGameShare.jslib)、それ以外は Unity のクリップボードを使う。
    /// </summary>
    public static class Invite
    {
        /// <summary>ブラウザ版の公開先。ブラウザ以外(Android / Windows)で作ったリンクの行き先。</summary>
        public const string PublicUrl = "https://jiantailangdasen6-rgb.github.io/cardgame-web/";

        /// <summary>起動時のリンク(またはテスト用の -invite 引数)から受け取った、まだ使っていない招待。</summary>
        public static PendingInvite? Pending { get; private set; }

        public static void Clear() => Pending = null;

        /// <summary>起動時に 1 回呼ぶ。リンクに部屋コードがあれば Pending に入れ、URL から消す(再読み込みで入り直さないように)。</summary>
        public static void CaptureAtStartup()
        {
            string? source = null;
            if (Application.platform == RuntimePlatform.WebGLPlayer) source = Application.absoluteURL;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-invite");
            if (i >= 0 && i + 1 < args.Length) source = args[i + 1];
            if (string.IsNullOrEmpty(source)) return;
            SetFromText(source!);
            if (Pending != null && Application.platform == RuntimePlatform.WebGLPlayer) ClearQuery();
        }

        /// <summary>リンクや招待文から招待を取り出して Pending に入れる。取り出せたら true。</summary>
        public static bool SetFromText(string text)
        {
            var code = ParseCode(text);
            if (code == null) return false;
            Pending = new PendingInvite(code, Regex.IsMatch(text, @"[?&]mode=draft\b"));
            return true;
        }

        /// <summary>
        /// 文字列から部屋コードを取り出す。受け付けるのは 招待リンク(room=)、招待文(「部屋コード: 」の後)、コードだけ、のどれか。
        /// 見つからなければ null。大文字にそろえて返す。
        /// </summary>
        public static string? ParseCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"[?&]room=([A-Za-z0-9]{4,12})");
            if (!m.Success) m = Regex.Match(text, @"コード\s*[::]\s*([A-Za-z0-9]{4,12})");
            if (!m.Success) m = Regex.Match(text.Trim(), @"^([A-Za-z0-9]{4,12})$");
            return m.Success ? m.Groups[1].Value.ToUpperInvariant() : null;
        }

        /// <summary>招待リンク。ブラウザ版ならいま開いている URL、それ以外は公開 URL を元にする。</summary>
        public static string BuildLink(string code, bool draft)
        {
            string baseUrl = PublicUrl;
            if (Application.platform == RuntimePlatform.WebGLPlayer && !string.IsNullOrEmpty(Application.absoluteURL))
            {
                baseUrl = Application.absoluteURL;
                int cut = baseUrl.IndexOfAny(new[] { '?', '#' });
                if (cut >= 0) baseUrl = baseUrl.Substring(0, cut);
            }
            return $"{baseUrl}?room={code}" + (draft ? "&mode=draft" : "");
        }

        /// <summary>リンクに添える文(相手が手で入力するときにも読めるよう、コードを書いておく)。</summary>
        public static string BuildMessage(string code, bool draft)
            => $"THE CHAOS Ⅱ で対戦しよう!{(draft ? "(ドラフト)" : "")}\n部屋コード: {code}";

        /// <summary>
        /// 共有する(スマホのブラウザは共有画面、それ以外はクリップボードへコピー)。
        /// done に "shared" / "copied" / "cancelled" / "manual" を返す。コルーチンとして回す。
        /// </summary>
        public static IEnumerator Share(string text, string url, Action<string> done)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CardGame_Share(text, url);
            string? r;
            while ((r = Poll()) == null) yield return null;
            done(r);
#else
            GUIUtility.systemCopyBuffer = string.IsNullOrEmpty(url) ? text : text + "\n" + url;
            done("copied");
            yield break;
#endif
        }

        /// <summary>クリップボードの文字を読む(読めなければ空文字)。コルーチンとして回す。</summary>
        public static IEnumerator ReadClipboard(Action<string> done)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CardGame_ClipboardRead();
            string? r;
            while ((r = Poll()) == null) yield return null;
            done(r);
#else
            done(GUIUtility.systemCopyBuffer ?? "");
            yield break;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void CardGame_Share(string text, string url);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void CardGame_ClipboardRead();
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern string CardGame_AsyncPoll();
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void CardGame_ClearQuery();

        /// <summary>非同期の結果。未完了なら null。</summary>
        private static string? Poll()
        {
            var s = CardGame_AsyncPoll();
            return string.IsNullOrEmpty(s) ? null : s.Substring(1);
        }

        private static void ClearQuery() => CardGame_ClearQuery();
#else
        private static void ClearQuery() { }
#endif
    }
}
