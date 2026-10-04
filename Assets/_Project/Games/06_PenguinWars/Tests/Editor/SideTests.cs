using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    public class SideTests
    {
        [Test]
        public void Forward_LeftAdvancesRight_RightAdvancesLeft()
        {
            Assert.AreEqual(1, Side.Left.Forward());
            Assert.AreEqual(-1, Side.Right.Forward());
        }

        [Test]
        public void Opponent_ReturnsOtherSide()
        {
            Assert.AreEqual(Side.Right, Side.Left.Opponent());
            Assert.AreEqual(Side.Left, Side.Right.Opponent());
        }
    }
}
