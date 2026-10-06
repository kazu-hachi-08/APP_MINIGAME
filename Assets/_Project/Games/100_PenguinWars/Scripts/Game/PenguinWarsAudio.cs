using System;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争のSE（出撃ポンッ・ヒットペチッ・撃破・ペンギン砲・レベルUP・城崩れ、
    /// ステージモードの警報・勝利・★・仲間・鍵）とBGMをプログラムで生成して鳴らす（仕様書 §9・ステージ計画 Phase 7）。
    /// 他のゲームの *Audio と同じく、正式な素材が用意できたらクリップ欄に差し込めばそのまま置き換わる
    /// </summary>
    public class PenguinWarsAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        // 同じ音の繰り返しが機械的に聞こえないよう、ピッチを少しだけ揺らす幅
        private const float PitchJitter = 0.08f;

        [Header("素材が用意できたら差し込む（未設定なら生成音を使う）")]
        [SerializeField] private AudioClip _spawnClip;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _deathClip;
        [SerializeField] private AudioClip _cannonClip;
        [SerializeField] private AudioClip _collapseClip;
        [SerializeField] private AudioClip _alarmClip;
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField] private AudioClip _starClip;
        [SerializeField] private AudioClip _fanfareClip;
        [SerializeField] private AudioClip _unlockClip;
        [Header("BGM（Audio/ に決まった名前で置くと Rebuild PenguinWars で自動で差し込まれる。仕様書 §9）")]
        [Tooltip("タイトル〜編成発表のBGM。未設定ならコードで作った仮のBGMを鳴らす")]
        [SerializeField] private AudioClip _titleBgmClip;
        [Tooltip("プレイ中のBGM。未設定ならコードで作った仮のBGMを鳴らす")]
        [SerializeField] private AudioClip _bgmClip;
        [Tooltip("ボスがいる間のBGM。未設定なら、プレイ中のBGMが生成音のときだけ速く低くした仮のBGMを鳴らす")]
        [SerializeField] private AudioClip _bossBgmClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _spawnVolume = 0.5f;
        [Tooltip("一番よく鳴る音なので小さめにする")]
        [Range(0f, 1f)] [SerializeField] private float _hitVolume = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _deathVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _cannonVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _jingleVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.6f;
        [SerializeField] private float _bgmFadeSeconds = 0.5f;
        [Tooltip("★を1つ出すごとの音の高さ（左の★から順に）。だんだん上がって「そろった」感じを出す")]
        [SerializeField] private float[] _starPitches = { 1f, 1.26f, 1.5f };

        [Header("同じ音を鳴らす最短間隔（秒）。大量の敵でも音が割れず、うるさくならないように")]
        [SerializeField] private float _hitMinInterval = 0.08f;
        [SerializeField] private float _defaultMinInterval = 0.04f;

        private readonly Dictionary<AudioClip, float> _lastPlayTimes = new Dictionary<AudioClip, float>();

        private void Awake()
        {
            // 生成は1回だけ。以降は同じクリップを使い回す
            if (_spawnClip == null) _spawnClip = CreateSweep("Se_PenguinSpawn", 320f, 760f, 0.12f);
            if (_hitClip == null) _hitClip = CreateSlap("Se_PenguinHit");
            if (_deathClip == null) _deathClip = CreateSweep("Se_PenguinDeath", 500f, 1300f, 0.4f);
            if (_cannonClip == null) _cannonClip = CreateRumble("Se_PenguinCannon", 0.8f, 70f, 4f);
            if (_collapseClip == null) _collapseClip = CreateRumble("Se_PenguinCollapse", 1.5f, 45f, 1.8f);
            if (_alarmClip == null) _alarmClip = CreateNotes("Se_PenguinAlarm", new[] { 880f, 659f, 880f, 659f, 880f, 659f }, 0.15f);
            if (_victoryClip == null) _victoryClip = CreateNotes("Se_PenguinVictory", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1047f }, 0.12f);
            if (_starClip == null) _starClip = CreateNotes("Se_PenguinStar", new[] { 1047f, 1568f }, 0.07f);
            if (_fanfareClip == null) _fanfareClip = CreateNotes("Se_PenguinFanfare", new[] { 784f, 784f, 784f, 1047f, 1047f }, 0.1f);
            if (_unlockClip == null) _unlockClip = CreateNotes("Se_PenguinUnlock", new[] { 1319f, 1760f }, 0.08f);
            if (_titleBgmClip == null) _titleBgmClip = PenguinBgmGenerator.CreateTitle();
            // 素材の戦闘曲に生成音のボス曲を続けると曲の雰囲気が急に変わるので、そのときはボス曲に切り替えない
            if (_bossBgmClip == null && _bgmClip == null) _bossBgmClip = PenguinBgmGenerator.CreateBoss();
            if (_bgmClip == null) _bgmClip = PenguinBgmGenerator.CreateBattle();
        }

        public void PlaySpawn() => Play(_spawnClip, _spawnVolume, RandomPitch(), _defaultMinInterval);

        public void PlayHit() => Play(_hitClip, _hitVolume, RandomPitch(), _hitMinInterval);

        public void PlayDeath() => Play(_deathClip, _deathVolume, RandomPitch(), _defaultMinInterval);

        public void PlayCannon() => Play(_cannonClip, _cannonVolume, 1f, _defaultMinInterval);

        public void PlayCollapse() => Play(_collapseClip, _cannonVolume, 1f, _defaultMinInterval);

        public void PlayAlarm() => Play(_alarmClip, _jingleVolume, 1f, _defaultMinInterval);

        public void PlayVictory() => Play(_victoryClip, _jingleVolume, 1f, _defaultMinInterval);

        /// <param name="index">左から何番目の★か（0 始まり）</param>
        public void PlayStar(int index)
        {
            float pitch = _starPitches.Length > 0 ? _starPitches[Mathf.Clamp(index, 0, _starPitches.Length - 1)] : 1f;
            Play(_starClip, _jingleVolume, pitch, _defaultMinInterval);
        }

        public void PlayFanfare() => Play(_fanfareClip, _jingleVolume, 1f, _defaultMinInterval);

        public void PlayUnlock() => Play(_unlockClip, _jingleVolume, 1f, _defaultMinInterval);

        public void PlayTitleBgm() => PlayBgm(_titleBgmClip);

        public void PlayBattleBgm() => PlayBgm(_bgmClip);

        /// <summary>ボスが出た・倒した（全員いなくなった）ときに曲を切り替える。ボス曲が無ければ何もしない</summary>
        public void SetBossBgm(bool bossPresent)
        {
            if (_bossBgmClip == null) return;

            PlayBgm(bossPresent ? _bossBgmClip : _bgmClip);
        }

        public void StopBgm()
        {
            if (!AudioManager.HasInstance) return;

            AudioManager.Instance.StopBgm(_bgmFadeSeconds);
        }

        /// <summary>素材が無いときは何もしない（止めると、前に流れていた曲まで消えて無音になるため）</summary>
        private void PlayBgm(AudioClip clip)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlayBgmClip(clip, _bgmFadeSeconds, _bgmVolume);
        }

        private static float RandomPitch() => UnityEngine.Random.Range(1f - PitchJitter, 1f + PitchJitter);

        /// <summary>同じフレーム・短い間隔で同じ音が重なったら1回にまとめる</summary>
        private void Play(AudioClip clip, float volume, float pitch, float minInterval)
        {
            if (clip == null || !AudioManager.HasInstance) return;
            if (_lastPlayTimes.TryGetValue(clip, out float last) && Time.unscaledTime - last < minInterval) return;

            _lastPlayTimes[clip] = Time.unscaledTime;
            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>音程が滑らかに上がる「ポンッ」「ひゅ〜ん」。出撃と、魂が昇る撃破に使う</summary>
        private static AudioClip CreateSweep(string name, float fromFrequency, float toFrequency, float duration)
        {
            // 立ち上がりを一瞬だけなだらかにして、鳴り始めのプツッというノイズを消す
            const float attackTime = 0.005f;
            float slope = (toFrequency - fromFrequency) / duration;
            return CreateClip(name, duration, t =>
            {
                // 周波数を直線で変えるときの位相は、周波数を時間で積分したもの
                float phase = 2f * Mathf.PI * (fromFrequency * t + 0.5f * slope * t * t);
                float envelope = Mathf.Min(1f, t / attackTime) * (1f - t / duration);
                return Mathf.Sin(phase) * envelope * 0.6f;
            });
        }

        /// <summary>ごく短いノイズと高めの打撃音を重ねた「ペチッ」</summary>
        private static AudioClip CreateSlap(string name)
        {
            const float duration = 0.07f;
            var random = new System.Random(1);
            return CreateClip(name, duration, t =>
            {
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 90f);
                float tone = Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-t * 60f);
                return (noise * 0.6f + tone * 0.4f) * 0.8f;
            });
        }

        /// <summary>低いうなりにこもったノイズを重ねた「ドーン」「ゴゴゴ」。ペンギン砲と城崩れに使う</summary>
        private static AudioClip CreateRumble(string name, float duration, float baseFrequency, float decay)
        {
            var random = new System.Random(2);
            float filtered = 0f;
            return CreateClip(name, duration, t =>
            {
                // 一次ローパスでノイズの刺さりを抑え、地響きに近づける（サンプル順に呼ばれる前提）
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.08f);
                float tone = Mathf.Sin(2f * Mathf.PI * baseFrequency * t);
                float envelope = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * decay) * (1f - t / duration);
                return (tone * 0.5f + filtered * 1.5f) * envelope;
            });
        }

        /// <summary>指定した音程を順に鳴らす短いジングル。矩形波を各音の頭だけ強めて、ゲームらしい歯切れのよい音色にする</summary>
        private static AudioClip CreateNotes(string name, float[] frequencies, float noteDuration)
        {
            return CreateClip(name, noteDuration * frequencies.Length, t =>
            {
                int note = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float local = t - note * noteDuration;
                float wave = Mathf.Sin(2f * Mathf.PI * frequencies[note] * t);
                return Mathf.Sign(wave) * 0.2f * Mathf.Exp(-local * 10f) + wave * 0.3f;
            });
        }

        private static AudioClip CreateClip(string name, float duration, Func<float, float> wave)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++) data[i] = Mathf.Clamp(wave(i / (float)SampleRate), -1f, 1f);

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
