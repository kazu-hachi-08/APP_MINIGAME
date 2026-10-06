// ブラウザ版の招待リンク・クリップボード・入力ダイアログ(05-online.md「招待リンクとコピー / 貼り付け」)。
// C# 側は Invite.cs / OnlineLobbyScreen.cs。
// 共有やクリップボードは Promise で返るが、C# から JS へのコールバックは面倒なので、
// 結果を変数に置いて C# が毎フレーム CardGame_AsyncPoll で取りに来る形にしている。
mergeInto(LibraryManager.library, {
  $CardGameAsync: { result: null },

  // 未完了なら空文字、完了していれば "!" + 結果(結果が空文字でも完了と区別できるように先頭に印を付ける)
  CardGame_AsyncPoll__deps: ['$CardGameAsync'],
  CardGame_AsyncPoll: function () {
    var r = CardGameAsync.result;
    if (r === null) return stringToNewUTF8('');
    CardGameAsync.result = null;
    return stringToNewUTF8('!' + r);
  },

  // スマホは共有画面(LINE などを選べる)、無ければクリップボード、それも無理ならダイアログで手コピー
  CardGame_Share__deps: ['$CardGameAsync'],
  CardGame_Share: function (textPtr, urlPtr) {
    var text = UTF8ToString(textPtr);
    var url = UTF8ToString(urlPtr);
    var all = url ? text + '\n' + url : text;
    CardGameAsync.result = null;
    var done = function (r) { CardGameAsync.result = r; };

    var manual = function () {
      window.prompt('このリンクをコピーして送ってください', all);
      done('manual');
    };
    var copy = function () {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(all).then(function () { done('copied'); }, manual);
      } else {
        manual();
      }
    };

    // PC のブラウザにも navigator.share はあるが、コピーの方が使いやすいのでスマホだけ共有画面にする
    var mobile = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    if (mobile && navigator.share) {
      navigator.share({ text: text, url: url }).then(
        function () { done('shared'); },
        function (e) { if (e && e.name === 'AbortError') done('cancelled'); else copy(); });
    } else {
      copy();
    }
  },

  // 読めなければ入力ダイアログを出す(長押しで貼り付けられる)
  CardGame_ClipboardRead__deps: ['$CardGameAsync'],
  CardGame_ClipboardRead: function () {
    CardGameAsync.result = null;
    var ask = function () {
      CardGameAsync.result = window.prompt('届いた招待(またはコード)を貼り付けてください', '') || '';
    };
    if (navigator.clipboard && navigator.clipboard.readText) {
      navigator.clipboard.readText().then(function (t) { CardGameAsync.result = t || ''; }, ask);
    } else {
      ask();
    }
  },

  // Unity の InputField はスマホ Safari でキーボードが開かないので、ブラウザ標準の入力を使う
  CardGame_Prompt: function (messagePtr, defaultPtr) {
    var r = window.prompt(UTF8ToString(messagePtr), UTF8ToString(defaultPtr));
    return stringToNewUTF8(r || '');
  },

  // 再読み込みで同じ部屋に入り直さないよう、URL から ?room= を消す
  CardGame_ClearQuery: function () {
    if (window.history && window.history.replaceState) {
      window.history.replaceState(null, '', window.location.pathname + window.location.hash);
    }
  }
});
