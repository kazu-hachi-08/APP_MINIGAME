using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>選手とラケットの組み合わせ</summary>
    public struct Loadout
    {
        public CharacterData Character;
        public RacketData Racket;

        public Loadout(CharacterData character, RacketData racket)
        {
            Character = character;
            Racket = racket;
        }
    }

    /// <summary>
    /// 選べる選手・ラケットの一覧。
    /// オンライン対戦では両端末が同じ一覧を持つので、アセットそのものではなく一覧の番号だけを送る。
    /// 先頭をスタンダードにしておき、初期選択として使う。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGame/TableTennis/Loadout Catalog", fileName = "LoadoutCatalog")]
    public class LoadoutCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public RacketData[] Rackets;

        public Loadout Default => new Loadout(Characters[0], Rackets[0]);

        public Loadout Get(int characterIndex, int racketIndex)
        {
            return new Loadout(
                Characters[Mathf.Clamp(characterIndex, 0, Characters.Length - 1)],
                Rackets[Mathf.Clamp(racketIndex, 0, Rackets.Length - 1)]);
        }

        public int IndexOf(CharacterData character)
        {
            return System.Array.IndexOf(Characters, character);
        }

        public int IndexOf(RacketData racket)
        {
            return System.Array.IndexOf(Rackets, racket);
        }
    }
}
