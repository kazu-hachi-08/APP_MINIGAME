using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 選べるキャラの一覧。GolfPlayerSlot にはアセットではなくこの一覧の番号だけを持たせる。
    /// オンライン対戦で番号だけ送れば全端末で同じキャラになるようにするため（モルックの MolkkyCharacterCatalog と同じ）。
    /// 先頭をバランス型にしておき、初期選択として使う。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfCharacterCatalog", menuName = "MiniGame/Golf/Character Catalog")]
    public class GolfCharacterCatalog : ScriptableObject
    {
        [SerializeField] private GolfCharacterData[] _characters;

        public int Count => _characters.Length;

        /// <summary>範囲外の番号はクランプする。相手端末から壊れた番号が届いても落ちないようにするため</summary>
        public GolfCharacterData Get(int index)
        {
            return _characters[Mathf.Clamp(index, 0, _characters.Length - 1)];
        }
    }
}
