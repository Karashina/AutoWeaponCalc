using CalcsheetGenerator;
using CalcsheetGenerator.Common;

namespace Test.Methods
{
    [Collection("コンソールテストケース")]
    public class StartupTest
    {

        readonly Preparation _Preparation = Preparation.GetInstance();

        public StartupTest()
        {
            Console.SetIn(new StringReader("a\n"));
            _Preparation.SelectMode();
        }

        [Fact(DisplayName="正常パターン")]
        public void Nomal()
        {
            _Preparation.Startup();
            var data = String.Join(Environment.NewLine, new[]
            {
                "Jean",
                "sword"
            });
            var input = new StringReader(data);
            Console.SetIn(input);

            UserInput expectOutput = new UserInput("Jean", "sword", "0", "y");
            Assert.Equivalent(expectOutput, _Preparation.Startup());
        }

        [Fact(DisplayName="未入力が含まれるパターンの場合")]
        public void ErrorInputContainsEmpty()
        {
            var input = new StringReader("\n\n");
            Console.SetIn(input);
            UserInput expectOutput = new UserInput("", "", "0", "y");
            Assert.Equivalent(expectOutput,  _Preparation.Startup());
        }
    }
}

