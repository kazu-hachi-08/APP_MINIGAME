using System;
using UnityEngine;

namespace MiniGame.Common.Audio
{
    /// <summary>
    /// BGM識別子
    /// </summary>
    public enum BgmId
    {
        None = 0,
        TitleBgm = 1,
        GameBgm01 = 2,
        GameBgm02 = 3,
        ResultBgm = 4
    }

    /// <summary>
    /// SE識別子
    /// </summary>
    public enum SeId
    {
        None = 0,
        ButtonClick = 1,
        ButtonCancel = 2,
        Whistle = 3,
        Kick = 4,
        GoalCheer = 5,
        GameOver = 6,
        GameClear = 7,
        CountDown = 8
    }

    [Serializable]
    public struct BgmData
    {
        public BgmId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [Serializable]
    public struct SeData
    {
        public SeId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }
}
