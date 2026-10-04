using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>出撃ボタン1つ分の再生産（仕様書 §4.3）。ボタンのゲージはこの値を読んで描く</summary>
    public class DeckSlotState
    {
        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public bool IsReady => Remaining <= 0f;
        /// <summary>残りの割合（1=出した直後、0=出せる）</summary>
        public float RemainingRatio => Duration > 0f ? Remaining / Duration : 0f;

        public void StartCooldown(float duration)
        {
            Duration = duration;
            Remaining = duration;
        }

        public void Tick(float deltaTime)
        {
            Remaining = Math.Max(0f, Remaining - deltaTime);
        }
    }
}
