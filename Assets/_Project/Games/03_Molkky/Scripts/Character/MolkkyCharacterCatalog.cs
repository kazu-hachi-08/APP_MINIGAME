using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 選べるキャラの一覧。PlayerSlot にはアセットではなくこの一覧の番号だけを持たせる。
    /// オンライン対戦（Phase C5）で番号だけ送れば全端末で同じキャラになるようにするため（卓球の LoadoutCatalog と同じ考え方）。
    /// 先頭をバランス型にしておき、初期選択として使う。
    /// </summary>
    [CreateAssetMenu(fileName = "MolkkyCharacterCatalog", menuName = "MiniGame/Molkky/Character Catalog")]
    public class MolkkyCharacterCatalog : ScriptableObject
    {
        [SerializeField] private MolkkyCharacterData[] _characters;

        public int Count => _characters.Length;

        public MolkkyCharacterData Default => _characters[0];

        /// <summary>範囲外の番号はクランプする。相手端末から壊れた番号が届いても落ちないようにするため</summary>
        public MolkkyCharacterData Get(int index)
        {
            return _characters[Mathf.Clamp(index, 0, _characters.Length - 1)];
        }

        public int IndexOf(MolkkyCharacterData character)
        {
            return System.Array.IndexOf(_characters, character);
        }
    }
}
