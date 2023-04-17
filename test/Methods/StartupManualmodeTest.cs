using System;
using System.IO;

using CalcsheetGenerator;

namespace Methods
{
    public class StartupManualmodeTest
    {
        [Fact(DisplayName="正常パターン")]
        public void Nomal()
        {
            var data = String.Join(Environment.NewLine, new[]
            {
                "Jean",
                "sword",
                "0",
                "y"
            });
            var input = new StringReader(data);
            Console.SetIn(input);

            string[] expectOutput = new String[] {"Jean", "sword", "0", "y"};
            Assert.Equal(expectOutput, AWC.Startup_manualmode());
        }

        [Theory(DisplayName="未入力が含まれるパターンで強制終了していること")]
        [InlineData("", "sword", "0", "y")]
        [InlineData("Jean", "", "0", "y")]
        [InlineData("Jean", "sword", "", "y")]
        [InlineData("Jean", "sword", "0", "")]
        [InlineData("", "", "", "")]
        public void ErrorInputContainsEmpty(string charactorName, string weaponType, string refine, string modeswtich)
        {
            var data = String.Join(Environment.NewLine, new[]
            {
                charactorName,
                weaponType,
                refine,
                modeswtich
            });
            var input = new StringReader(data);
            Console.SetIn(input);

            var enviromnent = new Mock<_Environment>();
            _Environment.Current = enviromnent.Object;
            AWC.Startup_manualmode();
            enviromnent.Verify(mock => mock.Exit(0), Times.Once);
        }
    }
}

