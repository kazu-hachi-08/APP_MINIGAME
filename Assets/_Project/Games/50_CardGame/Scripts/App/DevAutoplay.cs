#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Core.State;
using CardGame.Unity.Battle;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// 開発用: プレイヤーをコマンドライン引数付きで起動すると自動で画面を進め、スクリーンショットを保存して終了する。
    /// Claude が Unity エディタを開かずに画面と操作経路を確認するための仕組み。
    ///   -autoplay -shots DIR        : メニュー → AI 同士の対戦を観戦
    ///   -autoplay-human -shots DIR  : AI 対戦モードで、人間側の操作(ドラッグ・対象選択・ターン終了)を EventSystem 経由で模擬する
    /// </summary>
    public sealed class DevAutoplay : MonoBehaviour
    {
        private string? _dir;
        private int _shotNo;

        public static void TryStart(AppRoot root)
        {
            var args = Environment.GetCommandLineArgs();
            bool watch = args.Contains("-autoplay");
            bool human = args.Contains("-autoplay-human");
            bool onlineHost = args.Contains("-online-host");
            bool onlineGuest = args.Contains("-online-guest");
            bool previewList = args.Contains("-preview-list");
            bool previewFx = args.Contains("-preview-fx");
            bool previewSettings = args.Contains("-preview-settings");
            bool previewDraft = args.Contains("-preview-draft");
            int pc = Array.IndexOf(args, "-preview-cards");
            string? previewCards = pc >= 0 && pc + 1 < args.Length ? args[pc + 1] : null;
            if (!watch && !human && !onlineHost && !onlineGuest && !previewList && !previewFx && !previewSettings && !previewDraft && previewCards == null) return;
            int i = Array.IndexOf(args, "-shots");
            var comp = root.gameObject.AddComponent<DevAutoplay>();
            comp._dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "shots");
            Directory.CreateDirectory(comp._dir);
            int c = Array.IndexOf(args, "-codefile");
            comp._codeFile = c >= 0 && c + 1 < args.Length ? args[c + 1] : Path.Combine(Path.GetTempPath(), "cardgame-joincode.txt");
            if (previewCards != null) comp.StartCoroutine(comp.RunPreview(previewCards));
            else if (previewList) comp.StartCoroutine(comp.RunPreviewList());
            else if (previewFx) comp.StartCoroutine(comp.RunPreviewFx());
            else if (previewSettings) comp.StartCoroutine(comp.RunPreviewSettings());
            else if (previewDraft) comp.StartCoroutine(comp.RunPreviewDraft());
            else if (onlineHost) comp.StartCoroutine(comp.RunOnline(true));
            else if (onlineGuest) comp.StartCoroutine(comp.RunOnline(false));
            else comp.StartCoroutine(human ? comp.RunHuman() : comp.RunWatch());
        }

        // ------------------------------------------------------------------
        // 観戦モード
        // ------------------------------------------------------------------

        private IEnumerator RunWatch()
        {
            yield return new WaitForSeconds(1.0f);
            yield return Shot("menu");

            var app = AppRoot.Instance;
            app.ShowDeckSelect(BattleMode.VersusAi);
            yield return null;
            FindAnyObjectByType<DeckSelectScreen>()?.Select(1, app.Decks.Count - 1);
            yield return null;
            yield return Shot("deckselect");
            app.ShowDeckSelect(BattleMode.Online);
            yield return null;
            yield return Shot("deckselect-online");
            app.StartBattle(new BattleConfig { Mode = BattleMode.AiVersusAi, Deck0 = app.Decks[0], Deck1 = app.Decks[^1], Seed = 7 });
            for (int n = 0; n < 12; n++)
            {
                yield return new WaitForSeconds(2.5f);
                yield return Shot("watch");
            }
            Finish();
        }

        // ------------------------------------------------------------------
        // 人間操作の模擬
        // ------------------------------------------------------------------

        private string? _codeFile;

        /// <summary>-preview-cards N001,K010 : 各カードを拡大/手札/場のサイズで表示して撮影(イラスト確認用)。</summary>
        private IEnumerator RunPreview(string ids)
        {
            yield return new WaitForSeconds(0.5f);
            var app = AppRoot.Instance;
            app.StartBattle(new BattleConfig { Mode = BattleMode.AiVersusAi, Deck0 = app.Decks[0], Deck1 = app.Decks[^1], Seed = 1 });
            yield return null;
            var screen = FindAnyObjectByType<BattleScreen>();
            if (screen == null) { Finish(); yield break; }
            foreach (var id in ids.Split(','))
            {
                if (!app.Db.TryGet(id.Trim(), out var def)) continue;
                screen.ShowCardPreview(def);
                yield return null;
                yield return Shot("card-" + def.Id);
            }
            Finish();
        }

        /// <summary>-preview-list : カード一覧画面を開き、全部 → クラス絞り込み → ID 表示 → スクロール → 拡大 の順に撮影。</summary>
        private IEnumerator RunPreviewList()
        {
            yield return new WaitForSeconds(0.5f);
            AppRoot.Instance.ShowCardList();
            yield return null; yield return null;
            var list = FindAnyObjectByType<CardListScreen>();
            if (list == null) { Finish(); yield break; }
            yield return Shot("list-all");
            list.SetClass(CardClass.Knight);
            yield return null;
            yield return Shot("list-knight");
            list.SetDebug(true);
            yield return null;
            yield return Shot("list-knight-debug");
            list.SetDebug(false);
            list.SetClass(null);
            list.Scroll(0.5f);
            yield return null;
            yield return Shot("list-scrolled");
            // 実際のクリックで拡大が開くことを確認する(セルの Button 経由)
            var cell = list.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name.StartsWith("Cell "));
            if (cell != null) SimulateClick(cell.gameObject);
            yield return null;
            yield return Shot("list-detail");
            Finish();
        }

        /// <summary>-preview-fx : 戦闘エフェクトを順に出して撮影する(演出確認用)。</summary>
        private IEnumerator RunPreviewFx()
        {
            yield return new WaitForSeconds(0.5f);
            var app = AppRoot.Instance;
            app.StartBattle(new BattleConfig { Mode = BattleMode.AiVersusAi, Deck0 = app.Decks[0], Deck1 = app.Decks[^1], Seed = 3 });
            yield return null;
            var screen = FindAnyObjectByType<BattleScreen>();
            if (screen == null) { Finish(); yield break; }
            // 場にフォロワーが並ぶまで AI に進めてもらう
            yield return new WaitForSeconds(9f);
            for (int step = 0; step < 3; step++)
            {
                screen.ShowFxPreview(step);
                yield return new WaitForSeconds(0.10f);
                yield return Shot($"fx{step}-a");
                yield return new WaitForSeconds(0.12f);
                yield return Shot($"fx{step}-b");
                yield return new WaitForSeconds(0.6f);
            }
            // 全体攻撃のように重い命中を 5 つ同時に出しても、時間の流れが等速に戻ることを確かめる(隕石で固まった不具合の再発防止)
            for (int i = 0; i < 5; i++) CoroutineHost.Run(Fx.Impact(screen.transform, screen.transform.position, Color.red, 1f, heavy: true));
            yield return new WaitForSecondsRealtime(1.0f);
            Debug.Log($"[DevAutoplay] 重い命中 5 つの後の timeScale={Time.timeScale}");
            Finish();
        }

        /// <summary>-preview-settings : 設定画面を開いて撮影する。</summary>
        private IEnumerator RunPreviewSettings()
        {
            yield return new WaitForSeconds(0.6f);
            yield return Shot("menu");
            AppRoot.Instance.ShowSettings();
            yield return null; yield return null;
            yield return Shot("settings");
            Finish();
        }

        /// <summary>-preview-draft : AI 対戦のデッキ選択 → ドラフト(時間切れも 1 回)→ 確認 → 対戦開始 を撮影する。</summary>
        private IEnumerator RunPreviewDraft()
        {
            yield return new WaitForSeconds(0.6f);
            AppRoot.Instance.ShowDeckSelect(BattleMode.VersusAi);
            yield return null; yield return null;
            var select = FindAnyObjectByType<DeckSelectScreen>();
            select!.SelectDraft();
            yield return null;
            yield return Shot("deckselect");
            select.Proceed();
            yield return null; yield return null;
            yield return Shot("draft1");
            var draft = FindAnyObjectByType<DraftScreen>()!;
            for (int n = 0; n < 4; n++) { draft.DevTake(n % 3); yield return null; }
            yield return new WaitForSeconds(11f);
            yield return Shot("draft5_hurry");
            yield return new WaitForSeconds(4.5f);   // 時間切れでランダムに取る
            yield return Shot("draft6_timeout");
            while (!draft.DevIsComplete) { draft.DevTake(1); yield return null; }
            yield return null;
            yield return Shot("confirm");
            draft.DevGo();
            yield return new WaitForSeconds(4f);
            yield return Shot("battle");
            Finish();
        }

        private IEnumerator RunHuman()
        {
            yield return new WaitForSeconds(0.5f);
            var app = AppRoot.Instance;
            app.StartBattle(new BattleConfig { Mode = BattleMode.VersusAi, Deck0 = app.Decks[0], Deck1 = app.Decks[^1], Seed = 11 });
            yield return null;
            var screen = FindAnyObjectByType<BattleScreen>();
            if (screen == null) { Debug.LogError("[DevAutoplay] BattleScreen が見つからない"); Finish(); yield break; }
            yield return PlayLoop(screen);
        }

        /// <summary>
        /// オンライン対戦の自動テスト。ホストはロビーで部屋を作りコードをファイルに書く。
        /// ゲストはファイルからコードを読んで参加する。開始後は両者とも人間操作を模擬する。
        /// </summary>
        private IEnumerator RunOnline(bool host)
        {
            yield return new WaitForSeconds(0.5f);
            var app = AppRoot.Instance;
            // -online-draft: ドラフトのデッキ(中身ごと送る経路)で試す
            bool draft = Environment.GetCommandLineArgs().Contains("-online-draft");
            app.ShowOnlineLobby(draft ? null : app.Decks[host ? 0 : 1]);   // null = ドラフトの部屋(案 2)
            yield return null;
            var lobby = FindAnyObjectByType<OnlineLobbyScreen>();
            if (lobby == null) { Debug.LogError("[DevAutoplay] ロビーが見つからない"); Finish(); yield break; }
            yield return Shot("lobby");

            if (host)
            {
                if (File.Exists(_codeFile!)) File.Delete(_codeFile!);
                lobby.transform.Find("Panel/Host")?.GetComponent<Button>()?.onClick.Invoke();
                float t0 = Time.realtimeSinceStartup;
                while (string.IsNullOrEmpty(Net.OnlineSession.Instance?.JoinCode) && Time.realtimeSinceStartup - t0 < 60) yield return null;
                var code = Net.OnlineSession.Instance?.JoinCode ?? "";
                Debug.Log("[DevAutoplay] joinCode=" + code);
                if (code.Length == 0) { yield return Shot("host-fail"); Finish(); yield break; }
                // 「招待リンクを送る」(Windows ではクリップボードへコピー)を押し、コピーされた招待文をゲストに渡す
                lobby.transform.Find("Panel/Share")?.GetComponent<Button>()?.onClick.Invoke();
                yield return new WaitForSeconds(0.3f);
                var invite = GUIUtility.systemCopyBuffer ?? "";
                Debug.Log("[DevAutoplay] invite=" + invite.Replace("\n", " / "));
                File.WriteAllText(_codeFile!, invite.Length > 0 ? invite : code);
                yield return Shot("hosting");
            }
            else
            {
                float t0 = Time.realtimeSinceStartup;
                while (!File.Exists(_codeFile!) && Time.realtimeSinceStartup - t0 < 90) yield return new WaitForSeconds(0.5f);
                if (!File.Exists(_codeFile!)) { Debug.LogError("[DevAutoplay] コードファイルが来ない"); Finish(); yield break; }
                var text = File.ReadAllText(_codeFile!).Trim();
                var args = Environment.GetCommandLineArgs();
                if (args.Contains("-online-invite"))
                {
                    // 招待リンクで開いた場合と同じ流れ: 招待を受け取る → (通常の部屋なら)デッキ選択 → 部屋で自動参加
                    Net.Invite.SetFromText(text);
                    app.RouteInvite();
                    yield return new WaitForSeconds(0.5f);
                    var select = FindAnyObjectByType<DeckSelectScreen>();
                    if (select != null)
                    {
                        yield return Shot("invite-deckselect");
                        select.transform.Find("Go")?.GetComponent<Button>()?.onClick.Invoke();
                    }
                }
                else if (args.Contains("-online-paste"))
                {
                    // 「貼り付け」: ホストがコピーした招待文をクリップボードから読んで参加する
                    lobby.transform.Find("Panel/Paste")?.GetComponent<Button>()?.onClick.Invoke();
                }
                else
                {
                    var field = lobby.transform.Find("Panel/CodeField")?.GetComponent<InputField>();
                    if (field != null) field.text = Net.Invite.ParseCode(text) ?? text;
                    lobby.transform.Find("Panel/Join")?.GetComponent<Button>()?.onClick.Invoke();
                }
                yield return new WaitForSeconds(0.3f);
                yield return Shot("joining");
            }

            float t1 = Time.realtimeSinceStartup;
            BattleScreen? screen = null;
            bool draftShot = false, waitShot = false;
            while (screen == null && Time.realtimeSinceStartup - t1 < 180)
            {
                screen = FindAnyObjectByType<BattleScreen>();
                // ドラフトの部屋: 2 人同時にドラフト(ホストの方が速く選び、相手待ちの画面も撮る)
                var ds = FindAnyObjectByType<DraftScreen>();
                if (ds != null && screen == null)
                {
                    if (!draftShot) { draftShot = true; yield return Shot("online-draft"); }
                    if (!ds.DevIsComplete) { ds.DevTake(host ? 0 : 2); yield return new WaitForSeconds(host ? 0.4f : 1.5f); continue; }
                    if (!waitShot) { waitShot = true; yield return Shot("online-draft-wait"); }
                }
                yield return new WaitForSeconds(0.5f);
            }
            if (screen == null) { Debug.LogError("[DevAutoplay] 対戦が始まらない"); yield return Shot("nostart"); Finish(); yield break; }
            yield return Shot("online-start");
            yield return PlayLoop(screen, maxSteps: 600, stepWait: 0.6f);
        }

        private IEnumerator PlayLoop(BattleScreen screen, int maxSteps = 400, float stepWait = 0.4f)
        {

            int guard = 0;
            bool demoDone = false;
            while (!screen.Session.State.IsFinished && guard++ < maxSteps)
            {
                yield return new WaitForSeconds(stepWait);

                // マリガン: 1 枚目を交換して決定
                if (screen.CurrentOverlay != null)
                {
                    var overlay = screen.CurrentOverlay;
                    var confirm = overlay.transform.Find("Confirm")?.GetComponent<Button>();
                    if (confirm != null)
                    {
                        var firstCard = overlay.GetComponentsInChildren<CardView>().FirstOrDefault();
                        if (firstCard != null) SimulateClick(firstCard.gameObject);
                        yield return Shot("mulligan");
                        confirm.onClick.Invoke();
                        continue;
                    }
                    // 結果画面など
                    if (screen.Session.State.IsFinished) break;
                    continue;
                }

                if (!screen.InputEnabled) continue;

                // 一度だけ: ツールチップと拡大表示の確認
                if (!demoDone)
                {
                    var kwCard = screen.HandViews.Concat(screen.MyBoardViews).Concat(screen.EnemyBoardViews)
                        .FirstOrDefault(v => KeywordHelp.ForCard(v.Definition).Count > 0);
                    if (kwCard != null)
                    {
                        demoDone = true;
                        var pd = new PointerEventData(EventSystem.current) { position = CenterOf(kwCard.gameObject) };
                        ExecuteEvents.Execute(kwCard.gameObject, pd, ExecuteEvents.pointerEnterHandler);
                        yield return new WaitForSeconds(0.7f);
                        yield return Shot("tooltip");
                        ExecuteEvents.Execute(kwCard.gameObject, pd, ExecuteEvents.pointerExitHandler);
                        SimulateClick(kwCard.gameObject);
                        yield return null;
                        yield return Shot("detail");
                        var ov = screen.CurrentOverlay;
                        if (ov != null) ov.GetComponent<Button>()?.onClick.Invoke();
                        continue;
                    }
                }

                // 対象選択中なら最初の候補をクリック
                if (screen.HasPending)
                {
                    var target = screen.PendingTargets[0];
                    var go = FindTargetObject(screen, target);
                    yield return Shot("targeting");
                    if (go != null) SimulateClick(go);
                    else Debug.LogError($"[DevAutoplay] 対象 {target} の UI が見つからない");
                    continue;
                }

                // 1. プレイ可能な手札をドラッグして自分の場へ
                var playable = screen.HandViews.FirstOrDefault(v => v.Draggable);
                if (playable != null)
                {
                    Debug.Log($"[DevAutoplay] 手札ドラッグ: {playable.Definition.Name}");
                    SimulateDrag(playable.gameObject, Center(screen.MyBoardRect));
                    yield return null;
                    yield return Shot("play");
                    continue;
                }

                // 2. 攻撃できるフォロワーをドラッグして相手リーダー(守護がいれば守護)へ
                var attacker = screen.MyBoardViews.FirstOrDefault(v => v.Draggable && v.Entity != null);
                if (attacker != null)
                {
                    var targets = screen.Session.Engine.ValidAttackTargets(attacker.Entity!);
                    var t = targets.FirstOrDefault(x => x.IsLeader);
                    if (targets.Count > 0 && !targets.Contains(t)) t = targets[0];
                    var go = FindTargetObject(screen, t);
                    if (go != null)
                    {
                        Debug.Log($"[DevAutoplay] 攻撃ドラッグ: {attacker.Definition.Name} → {t}");
                        SimulateDrag(attacker.gameObject, CenterOf(go));
                        yield return null;
                        yield return Shot("attack");
                        continue;
                    }
                }

                // 3. ターン終了
                if (screen.EndTurnButton.interactable)
                {
                    screen.EndTurnButton.onClick.Invoke();
                    yield return Shot("endturn");
                }
            }

            yield return new WaitForSeconds(1.0f);
            yield return Shot("result");
            Debug.Log($"[DevAutoplay] 決着: winner={screen.Session.State.Winner} turn={screen.Session.State.TurnNumber}");
            Finish();
        }

        private static GameObject? FindTargetObject(BattleScreen screen, TargetRef t)
        {
            if (t.IsLeader) return screen.EnemyLeaderRect.gameObject;
            return screen.EnemyBoardViews.Concat(screen.MyBoardViews)
                .FirstOrDefault(v => v.Entity != null && v.Entity.InstanceId == t.InstanceId)?.gameObject;
        }

        private static Vector2 Center(RectTransform rt) => rt.TransformPoint(rt.rect.center);
        private static Vector2 CenterOf(GameObject go) => Center((RectTransform)go.transform);

        /// <summary>EventSystem を通した本物に近いドラッグ模擬(Begin → Drag → End)。</summary>
        private static void SimulateDrag(GameObject from, Vector2 to)
        {
            var es = EventSystem.current;
            var pd = new PointerEventData(es) { button = PointerEventData.InputButton.Left, position = CenterOf(from), pointerPress = from, pointerDrag = from };
            ExecuteEvents.Execute(from, pd, ExecuteEvents.beginDragHandler);
            pd.position = to;
            pd.dragging = true;
            ExecuteEvents.Execute(from, pd, ExecuteEvents.dragHandler);
            var results = new List<RaycastResult>();
            es.RaycastAll(pd, results);
            pd.pointerCurrentRaycast = results.FirstOrDefault();
            ExecuteEvents.Execute(from, pd, ExecuteEvents.endDragHandler);
        }

        private static void SimulateClick(GameObject go)
        {
            var pd = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = CenterOf(go), dragging = false };
            ExecuteEvents.Execute(go, pd, ExecuteEvents.pointerClickHandler);
        }

        // ------------------------------------------------------------------

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(_dir!, $"{++_shotNo:00}-{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
        }

        private static void Finish()
        {
            Debug.Log("[DevAutoplay] 完了");
            Application.Quit();
        }
    }
}
