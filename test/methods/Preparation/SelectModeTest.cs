using CalcsheetGenerator;

using CalcsheetGenerator.Enum;

namespace Test.Methods
{
    [Collection("コンソールテストケース")]
    public class SelectModeTest
    {
        readonly Preparation _Preparation = Preparation.GetInstance();

        [Fact(DisplayName="モード指定Autoでオートモードの設定になっていること")]
        public void NomalAuto()
        {
            var input = new StringReader("a");
            Console.SetIn(input);
            _Preparation.SelectMode();
            Assert.Equal(Mode.Auto, _Preparation._Mode);
        }

        [Fact(DisplayName="モード指定Manualでマニュアルモードの設定になっていること")]
        public void NomalManual()
        {
            var input = new StringReader("m");
            Console.SetIn(input);
            _Preparation.SelectMode();
            Assert.Equal(Mode.Manual, _Preparation._Mode);
        }

        [Fact(DisplayName="モード指定なしでFormatExceptionを返すこと")]
        public void ErrorOtherInput()
        {
            var input = new StringReader("");
            Console.SetIn(input);
            var exception = Assert.Throws<FormatException>(() => 
                _Preparation.SelectMode());
            Assert.NotEmpty(exception.Message);
        }
    }
}

