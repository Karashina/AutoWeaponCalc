using System;
using System.IO;

using CalcsheetGenerator;

namespace Methods
{
    [Collection("コンソールテストケース")]
    public class StartupAutomodeTest
    {
        [Fact(DisplayName="正常パターン")]
        public void Nomal()
        {
            var data = String.Join(Environment.NewLine, new[]
            {
                "Jean",
                "sword"
            });
            var input = new StringReader(data);
            Console.SetIn(input);

            string[] expectOutput = new String[] {"Jean", "sword", "0", "y"};
            Assert.Equal(expectOutput, AWC.Startup_automode());
        }

        [Theory(DisplayName="未入力が含まれるパターンで強制終了していること")]
        [InlineData("", "sword")]
        [InlineData("Jean", "")]
        [InlineData("", "")]
        public void ErrorInputContainsEmpty(string charactorName, string weaponType)
        {
            var input = new StringReader($"{charactorName}\n{weaponType}\n");
            Console.SetIn(input);

            var enviromnent = new Mock<_Environment>();
            _Environment.Current = enviromnent.Object;
            AWC.Startup_automode();
            enviromnent.Verify(mock => mock.Exit(0), Times.Once);
        }
    }
}

