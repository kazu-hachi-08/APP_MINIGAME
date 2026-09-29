using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniGame.Molkky.Tests
{
    /// <summary>
    /// オンライン対戦で「投げた側の画面では倒れたのに、相手の画面では倒れていない」を防げているかの確認。
    /// 2台の端末を、毎回作り直したピン一式（＝物理の履歴が別）で順番に再現して比べる。
    /// </summary>
    public class MolkkyOnlineSyncTests
    {
        // 手番の合間に端末ごとに生じていたピン位置のずれ（採点表示中の滑り・棒の押し出し）を模した量
        private const float DriftAmount = 0.03f;

        // 受け側は投げる瞬間のフレーム位置を投げた側とわざとずらす（端末ごとのフレームのタイミング差を模す）
        private const int ObserverFrameOffset = 3;

        // 本番の MolkkyOnlineLink と同じ送信バッファの大きさ
        private const int MessageBufferSize = 512;

        private const int SentPinCount = 12;

        // ピンごとにずれの向きを変えるための角度の刻み（ラジアン）
        private const float DriftAngleStep = 2.4f;

        private static readonly ThrowRequest[] Throws =
        {
            new ThrowRequest(0f, 0f, 8f, ThrowStyle.Horizontal),
            new ThrowRequest(0.3f, -4f, 9f, ThrowStyle.Horizontal),
            new ThrowRequest(-0.4f, 5f, 7.5f, ThrowStyle.Vertical),
            new ThrowRequest(0.2f, -2f, 10f, ThrowStyle.Vertical),
            new ThrowRequest(0f, 1f, 8f, ThrowStyle.Horizontal, ThrowArc.High),
            new ThrowRequest(-0.2f, 3f, 9.5f, ThrowStyle.Vertical, ThrowArc.High),
        };

        [Test]
        public void 投擲メッセージにピン状態を載せても送受信で値が変わらない()
        {
            PinState[] sent = Enumerable.Range(0, SentPinCount)
                .Select(i => new PinState(new Vector2(i * 0.123f, 3.5f + i * 0.01f), i % 3 == 0, new Vector2(0.6f, -0.8f)))
                .ToArray();

            MethodInfo write = GetLinkMethod("WriteStates");
            MethodInfo read = GetLinkMethod("ReadStates");

            // 本番と同じ大きさのバッファに、投擲内容（float×3 + int×2）と一緒に収まることも確かめる
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(0f);
            writer.WriteValueSafe(0f);
            writer.WriteValueSafe(0f);
            writer.WriteValueSafe(0);
            writer.WriteValueSafe(0);
            write.Invoke(null, new object[] { writer, sent });

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out float _);
            reader.ReadValueSafe(out float _);
            reader.ReadValueSafe(out float _);
            reader.ReadValueSafe(out int _);
            reader.ReadValueSafe(out int _);
            var received = (PinState[])read.Invoke(null, new object[] { reader });

            Assert.AreEqual(sent.Length, received.Length);
            for (int i = 0; i < sent.Length; i++)
            {
                Assert.AreEqual(sent[i].Position, received[i].Position);
                Assert.AreEqual(sent[i].IsFallen, received[i].IsFallen);
                Assert.AreEqual(sent[i].FallDirection, received[i].FallDirection);
            }
        }

        [Test]
        public void キャラメッセージの席番号とキャラ番号が送受信で変わらない()
        {
            MethodInfo write = GetLinkMethod("WriteCharacter");
            MethodInfo read = GetLinkMethod("ReadCharacter");

            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            write.Invoke(null, new object[] { writer, 3, 2 });

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var (seat, characterIndex) = ((int, int))read.Invoke(null, new object[] { reader });

            Assert.AreEqual(3, seat);
            Assert.AreEqual(2, characterIndex);
        }

        [Test]
        public void チームメッセージの各席のチーム番号が送受信で変わらない()
        {
            Assert.AreEqual(new[] { 0, 1, 0, 0 }, RoundTripTeams(new[] { 0, 1, 0, 0 }));
        }

        [Test]
        public void 個人戦のチームメッセージは個人戦として届く()
        {
            Assert.IsNull(RoundTripTeams(null));
        }

        [Test]
        public void 正しいチーム設定はそのまま使われる()
        {
            int[] teams = { 0, 1, 1 };

            Assert.AreSame(teams, MolkkyOnlineLink.SanitizeTeams(teams, 3));
        }

        [Test]
        public void 壊れたチーム設定が届いたら個人戦として扱う()
        {
            Assert.IsNull(MolkkyOnlineLink.SanitizeTeams(new[] { 0, 1, 0 }, 4), "席数が合わない");
            Assert.IsNull(MolkkyOnlineLink.SanitizeTeams(new[] { 0, 2, 1 }, 3), "範囲外のチーム番号");
            Assert.IsNull(MolkkyOnlineLink.SanitizeTeams(new[] { 0, -1, 1 }, 3), "負のチーム番号");
            Assert.IsNull(MolkkyOnlineLink.SanitizeTeams(new[] { 0, 0, 0, 0 }, 4), "チームBが0人");
            Assert.IsNull(MolkkyOnlineLink.SanitizeTeams(null, 3), "個人戦");
        }

        [Test]
        public void 範囲外のキャラ番号が届いても先頭か末尾のキャラにクランプされる()
        {
            // 生成済みアセットの有無に左右されないよう、テスト用のカタログをその場で作る
            MolkkyCharacterData[] characters = Enumerable.Range(0, 4)
                .Select(_ => ScriptableObject.CreateInstance<MolkkyCharacterData>())
                .ToArray();
            var catalog = ScriptableObject.CreateInstance<MolkkyCharacterCatalog>();
            SetField(catalog, "_characters", characters);

            Assert.AreSame(characters[0], catalog.Get(-5));
            Assert.AreSame(characters[3], catalog.Get(catalog.Count + 10));

            Object.DestroyImmediate(catalog);
            foreach (MolkkyCharacterData character in characters) Object.DestroyImmediate(character);
        }

        [UnityTest]
        public IEnumerator 相手端末の再生で倒れるピンが投げた側と一致する()
        {
            yield return new EnterPlayMode();

            int mismatchesWithoutSync = 0;
            var failures = new List<string>();

            for (int i = 0; i < Throws.Length; i++)
            {
                ThrowRequest request = Throws[i];

                // 投げた側（A）：自分の配置のまま投げ、投げる直前の配置を送る
                var thrower = new RunResult();
                yield return RunThrow(request, null, 0, 0, thrower);

                // 相手側（B・修正後）：自分の配置は少しずれているが、届いた配置を適用してから再生する
                var observer = new RunResult();
                yield return RunThrow(request, thrower.StartStates, DriftAmount, i + ObserverFrameOffset, observer);

                // 比較用（B・修正前）：届いた配置を使わず、ずれた自分の配置のまま再生する
                var legacy = new RunResult();
                yield return RunThrow(request, null, DriftAmount, i + ObserverFrameOffset, legacy);

                string a = Describe(thrower.Fallen);
                string b = Describe(observer.Fallen);
                Debug.Log($"[MolkkySyncTest] throw#{i} A=[{a}] B(修正後)=[{b}] B(修正前)=[{Describe(legacy.Fallen)}] 位置の最大差={MaxPositionDiff(thrower.EndStates, observer.EndStates):F6}");

                if (a != b) failures.Add($"throw#{i}: A=[{a}] B=[{b}]");
                if (a != Describe(legacy.Fallen)) mismatchesWithoutSync++;
            }

            Debug.Log($"[MolkkySyncTest] 修正前の方式で倒れたピンが食い違った投擲: {mismatchesWithoutSync}/{Throws.Length}");

            yield return new ExitPlayMode();

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private class RunResult
        {
            public PinState[] StartStates;
            public PinState[] EndStates;
            public List<int> Fallen;
        }

        /// <summary>1台分の投擲を、新しく作ったピン一式で再現する（MolkkyGameManager.ExecuteThrow と同じ順序）</summary>
        /// <param name="receivedStates">相手端末から届いた投げる直前の配置。null なら自分の配置のまま</param>
        /// <param name="drift">この端末のピン位置のずれ</param>
        /// <param name="framesBeforeThrow">投げる前に待つフレーム数。端末ごとのフレームのタイミング差を作る</param>
        private static IEnumerator RunThrow(ThrowRequest request, PinState[] receivedStates, float drift,
            int framesBeforeThrow, RunResult result)
        {
            GameObject root = CreateRig(out PinRack rack, out StickThrower stick, out ThrowSettleWatcher watcher);
            yield return new WaitForFixedUpdate();

            ApplyDrift(rack, drift);
            for (int f = 0; f < framesBeforeThrow; f++) yield return null;

            // MolkkyGameManager と同じく、投げる側は自分の配置を取り直して適用、受け側は届いた配置を適用する
            PinState[] start = receivedStates ?? rack.CaptureStates();
            rack.ApplyStates(start);
            result.StartStates = start;

            bool settled = false;
            watcher.Settled += () => settled = true;
            rack.ArmAll();
            stick.Throw(request);
            watcher.Begin();
            yield return new WaitUntil(() => settled);

            rack.DisarmAll();
            result.EndStates = rack.CaptureStates();
            result.Fallen = rack.CollectFallenNumbers();
            result.Fallen.Sort();

            Object.Destroy(root);
            yield return new WaitForFixedUpdate();
        }

        private static GameObject CreateRig(out PinRack rack, out StickThrower stick, out ThrowSettleWatcher watcher)
        {
            string guid = AssetDatabase.FindAssets("t:MolkkyPhysicsSettings").First();
            var settings = AssetDatabase.LoadAssetAtPath<MolkkyPhysicsSettings>(AssetDatabase.GUIDToAssetPath(guid));

            // Awake より先に参照を入れたいので、非アクティブで組み立ててから有効にする
            var root = new GameObject("MolkkySyncTestRig");
            root.SetActive(false);

            rack = CreateChild(root).AddComponent<PinRack>();
            SetField(rack, "_settings", settings);

            stick = CreateChild(root).AddComponent<StickThrower>();
            SetField(stick, "_settings", settings);

            watcher = root.AddComponent<ThrowSettleWatcher>();
            SetField(watcher, "_settings", settings);
            SetField(watcher, "_pinRack", rack);
            SetField(watcher, "_stick", stick);

            root.SetActive(true);
            return root;
        }

        /// <summary>非アクティブな親の下に先に置くことで、AddComponent 時に Awake が走らないようにする</summary>
        private static GameObject CreateChild(GameObject root)
        {
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform, false);
            return child;
        }

        /// <summary>ピンごとに向きの違う小さなずれを入れる（端末ごとに滑り方が違ったのを模す）</summary>
        private static void ApplyDrift(PinRack rack, float drift)
        {
            if (drift == 0f) return;

            PinState[] states = rack.CaptureStates();
            for (int i = 0; i < states.Length; i++)
            {
                float angle = i * DriftAngleStep;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * drift;
                states[i] = new PinState(states[i].Position + offset, false, Vector2.zero);
            }

            rack.ApplyStates(states);
        }

        private static int[] RoundTripTeams(int[] sent)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            GetLinkMethod("WriteTeams").Invoke(null, new object[] { writer, sent });

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            return (int[])GetLinkMethod("ReadTeams").Invoke(null, new object[] { reader });
        }

        private static MethodInfo GetLinkMethod(string name)
        {
            return typeof(MolkkyOnlineLink).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private static string Describe(List<int> fallen)
        {
            return string.Join(",", fallen);
        }

        private static float MaxPositionDiff(PinState[] a, PinState[] b)
        {
            float max = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                max = Mathf.Max(max, Vector2.Distance(a[i].Position, b[i].Position));
            }

            return max;
        }
    }
}
