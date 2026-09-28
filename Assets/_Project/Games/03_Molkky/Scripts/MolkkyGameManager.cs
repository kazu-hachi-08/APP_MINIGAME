using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Core;
using MiniGame.Common.Online;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 手番と進行（MolkkyPhase）を回す。得点ルールは MolkkyRules、物理は PinRack / StickThrower、
    /// NPCの狙いは NpcThrower、オンラインの送受信は MolkkyOnlineLink に任せ、ここは「いつ何を呼ぶか」だけを持つ。
    /// </summary>
    public class MolkkyGameManager : BaseMiniGameManager
    {
        private const string LocalPlayerName = "あなた";
        private const string SurvivorVictoryLine = "最後まで残った！";
        private const int NotSelected = -1;

        [Header("Molkky")]
        [SerializeField] private PinRack _pinRack;
        [SerializeField] private StickThrower _stick;
        [SerializeField] private ThrowInput _input;
        [SerializeField] private NpcThrower _npc;
        [SerializeField] private ThrowSettleWatcher _settleWatcher;
        [SerializeField] private ScoreBoardView _scoreBoard;
        [SerializeField] private ScorePopupView _scorePopup;
        [SerializeField] private MolkkyAudio _audio;
        [SerializeField] private TurnBannerView _turnBanner;
        [SerializeField] private PlayerSetupPanel _setupPanel;
        [SerializeField] private ThrowStyleButton _styleButton;
        [SerializeField] private ThrowArcButton _arcButton;

        [Header("Character")]
        [SerializeField] private MolkkyCharacterCatalog _characterCatalog;
        [SerializeField] private CharacterSelectPanel _characterSelectPanel;
        [SerializeField] private ThrowerView _throwerView;
        [SerializeField] private VictoryShowView _victoryShow;

        [Header("Online (§19)")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private MolkkyOnlineLink _onlineLink;

        [Header("Timing (sec)")]
        [SerializeField] private float _npcBannerDuration = 0.8f;
        [Tooltip("オンライン対戦の「○○の番」表示時間。端末を回さないのでタップ待ちにしない")]
        [SerializeField] private float _onlineBannerDuration = 0.8f;
        [Tooltip("NPCが投げる前の間。考えている感じを出す（§9.5）")]
        [SerializeField] private float _npcThinkTime = 0.8f;
        [SerializeField] private float _scoreDisplayDuration = 1.2f;
        [Tooltip("50点ちょうどの演出を見せてから結果画面を出すまでの時間。通常の得点より長く余韻を残す")]
        [SerializeField] private float _winDisplayDuration = 2.2f;
        [SerializeField] private float _pinResetDuration = 0.5f;

        private readonly List<PlayerSlot> _players = new List<PlayerSlot>();
        private int _currentIndex;

        private bool _isOnline;
        private int _localIndex;

        /// <summary>オンラインで各席が選んだキャラ番号。まだ届いていない席は NotSelected</summary>
        private int[] _onlineCharacters;

        // 相手端末から届いた投擲・結果。自分の画面がまだ前の手番の演出中でも取りこぼさないよう、
        // 届いた時点では溜めておき、相手の手番の処理で古い順に取り出す。
        // 3人以上だと自分の演出中に次の人の投擲まで届くことがあるため、1件ではなくキューにしている
        private readonly Queue<(ThrowRequest Request, PinState[] StartStates)> _remoteThrows =
            new Queue<(ThrowRequest Request, PinState[] StartStates)>();
        private readonly Queue<PinState[]> _remoteResults = new Queue<PinState[]>();

        public MolkkyPhase Phase { get; private set; }

        private PlayerSlot CurrentPlayer => _players[_currentIndex];

        private MolkkyCharacterData CurrentCharacter => _characterCatalog.Get(CurrentPlayer.CharacterIndex);

        private bool IsRemoteTurn => _isOnline && _currentIndex != _localIndex;

        protected override void OnGameReady()
        {
            _input.ThrowRequested += HandleThrowRequested;
            _input.PositionChanged += HandlePositionChanged;
            _input.StyleChanged += HandleStyleChanged;
            _settleWatcher.Settled += HandleSettled;
            SubscribeOnline();

            Phase = MolkkyPhase.PlayerSetup;
            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.Show(ShowPlayerSetup, HandleOnlineStarted, PlayerSetupPanel.MaxPlayers);
            }
            else
            {
                ShowPlayerSetup();
            }
        }

        private void ShowPlayerSetup()
        {
            Phase = MolkkyPhase.PlayerSetup;
            _setupPanel.Show(HandlePlayersConfirmed);
        }

        private void HandlePlayersConfirmed(IReadOnlyList<PlayerKind> kinds)
        {
            Phase = MolkkyPhase.CharacterSelect;
            _characterSelectPanel.Show(kinds, characters => HandleCharactersConfirmed(kinds, characters), ShowPlayerSetup);
        }

        /// <summary>
        /// 名前は「P1 パワー型」のように席番号＋キャラ名にする。同じキャラを複数人が選べるので、
        /// キャラ名だけだと誰か分からなくなるため
        /// </summary>
        private void HandleCharactersConfirmed(IReadOnlyList<PlayerKind> kinds, IReadOnlyList<int> characters)
        {
            _players.Clear();
            for (int i = 0; i < kinds.Count; i++)
            {
                string name = $"P{i + 1} {_characterCatalog.Get(characters[i]).DisplayName}";
                _players.Add(new PlayerSlot(name, kinds[i], characters[i]));
            }

            StartGame();
        }

        /// <summary>
        /// オンラインは部屋に集まった人数で遊ぶので人数設定は出さない。席番号＝手番の順で、先攻はホスト（§19.4）。
        /// 各端末で自分のキャラだけを選び、全席のキャラ番号が揃ったら始める（仕様書 §20.6）
        /// </summary>
        private void HandleOnlineStarted(int localSeat, int playerCount)
        {
            _isOnline = true;
            _localIndex = localSeat;
            _onlineCharacters = new int[playerCount];
            System.Array.Fill(_onlineCharacters, NotSelected);
            // 受信ハンドラを登録する前に Phase を変えておき、届いたキャラ番号を取りこぼさないようにする
            Phase = MolkkyPhase.CharacterSelect;
            _onlineLink.Begin();

            _characterSelectPanel.ShowOnline(localSeat, playerCount, HandleLocalCharacterConfirmed);
        }

        private void HandleLocalCharacterConfirmed(int characterIndex)
        {
            _onlineLink.SendCharacter(_localIndex, characterIndex);
            SetOnlineCharacter(_localIndex, characterIndex);
        }

        private void HandleRemoteCharacter(int seat, int characterIndex)
        {
            if (!_isOnline || Phase != MolkkyPhase.CharacterSelect) return;
            if (seat < 0 || seat >= _onlineCharacters.Length) return;

            SetOnlineCharacter(seat, characterIndex);
        }

        /// <summary>範囲外の番号はここでクランプする。NotSelected と区別できなくなって待ち続けるのを防ぐため</summary>
        private void SetOnlineCharacter(int seat, int characterIndex)
        {
            _onlineCharacters[seat] = Mathf.Clamp(characterIndex, 0, _characterCatalog.Count - 1);
            if (System.Array.IndexOf(_onlineCharacters, NotSelected) >= 0) return;

            StartOnlineGame();
        }

        /// <summary>
        /// 名前は端末ごとに自分だけ「あなた」にする。名前は表示にしか使わないので端末間で違っていてよい
        /// </summary>
        private void StartOnlineGame()
        {
            _characterSelectPanel.Hide();

            _players.Clear();
            for (int i = 0; i < _onlineCharacters.Length; i++)
            {
                string owner = i == _localIndex ? LocalPlayerName : $"P{i + 1}";
                string name = $"{owner} {_characterCatalog.Get(_onlineCharacters[i]).DisplayName}";
                _players.Add(new PlayerSlot(name, PlayerKind.Human, _onlineCharacters[i]));
            }

            StartGame();
        }

        protected override void OnGameStart()
        {
            _currentIndex = 0;
            StartCoroutine(TurnStartRoutine());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_input != null)
            {
                _input.ThrowRequested -= HandleThrowRequested;
                _input.PositionChanged -= HandlePositionChanged;
                _input.StyleChanged -= HandleStyleChanged;
            }

            if (_settleWatcher != null) _settleWatcher.Settled -= HandleSettled;
            UnsubscribeOnline();
        }

        private void SubscribeOnline()
        {
            if (_onlineLink != null)
            {
                _onlineLink.OnThrowReceived += HandleRemoteThrow;
                _onlineLink.OnResultReceived += HandleRemoteResult;
                _onlineLink.OnCharacterReceived += HandleRemoteCharacter;
            }

            if (_onlineSession != null) _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
        }

        private void UnsubscribeOnline()
        {
            if (_onlineLink != null)
            {
                _onlineLink.OnThrowReceived -= HandleRemoteThrow;
                _onlineLink.OnResultReceived -= HandleRemoteResult;
                _onlineLink.OnCharacterReceived -= HandleRemoteCharacter;
            }

            if (_onlineSession != null) _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
        }

        /// <summary>
        /// オンラインでは相手の端末は止まらないため、時間は止めずに自分の投擲受付だけ止める（§19.4）。
        /// PAUSE中は IsPlaying が false になり、HandleThrowRequested で投擲が弾かれる
        /// </summary>
        public override void PauseGame()
        {
            base.PauseGame();

            if (_isOnline) Time.timeScale = 1f;
        }

        private IEnumerator TurnStartRoutine()
        {
            Phase = MolkkyPhase.TurnStart;
            _scoreBoard.Show(_players, _currentIndex);
            ApplyCurrentCharacter();
            // 前の人の投げ方を引き継ぐと気づかず違う投げ方をしてしまうので、毎手番 横・低め（初期値）に戻す
            _input.SetStyle(ThrowStyle.Horizontal);
            _input.SetArc(ThrowArc.Low);

            yield return PlayTurnBanner();

            Phase = MolkkyPhase.Aiming;
            if (CurrentPlayer.IsNpc)
            {
                yield return NpcThrowRoutine();
            }
            else if (IsRemoteTurn)
            {
                yield return RemoteThrowRoutine();
            }
            else
            {
                _input.IsAccepting = true;
                SetThrowButtonsVisible(true);
            }
        }

        /// <summary>
        /// 人間・NPC・相手端末のどの手番でも適用する。棒の長さは当たり判定なので、
        /// 相手の手番でも揃えておかないと再生で倒れるピンが相手端末とずれる
        /// </summary>
        private void ApplyCurrentCharacter()
        {
            MolkkyCharacterData character = CurrentCharacter;
            _throwerView.SetCharacter(character);
            _stick.SetCharacter(character);
            _input.SetCharacter(character);
        }

        /// <summary>1台を回すときだけ人間の番をタップ待ちにする。NPCとオンラインは端末を渡さないので自動で閉じる</summary>
        private IEnumerator PlayTurnBanner()
        {
            bool waitForTap = !CurrentPlayer.IsNpc && !_isOnline;
            float duration = _isOnline ? _onlineBannerDuration : _npcBannerDuration;
            yield return _turnBanner.Play($"{CurrentPlayer.Name} の番", MolkkyPlayerColors.Get(_currentIndex),
                waitForTap, duration);
        }

        private IEnumerator NpcThrowRoutine()
        {
            ThrowRequest request = _npc.CreateRequest(CurrentPlayer, CurrentCharacter);
            _stick.SetStyle(request.Style);
            _stick.PlaceOnLine(request.PositionX);

            yield return new WaitForSeconds(_npcThinkTime);

            ExecuteThrow(request);
        }

        /// <summary>
        /// 相手の手番：届いた ThrowRequest で自分の端末でも物理を動かして見せ、
        /// 相手端末の結果が届いたらピンを上書きして採点する（§19.2 / 案1）。
        /// 投げる前に相手端末のピン配置へ揃えるのは、配置が少しでもずれていると再生で倒れるピンが変わるため
        /// </summary>
        private IEnumerator RemoteThrowRoutine()
        {
            yield return new WaitUntil(() => _remoteThrows.Count > 0);
            (ThrowRequest request, PinState[] startStates) = _remoteThrows.Dequeue();
            _pinRack.ApplyStates(startStates);
            ExecuteThrow(request);

            yield return new WaitUntil(() => _remoteResults.Count > 0);
            _settleWatcher.Cancel();
            _stick.Freeze();
            _pinRack.ApplyStates(_remoteResults.Dequeue());

            yield return ScoringRoutine();
        }

        private void HandleRemoteThrow(ThrowRequest request, PinState[] startStates)
        {
            _remoteThrows.Enqueue((request, startStates));
        }

        private void HandleRemoteResult(PinState[] states)
        {
            _remoteResults.Enqueue(states);
        }

        private void HandlePositionChanged(float x)
        {
            _stick.PlaceOnLine(x);
        }

        private void HandleStyleChanged(ThrowStyle style)
        {
            _stick.SetStyle(style);
        }

        private void HandleThrowRequested(ThrowRequest request)
        {
            if (Phase != MolkkyPhase.Aiming || !IsPlaying || CurrentPlayer.IsNpc || IsRemoteTurn) return;

            _input.IsAccepting = false;
            SetThrowButtonsVisible(false);
            if (_isOnline) SendThrowWithStartStates(request);
            ExecuteThrow(request);
        }

        /// <summary>
        /// 送ったのと同じ状態を自分にも適用し直す。速度などピン状態に含まれない差を消して、
        /// 相手端末と全く同じ条件から物理を始めるため
        /// </summary>
        private void SendThrowWithStartStates(ThrowRequest request)
        {
            PinState[] startStates = _pinRack.CaptureStates();
            _pinRack.ApplyStates(startStates);
            _onlineLink.SendThrow(request, startStates);
        }

        private void SetThrowButtonsVisible(bool visible)
        {
            _styleButton.SetVisible(visible);
            _arcButton.SetVisible(visible);
        }

        private void ExecuteThrow(ThrowRequest request)
        {
            Phase = MolkkyPhase.Throwing;

            _pinRack.ArmAll();
            _stick.Throw(request);
            _audio.PlayThrow(request);
            _settleWatcher.Begin();
        }

        private void HandleSettled()
        {
            // 相手の投擲は相手端末の結果で採点するので、自分の端末の物理が止まっても進めない
            if (IsRemoteTurn) return;

            if (_isOnline) SendAndFreezeResult();
            StartCoroutine(ScoringRoutine());
        }

        /// <summary>
        /// 送った瞬間の状態で自分の画面も止める。止めないと採点表示中もピンや棒が滑り続け、
        /// 相手端末とピン配置がずれたまま立て直し → 次の投擲へ持ち越されるため
        /// </summary>
        private void SendAndFreezeResult()
        {
            PinState[] states = _pinRack.CaptureStates();
            _onlineLink.SendResult(states);
            _stick.Freeze();
            _pinRack.ApplyStates(states);
        }

        private IEnumerator ScoringRoutine()
        {
            Phase = MolkkyPhase.Scoring;
            _pinRack.DisarmAll();

            List<int> fallen = _pinRack.CollectFallenNumbers();
            ThrowResult result = MolkkyRules.ApplyThrow(CurrentPlayer, fallen);
            Debug.Log($"[Molkky] {CurrentPlayer.Name}: 倒れたピン [{string.Join(", ", fallen)}] → {result.Outcome} +{result.Points} (合計 {CurrentPlayer.Score})");

            _scoreBoard.Show(_players, _currentIndex);
            PlayResultEffect(result);

            bool isWin = result.Outcome == ThrowOutcome.Win;
            yield return new WaitForSeconds(isWin ? _winDisplayDuration : _scoreDisplayDuration);

            PlayerSlot winner = FindWinner(result);
            if (winner != null)
            {
                yield return FinishRoutine(winner, result);
                yield break;
            }

            Phase = MolkkyPhase.PinReset;
            _scorePopup.Hide();
            _pinRack.StandUpFallen();
            if (fallen.Count > 0) _audio.PlayPinReset();
            // 棒がピンの間に残っていると立て直したピンを押してしまうので、先に投擲ラインへ戻す
            _input.ResetPosition(0f);

            yield return new WaitForSeconds(_pinResetDuration);

            _currentIndex = MolkkyRules.NextPlayerIndex(_players, _currentIndex);
            yield return TurnStartRoutine();
        }

        /// <summary>得点ポップアップ・音・スコア枠の揺れで、1投の結果を伝える（§14 Phase 7）</summary>
        private void PlayResultEffect(ThrowResult result)
        {
            _scorePopup.ShowResult(result);
            _audio.PlayResult(result.Outcome);

            if (result.Outcome == ThrowOutcome.OverTo25 || result.Outcome == ThrowOutcome.Disqualified)
            {
                _scoreBoard.Shake(_currentIndex);
            }
        }

        /// <summary>50点ちょうど、または残り1人なら勝者を返す。まだ続くなら null</summary>
        private PlayerSlot FindWinner(ThrowResult result)
        {
            return result.Outcome == ThrowOutcome.Win
                ? CurrentPlayer
                : MolkkyRules.FindSoleSurvivor(_players);
        }

        /// <summary>勝ったキャラの勝利演出を見せてから結果画面を出す（仕様書 §20.5）</summary>
        private IEnumerator FinishRoutine(PlayerSlot winner, ThrowResult result)
        {
            Phase = MolkkyPhase.GameSet;
            _scorePopup.Hide();

            bool exactWin = result.Outcome == ThrowOutcome.Win;
            MolkkyCharacterData character = _characterCatalog.Get(winner.CharacterIndex);
            // 失格で勝ったときに「ぴったり50点！」と言わせないよう、キャラ別のセリフは50点ちょうどのときだけ使う
            string line = exactWin ? character.VictoryLine : SurvivorVictoryLine;
            int winnerIndex = _players.IndexOf(winner);
            _audio.PlayVictory();
            yield return _victoryShow.Play(character, MolkkyPlayerColors.Get(winnerIndex), winner.Name, line);

            string detail = exactWin ? "50点ちょうど！" : "他のプレイヤーが失格";
            FinishGame(IsLocalVictory(winner), $"{winner.Name} の勝ち", detail);
        }

        /// <summary>オンラインは自分が勝ったか、1台プレイはNPCが勝ったら人間側の負けとして GAME OVER を出す</summary>
        private bool IsLocalVictory(PlayerSlot winner)
        {
            if (_isOnline) return _players.IndexOf(winner) == _localIndex;

            return !winner.IsNpc;
        }

        /// <summary>試合中に誰か1人でも切れたら全員その時点で終了する（§19.4）。再接続はしない（§19.5）</summary>
        private void HandlePeerDisconnected()
        {
            if (!_isOnline || Phase == MolkkyPhase.GameSet) return;

            StopAllCoroutines();
            Phase = MolkkyPhase.GameSet;
            _input.IsAccepting = false;
            SetThrowButtonsVisible(false);
            _scorePopup.Hide();
            // キャラ選択・待機中に切れたときも、選択パネルを残したまま結果画面を出さないようにする
            _characterSelectPanel.Hide();

            FinishGame(false, "他のプレイヤーとの接続が切れました", "試合を終了しました");
        }
    }
}
