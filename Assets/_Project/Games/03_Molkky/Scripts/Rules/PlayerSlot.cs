namespace MiniGame.Molkky
{
    /// <summary>
    /// 1席分の名前・人間/NPC・キャラ。点数は TeamScore が持つ（チーム戦で得点を共有するため）。
    /// </summary>
    public class PlayerSlot
    {
        public string Name { get; }
        public PlayerKind Kind { get; }

        /// <summary>MolkkyCharacterCatalog の番号。見た目・能力はカタログから引く（オンラインで番号だけ送れるようにするため）</summary>
        public int CharacterIndex { get; }

        public bool IsNpc => Kind != PlayerKind.Human;

        public PlayerSlot(string name, PlayerKind kind = PlayerKind.Human, int characterIndex = 0)
        {
            Name = name;
            Kind = kind;
            CharacterIndex = characterIndex;
        }
    }
}
