using System;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 一人用のステージモード: ステージ選択 → 詳細 → 編成発表 → プレイ → ★判定・保存 → リザルト。
    /// リザルトからの「つぎのステージ」「もういちど」「ステージ選択」はどれもシーンを読み直して行き先だけ static で渡す
    /// （試合の後片付けを書かずに済むため。リトライと同じ方式）
    /// </summary>
    public partial class PenguinWarsGameManager
    {
        private const string StageClearTitle = "STAGE CLEAR!";
        private const string StageFailedTitle = "GAME OVER";

        // シーンを読み直すとインスタンスの値は消えるので static に置く。
        // s_restartStageId: このステージの編成発表からすぐ始める / s_openStageSelect: ステージ選択から始める
        private static string s_restartStageId;
        private static bool s_openStageSelect;

        [Header("ステージモード")]
        [SerializeField] private StageSelectPanel _stageSelectPanel;
        [SerializeField] private StageResultPanel _stageResultPanel;

        private CampaignProgress _progress;
        private StageDefinition _currentStage;

        /// <summary>リザルトから読み直してきたなら、タイトルを飛ばしてその続きから始める</summary>
        private bool TryResumeStageFlow()
        {
            string restartStageId = s_restartStageId;
            bool openStageSelect = s_openStageSelect;
            s_restartStageId = null;
            s_openStageSelect = false;

            if (restartStageId != null)
            {
                StartStage(restartStageId);
                return true;
            }
            if (!openStageSelect) return false;

            // タイトルを通らないので、タイトルの曲をここで流す
            _audio.PlayTitleBgm();
            ShowStageSelect();
            return true;
        }

        private void ShowStageSelect()
        {
            Phase = PenguinWarsPhase.StageSelect;
            _stageSelectPanel.Show(_progress, StartStage, ShowModeSelect);
        }

        private void StartStage(string stageId)
        {
            _currentStage = StageDefinitions.Find(stageId);
            if (_currentStage == null)
            {
                Debug.LogWarning($"[PenguinWarsGameManager] ステージ {stageId} がありません。最初のステージで始めます");
                _currentStage = StageDefinitions.All[0];
            }
            _progress.LastPlayedId = _currentStage.Id;
            // リザルトの「つぎのステージ」から直接遊んだステージに、後でステージ選択を開いたとき鍵の演出を出さない
            _progress.MarkUnlockRevealed(_currentStage.Id);
            CampaignSave.Save(_progress);

            // 編成画面で決めた編成（未保存・未解放が混ざっていたら補完したもの）
            _battleRunner.InitializeStage(_currentStage, DeckRules.CurrentDeck(_progress));
            BeginIntro(_deckIntroDuration);
        }

        private void EndStage()
        {
            StageResult result = StageResult.From(_battleRunner.World);
            if (result.Cleared) ShowStageClear(result);
            else ShowStageFailed(result);
        }

        private void ShowStageClear(StageResult result)
        {
            StarFlags stars = StarRule.Evaluate(_currentStage, result);
            StageRecordChange change = _progress.Record(_currentStage.Id, stars, result.ElapsedSeconds);
            CampaignSave.Save(_progress);

            string detail = $"クリア {StageLabels.TimeText(result.ElapsedSeconds)}\n撃破 {result.KillCount}体";
            if (change.NewStars != StarFlags.None) detail += $"\n\n新しい★\n{StageLabels.NewStars(_currentStage, change.NewStars)}";

            StageDefinition next = StageDefinitions.Next(_currentStage.Id);
            Action onNext = next != null ? () => ReloadInto(next.Id) : (Action)null;
            ShowStageResult(true, new StageResultContent
            {
                Title = StageClearTitle,
                Stars = stars,
                NewStars = change.NewStars,
                Detail = detail,
                IsNewRecord = change.IsNewBest,
                UnlockNos = change.NewUnlockNos,
            }, onNext);
        }

        private void ShowStageFailed(StageResult result)
        {
            ShowStageResult(false, new StageResultContent { Title = StageFailedTitle, Detail = $"撃破 {result.KillCount}体" }, null);
        }

        /// <summary>BaseMiniGameManager.FinishGame と同じ状態の進め方をして、表示だけ専用のリザルトにする</summary>
        private void ShowStageResult(bool isClear, StageResultContent content, Action onNext)
        {
            ChangeState(MiniGameState.GameOver);
            // クリアは城が崩れ始めたときに勝利のジングルを鳴らしている（BattleEventPresenter）ので、ここでは重ねない
            if (!isClear && AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.GameOver);
            OnGameOver(isClear);
            ChangeState(MiniGameState.Result);
            content.Detail = $"{StageLabels.Title(_currentStage)}\n{content.Detail}";
            _stageResultPanel.Show(content, onNext, RestartGame, ReloadIntoStageSelect);
        }

        private void ReloadInto(string stageId)
        {
            s_restartStageId = stageId;
            base.RestartGame();
        }

        private void ReloadIntoStageSelect()
        {
            s_openStageSelect = true;
            base.RestartGame();
        }
    }
}
