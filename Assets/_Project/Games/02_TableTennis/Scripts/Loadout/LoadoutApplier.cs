using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 選んだ選手・ラケットを、各コンポーネントの倍率と見た目へ配る。
    /// 能力の反映先をここに集めることで、GameManager は「誰に何を選んだか」だけを渡せばよくなる。
    /// </summary>
    public class LoadoutApplier : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private RacketController _playerRacket;
        [SerializeField] private PlayerSwing _playerSwing;
        [SerializeField] private ShotCalculator _shotCalculator;
        [SerializeField] private CharacterView _playerCharacter;

        [Header("Opponent")]
        [SerializeField] private NpcController _npc;
        [SerializeField] private NpcRacketView _opponentRacketView;
        [SerializeField] private CharacterView _opponentCharacter;

        /// <summary>自分の選手・ラケットを能力と見た目の両方に反映する</summary>
        public void ApplyPlayer(Loadout loadout)
        {
            CharacterData character = loadout.Character;
            RacketData racket = loadout.Racket;

            _playerRacket.SetFollowSpeedMultiplier(character.MoveSpeedMultiplier);
            _playerSwing.SetCharacterMultipliers(character.ReachMultiplier, character.SwingDurationMultiplier);
            _shotCalculator.SetRacketMultipliers(racket.SpeedMultiplier, racket.SpinMultiplier, racket.ErrorMultiplier);

            _playerCharacter.SetSprite(character.BackSprite);
            _playerRacket.SetSprite(racket.Sprite);
        }

        /// <summary>NPCの選手・ラケットを能力と見た目の両方に反映する</summary>
        public void ApplyNpc(Loadout loadout)
        {
            _npc.SetLoadoutMultipliers(
                loadout.Character.MoveSpeedMultiplier,
                loadout.Character.ReachMultiplier,
                loadout.Racket.SpeedMultiplier,
                loadout.Racket.SpinMultiplier);

            ApplyOpponentLook(loadout);
        }

        /// <summary>
        /// 相手の見た目だけを反映する。オンライン対戦では相手の能力は相手端末で計算済みのため、見た目だけでよい
        /// </summary>
        public void ApplyOpponentLook(Loadout loadout)
        {
            _opponentCharacter.SetSprite(loadout.Character.FrontSprite);
            _opponentRacketView.SetSprite(loadout.Racket.Sprite);
        }
    }
}
