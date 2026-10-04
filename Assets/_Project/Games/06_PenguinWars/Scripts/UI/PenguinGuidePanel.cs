using System;
using MiniGame.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// タイトルから開く「あそびかた」。上に説明カード、下にページ送りを出し、間に見える本物の戦場でデモを流す（仕様書 §2.0）。
    /// 戦場を見せるためタイトルの幕は閉じて開くので、閉じたら onClosed でタイトルに戻してもらう
    /// </summary>
    public class PenguinGuidePanel : MonoBehaviour
    {
        [SerializeField] private GuideDemoDirector _director;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _bodyLabel;
        [SerializeField] private Text _pageLabel;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _closeButton;

        private int _page;
        private Action _onClosed;

        private void Awake()
        {
            _prevButton.onClick.AddListener(() => Turn(-1));
            _nextButton.onClick.AddListener(() => Turn(1));
            _closeButton.onClick.AddListener(Close);
        }

        public void Show(Action onClosed)
        {
            _onClosed = onClosed;
            _page = 0;
            gameObject.SetActive(true);
            // デモはすべて左の城のまわりで起こるので、タイトルでスクロールされていても左端に戻す
            _battleCamera.LookAt(float.MinValue);
            ShowPage();
        }

        private void Turn(int step)
        {
            PlayClick();
            int count = GuideTopics.All.Count;
            // 端で止めずに一周させる（最後まで見たらそのまま最初に戻れるように）
            _page = (_page + step + count) % count;
            ShowPage();
        }

        private void ShowPage()
        {
            GuideTopic topic = GuideTopics.All[_page];
            _titleLabel.text = topic.Title;
            _bodyLabel.text = topic.Body;
            _pageLabel.text = $"{_page + 1} / {GuideTopics.All.Count}";
            _director.Play(topic);
        }

        private void Close()
        {
            PlayClick();
            _director.Stop();
            gameObject.SetActive(false);
            _onClosed?.Invoke();
        }

        private static void PlayClick()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
