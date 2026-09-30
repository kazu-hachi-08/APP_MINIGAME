using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 選べるキャラの一覧。席にはアセットではなくこの一覧の番号だけを持たせる
    /// （フェーズ5のオンラインで番号だけ送れば済むようにするため。モルックと同じ）。
    /// </summary>
    [CreateAssetMenu(fileName = "LifeCharacterCatalog", menuName = "MiniGame/LifeGame/Character Catalog")]
    public class LifeCharacterCatalog : ScriptableObject
    {
        [SerializeField] private LifeCharacterData[] _characters;

        public int Count => _characters.Length;

        /// <summary>範囲外の番号はクランプする。相手端末から壊れた番号が届いても落ちないようにするため</summary>
        public LifeCharacterData Get(int index)
        {
            return _characters[Mathf.Clamp(index, 0, _characters.Length - 1)];
        }
    }
}
