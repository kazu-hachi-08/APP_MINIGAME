#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// 戦闘演出(04-screens.md「エフェクト」)。画像アセットを使わず、実行時に生成したスプライトを
    /// uGUI の Image として動かす。光り物は加算合成(Resources/Shaders/UIAdditive)で重ねるので、
    /// 重なるほど明るくなり「発光して見える」。WebGL / スマホでも負荷は低い。
    /// </summary>
    public static class Fx
    {
        private static readonly Dictionary<string, Sprite> Cache = new();
        private const int Size = 256;

        private static Material? _additive;
        /// <summary>光り物用の加算マテリアル。読めなければ通常合成にフォールバックする。</summary>
        internal static Material? Additive
        {
            get
            {
                if (_additive == null)
                {
                    var sh = Resources.Load<Shader>("Shaders/UIAdditive") ?? Shader.Find("CardGame/UIAdditive");
                    if (sh != null) _additive = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                }
                return _additive;
            }
        }

        // ==================================================================
        // スプライト(すべて手続き生成)
        // ==================================================================

        /// <summary>中心が白く外へ滑らかに減衰する光。</summary>
        public static Sprite Glow() => Build("glow", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - r);
            return new Color(1, 1, 1, Mathf.Pow(a, 2.2f));
        });

        /// <summary>内側が明るく外へ消える輪(衝撃波)。</summary>
        public static Sprite Shockwave() => Build("shock", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.80f) / 0.13f);
            a = Mathf.Pow(a, 1.6f);
            // 内側にうっすら広がる光を足して厚みを出す
            a += Mathf.Clamp01(1f - r / 0.80f) * 0.12f;
            return new Color(1, 1, 1, Mathf.Clamp01(a));
        });

        /// <summary>細く鋭い弧(斬撃)。中央が太く、両端で鋭く消える。</summary>
        public static Sprite Slash() => Build("slash", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Atan2(y, x);
            float taper = Mathf.Clamp01(1f - Mathf.Abs(ang) / 1.25f);
            float width = 0.10f * Mathf.Pow(taper, 0.8f);
            if (width <= 0.001f) return new Color(1, 1, 1, 0);
            float d = Mathf.Abs(r - 0.80f);
            float a = Mathf.Clamp01(1f - d / width);
            // 芯を白く、外縁をぼかす
            float core = Mathf.Clamp01(1f - d / (width * 0.35f));
            return new Color(1, 1, 1, Mathf.Clamp01(Mathf.Pow(a, 1.6f) * 0.85f + core * 0.5f));
        });

        /// <summary>放射状に伸びる光条(4 本 + 細い 4 本)。</summary>
        public static Sprite Star() => Build("star", (x, y) =>
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float r = Mathf.Sqrt(x * x + y * y);
            float fall = Mathf.Clamp01(1f - r);
            float v = Mathf.Clamp01(1f - ax / 0.055f) * fall;
            float h = Mathf.Clamp01(1f - ay / 0.055f) * fall;
            float dx = Mathf.Abs(x - y) * 0.7071f, dy = Mathf.Abs(x + y) * 0.7071f;
            float d1 = Mathf.Clamp01(1f - dx / 0.035f) * fall * 0.6f;
            float d2 = Mathf.Clamp01(1f - dy / 0.035f) * fall * 0.6f;
            float core = Mathf.Pow(Mathf.Clamp01(1f - r / 0.22f), 2f);
            return new Color(1, 1, 1, Mathf.Clamp01(v + h + d1 + d2 + core));
        });

        /// <summary>細長い尾(飛ぶ光の軌跡)。左が細く右が太い。</summary>
        public static Sprite Streak() => Build("streak", (x, y) =>
        {
            float head = Mathf.Clamp01((x + 1f) / 2f);              // 0(尾)→ 1(頭)
            float width = 0.06f + 0.30f * head * head;
            float a = Mathf.Clamp01(1f - Mathf.Abs(y) / width) * Mathf.Pow(head, 1.5f);
            return new Color(1, 1, 1, Mathf.Pow(a, 1.4f));
        });

        /// <summary>魔法陣(二重円 + 目盛り + ルーン風の点)。召喚に使う。</summary>
        public static Sprite RuneCircle() => Build("rune", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Atan2(y, x);
            float a = 0;
            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.95f) / 0.022f);          // 外円
            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / 0.014f) * 0.8f;   // 内円
            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.42f) / 0.016f) * 0.7f;   // 中心の円
            // 目盛り(24 本)
            float ticks = Mathf.Abs(Mathf.Sin(ang * 12f));
            if (r > 0.80f && r < 0.94f) a += Mathf.Clamp01((ticks - 0.93f) / 0.07f);
            // ルーン風の点(8 個)
            float runes = Mathf.Abs(Mathf.Sin(ang * 4f));
            if (r > 0.50f && r < 0.70f) a += Mathf.Clamp01((runes - 0.97f) / 0.03f) * 0.9f;
            return new Color(1, 1, 1, Mathf.Clamp01(a));
        });

        /// <summary>もやもやした煙(ノイズ)。破壊に使う。</summary>
        public static Sprite Smoke() => Build("smoke", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float n = Mathf.PerlinNoise((x + 1f) * 3.2f + 11f, (y + 1f) * 3.2f + 5f);
            float n2 = Mathf.PerlinNoise((x + 1f) * 7f + 31f, (y + 1f) * 7f + 17f);
            float a = Mathf.Clamp01(1f - r) * (0.55f + n * 0.6f + n2 * 0.25f);
            return new Color(1, 1, 1, Mathf.Clamp01(a * a));
        });

        /// <summary>下から上へ細くなる光柱。</summary>
        public static Sprite Pillar() => Build("pillar", (x, y) =>
        {
            float t = Mathf.Clamp01((y + 1f) / 2f);                  // 0(下)→ 1(上)
            float width = Mathf.Lerp(0.85f, 0.25f, t);
            float a = Mathf.Clamp01(1f - Mathf.Abs(x) / width) * Mathf.Clamp01(1.15f - t);
            return new Color(1, 1, 1, Mathf.Pow(a, 1.7f));
        });

        private static Sprite Build(string key, System.Func<float, float, Color> shader)
        {
            if (Cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                px[y * Size + x] = shader((x + 0.5f) / Size * 2 - 1, (y + 0.5f) / Size * 2 - 1);
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            Cache[key] = s;
            return s;
        }

        // ==================================================================
        // 部品
        // ==================================================================

        private static Image Piece(Transform layer, Sprite sprite, Vector3 world, Vector2 size, Color color, float rotation = 0, bool additive = true)
        {
            var go = new GameObject("Fx", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(layer, false);
            rt.sizeDelta = size;
            rt.position = world;
            rt.localRotation = Quaternion.Euler(0, 0, rotation);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (additive && Additive != null) img.material = Additive;
            return img;
        }

        private static Image Piece(Transform layer, Sprite sprite, Vector3 world, float size, Color color, float rotation = 0, bool additive = true)
            => Piece(layer, sprite, world, new Vector2(size, size), color, rotation, additive);

        /// <summary>大きさ・回転・不透明度を時間で動かして消える。ease は 0..1 → 0..1。</summary>
        private static IEnumerator Anim(Image img, float dur, float fromScale, float toScale, float spin = 0f, float fadeStart = 0f, float delay = 0f)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (img == null) yield break;
            var rt = img.rectTransform;
            var baseColor = img.color;
            float z = rt.localEulerAngles.z;
            float t = 0;
            while (t < dur && img != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(fromScale, toScale, 1f - Mathf.Pow(1f - k, 3f));   // ease-out cubic
                rt.localScale = new Vector3(s, s, 1);
                if (spin != 0) rt.localRotation = Quaternion.Euler(0, 0, z + spin * k);
                float fade = k < fadeStart ? 1f : 1f - (k - fadeStart) / Mathf.Max(0.001f, 1f - fadeStart);
                var c = baseColor; c.a = baseColor.a * fade; img.color = c;
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        private static IEnumerator Fly(Image img, Vector2 velocity, float gravity, float dur, float spin = 0f)
        {
            var rt = img.rectTransform;
            var baseColor = img.color;
            var start = rt.anchoredPosition;
            float z = rt.localEulerAngles.z;
            float t = 0;
            while (t < dur && img != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                rt.anchoredPosition = start + velocity * t + new Vector2(0, gravity * t * t);
                if (spin != 0) rt.localRotation = Quaternion.Euler(0, 0, z + spin * t);
                var c = baseColor; c.a = baseColor.a * (1 - k * k); img.color = c;
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        // ==================================================================
        // 演出
        // ==================================================================

        /// <summary>攻撃の命中。交差する斬撃 + 衝撃波 + 火花 + 煙、強いときは一瞬止まる(ヒットストップ)。</summary>
        public static IEnumerator Impact(Transform layer, Vector3 world, Color color, float scale = 1f, bool heavy = false)
        {
            float rot = Random.Range(-40f, 40f);
            var warm = new Color(1f, 0.92f, 0.72f);

            // 斬撃 2 本(交差)
            var s1 = Piece(layer, Slash(), world, 260 * scale, warm, rot);
            var s2 = Piece(layer, Slash(), world, 210 * scale, new Color(color.r, color.g, color.b, 0.9f), rot + 145f);
            CoroutineHost.Run(Anim(s1, 0.22f, 0.45f, 1.35f, 30f, 0.25f));
            CoroutineHost.Run(Anim(s2, 0.26f, 0.35f, 1.2f, -24f, 0.2f, 0.03f));

            // 芯の閃光 + 衝撃波 2 枚(時間差)
            var core = Piece(layer, Glow(), world, 150 * scale, new Color(1f, 0.97f, 0.9f, 1f));
            CoroutineHost.Run(Anim(core, 0.16f, 1.1f, 0.25f));
            var w1 = Piece(layer, Shockwave(), world, 170 * scale, color);
            CoroutineHost.Run(Anim(w1, 0.34f, 0.25f, 2.2f, 0f, 0.15f));
            var w2 = Piece(layer, Shockwave(), world, 170 * scale, new Color(warm.r, warm.g, warm.b, 0.6f));
            CoroutineHost.Run(Anim(w2, 0.40f, 0.2f, 2.9f, 0f, 0.1f, 0.06f));

            // 火花(放射状)
            int sparks = heavy ? 14 : 9;
            for (int i = 0; i < sparks; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(180f, 460f) * scale;
                var sp = Piece(layer, Streak(), world, new Vector2(Random.Range(40f, 90f) * scale, Random.Range(6f, 12f) * scale),
                    Color.Lerp(warm, color, Random.value), a * Mathf.Rad2Deg);
                CoroutineHost.Run(Fly(sp, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, -500f, Random.Range(0.25f, 0.45f)));
            }

            // 砂煙(通常合成で暗く)
            var dust = Piece(layer, Smoke(), world, 190 * scale, new Color(0.35f, 0.3f, 0.28f, 0.5f), Random.Range(0, 360f), additive: false);
            CoroutineHost.Run(Anim(dust, 0.5f, 0.5f, 1.5f, 12f, 0.1f));

            if (heavy)
            {
                // ヒットストップ: 一瞬だけ時間を止めて重さを出す
                CoroutineHost.Run(HitStop(0.07f));
                yield return new WaitForSecondsRealtime(0.1f);
            }
            else yield return new WaitForSeconds(0.08f);
        }

        private static float _hitStopUntil;
        private static bool _hitStopRunning;

        /// <summary>
        /// 一瞬だけ時間を遅くする。重なったら 1 つにまとめ、終わったら必ず等速(1)に戻す。
        /// (以前は開始時の速さを覚えて戻していたため、全体攻撃で同時に走ると遅いまま戻らず、ゲームが 1/50 の速さで固まった。2026-09-23)
        /// </summary>
        public static IEnumerator HitStop(float seconds)
        {
            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.realtimeSinceStartup + seconds);
            if (_hitStopRunning) yield break;
            _hitStopRunning = true;
            Time.timeScale = 0.02f;
            while (Time.realtimeSinceStartup < _hitStopUntil) yield return null;
            Time.timeScale = 1f;
            _hitStopRunning = false;
        }

        /// <summary>画面全体を一瞬光らせる(強い一撃)。layer は画面いっぱいの矩形。</summary>
        public static void ScreenFlash(RectTransform layer, Color color, float strength = 0.5f)
        {
            var img = new GameObject("Flash", typeof(RectTransform)).AddComponent<Image>();
            var rt = img.rectTransform;
            rt.SetParent(layer, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            img.color = new Color(color.r, color.g, color.b, strength);
            img.raycastTarget = false;
            if (Additive != null) img.material = Additive;
            CoroutineHost.Run(FadeAndDie(img, 0.22f));
        }

        private static IEnumerator FadeAndDie(Image img, float dur)
        {
            float t = 0; var baseColor = img.color;
            while (t < dur && img != null)
            {
                t += Time.deltaTime;
                var c = baseColor; c.a = baseColor.a * (1 - t / dur); img.color = c;
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        /// <summary>召喚: 魔法陣が回りながら広がり、光柱が立って粒が昇る。</summary>
        public static void Summon(Transform layer, RectTransform target, Color color)
        {
            float w = target.rect.width * target.lossyScale.x;
            float h = target.rect.height * target.lossyScale.y;
            var foot = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMin, 0));

            // 床の魔法陣(縦に潰して床置きに見せる)
            var rune = Piece(layer, RuneCircle(), foot, w * 1.5f, new Color(color.r, color.g, color.b, 0.95f));
            CoroutineHost.Run(FlatSpin(rune, 0.55f, 0.35f, 1.15f, 90f));
            var ring = Piece(layer, Shockwave(), foot, w * 1.2f, color);
            CoroutineHost.Run(FlatSpin(ring, 0.45f, 0.2f, 1.8f, 0f));

            // 光柱
            var pillar = Piece(layer, Pillar(), foot + new Vector3(0, h * 0.55f, 0), new Vector2(w * 0.95f, h * 1.15f),
                new Color(color.r, color.g, color.b, 0.85f));
            CoroutineHost.Run(Anim(pillar, 0.45f, 1.05f, 0.9f, 0f, 0.25f));

            // 昇る粒
            for (int i = 0; i < 8; i++)
            {
                var mote = Piece(layer, Glow(), foot + new Vector3(Random.Range(-w * 0.45f, w * 0.45f), Random.Range(-8f, 12f), 0),
                    Random.Range(10f, 22f), new Color(color.r, color.g, color.b, 0.9f));
                CoroutineHost.Run(Fly(mote, new Vector2(Random.Range(-18f, 18f), Random.Range(110f, 230f)), 0f, Random.Range(0.4f, 0.7f)));
            }
        }

        /// <summary>床に置いた輪の演出(縦に潰して拡大 + 回転)。</summary>
        private static IEnumerator FlatSpin(Image img, float dur, float from, float to, float spin)
        {
            var rt = img.rectTransform;
            var baseColor = img.color;
            float t = 0;
            while (t < dur && img != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - k, 3f));
                rt.localScale = new Vector3(s, s * 0.38f, 1);
                if (spin != 0) rt.localRotation = Quaternion.Euler(0, 0, spin * k);
                var c = baseColor; c.a = baseColor.a * (1 - k * k); img.color = c;
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        /// <summary>破壊: 閃光 → 煙 → 破片と残り火。</summary>
        public static void Destroyed(Transform layer, Vector3 world, Color color)
        {
            var flash = Piece(layer, Glow(), world, 170, new Color(1f, 0.85f, 0.7f, 0.9f));
            CoroutineHost.Run(Anim(flash, 0.14f, 0.8f, 1.3f));

            for (int i = 0; i < 3; i++)
            {
                var smoke = Piece(layer, Smoke(), world + new Vector3(Random.Range(-30f, 30f), Random.Range(-20f, 20f), 0),
                    Random.Range(150f, 230f), new Color(0.16f, 0.14f, 0.16f, 0.8f), Random.Range(0, 360f), additive: false);
                CoroutineHost.Run(Anim(smoke, Random.Range(0.45f, 0.7f), 0.5f, 1.6f, Random.Range(-40f, 40f), 0.25f, i * 0.04f));
            }
            // 破片
            for (int i = 0; i < 10; i++)
            {
                float a = Random.Range(0.15f, Mathf.PI - 0.15f);
                var shard = Piece(layer, Streak(), world, new Vector2(Random.Range(26f, 52f), Random.Range(5f, 10f)),
                    new Color(color.r, color.g, color.b, 0.95f), Random.Range(0, 360f));
                CoroutineHost.Run(Fly(shard, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(140f, 320f), -900f, Random.Range(0.4f, 0.65f), Random.Range(-500f, 500f)));
            }
            // 残り火
            for (int i = 0; i < 6; i++)
            {
                var ember = Piece(layer, Glow(), world + new Vector3(Random.Range(-40f, 40f), Random.Range(-30f, 10f), 0),
                    Random.Range(8f, 16f), new Color(1f, 0.55f, 0.25f, 0.9f));
                CoroutineHost.Run(Fly(ember, new Vector2(Random.Range(-30f, 30f), Random.Range(60f, 140f)), -40f, Random.Range(0.5f, 0.9f)));
            }
        }

        /// <summary>スペル / 効果ダメージ: 尾を引いて飛ぶ光弾。</summary>
        public static IEnumerator Projectile(Transform layer, Vector3 from, Vector3 to, Color color)
        {
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            var tail = Piece(layer, Streak(), from, new Vector2(190f, 46f), new Color(color.r, color.g, color.b, 0.9f), angle);
            var head = Piece(layer, Glow(), from, 90f, new Color(1f, 0.97f, 0.9f, 1f));
            var star = Piece(layer, Star(), from, 150f, new Color(color.r, color.g, color.b, 0.9f));

            const float dur = 0.26f;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = 1f - Mathf.Pow(1f - k, 2f);
                var p = Vector3.Lerp(from, to, e);
                if (tail != null) tail.rectTransform.position = p;
                if (head != null) head.rectTransform.position = p;
                if (star != null) { star.rectTransform.position = p; star.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 540f); }
                // 軌跡(残像)
                if (Random.value < 0.6f)
                {
                    var ghost = Piece(layer, Glow(), p, Random.Range(28f, 52f), new Color(color.r, color.g, color.b, 0.55f));
                    CoroutineHost.Run(Anim(ghost, 0.3f, 1f, 0.2f));
                }
                yield return null;
            }
            if (tail != null) Object.Destroy(tail.gameObject);
            if (head != null) Object.Destroy(head.gameObject);
            if (star != null) Object.Destroy(star.gameObject);
        }

        /// <summary>回復: 緑の光の粒と輪が昇る。</summary>
        public static void Heal(Transform layer, Vector3 world, Color color)
        {
            var ring = Piece(layer, Shockwave(), world, 150f, new Color(color.r, color.g, color.b, 0.8f));
            CoroutineHost.Run(FlatSpin(ring, 0.5f, 1.2f, 0.4f, 0f));
            for (int i = 0; i < 8; i++)
            {
                var p = Piece(layer, Star(), world + new Vector3(Random.Range(-55f, 55f), Random.Range(-40f, 20f), 0),
                    Random.Range(30f, 56f), new Color(color.r, color.g, color.b, 0.95f), Random.Range(0, 360f));
                CoroutineHost.Run(Fly(p, new Vector2(Random.Range(-20f, 20f), Random.Range(90f, 190f)), 0f, Random.Range(0.5f, 0.85f), Random.Range(-120f, 120f)));
            }
        }

        /// <summary>強化: 金の輪が下から上へ抜け、光条が散る。</summary>
        public static void Buff(Transform layer, RectTransform target)
        {
            float w = target.rect.width * target.lossyScale.x;
            float h = target.rect.height * target.lossyScale.y;
            var foot = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMin, 0));
            var gold = FantasyUi.Gold;
            for (int i = 0; i < 2; i++)
            {
                var ring = Piece(layer, Shockwave(), foot, w * 1.15f, new Color(gold.r, gold.g, gold.b, 0.95f));
                CoroutineHost.Run(RiseRing(ring, h * 0.95f, 0.45f, i * 0.12f));
            }
            for (int i = 0; i < 5; i++)
            {
                var p = Piece(layer, Star(), foot + new Vector3(Random.Range(-w * 0.4f, w * 0.4f), Random.Range(0, h * 0.3f), 0),
                    Random.Range(26f, 44f), new Color(gold.r, gold.g, gold.b, 0.9f), Random.Range(0, 360f));
                CoroutineHost.Run(Fly(p, new Vector2(Random.Range(-25f, 25f), Random.Range(120f, 220f)), 0f, Random.Range(0.35f, 0.6f)));
            }
        }

        private static IEnumerator RiseRing(Image img, float distance, float dur, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (img == null) yield break;
            var rt = img.rectTransform;
            var baseColor = img.color;
            var start = rt.anchoredPosition;
            float t = 0;
            while (t < dur && img != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                rt.anchoredPosition = start + new Vector2(0, distance * k);
                rt.localScale = new Vector3(Mathf.Lerp(0.7f, 1.1f, k), 0.4f, 1);
                var c = baseColor; c.a = baseColor.a * (1 - k); img.color = c;
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        /// <summary>指定の矩形を短く揺らす。</summary>
        public static IEnumerator Shake(RectTransform rt, float strength, float dur)
        {
            var start = rt.anchoredPosition;
            float t = 0;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float damp = Mathf.Pow(1 - t / dur, 2f);
                float a = Random.Range(0f, Mathf.PI * 2f);
                rt.anchoredPosition = start + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * strength * damp;
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = start;
        }
    }

    /// <summary>
    /// 発火して放置するコルーチン用の常駐オブジェクト。
    /// (エフェクトの発生元が破棄されてもアニメーションが止まらないようにする)
    /// </summary>
    public sealed class CoroutineHost : MonoBehaviour
    {
        private static CoroutineHost? _instance;

        public static void Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                var go = new GameObject("FxHost");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineHost>();
            }
            _instance.StartCoroutine(routine);
        }
    }
}
