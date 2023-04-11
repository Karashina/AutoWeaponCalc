using System;

using CalcsheetGenerator;

namespace Methods
{
    public class StartupAutoswitchTest : IDisposable
    {
        //setup
        public StartupAutoswitchTest()
        {
            var output = new StringWriter();
            Console.SetOut(output);
        }

        // teardown
        public void Dispose()
        {
            //pass
        }

        [Fact(DisplayName="モード指定Autoでtrueを返すこと")]
        public void NomalAuto()
        {
            var input = new StringReader("a");
            Console.SetIn(input);
            Assert.True(AWC.Startup_autoswitch());
        }

        [Fact(DisplayName="モード指定Manualでfalseを返すこと")]
        public void NomalManual()
        {
            var input = new StringReader("m");
            Console.SetIn(input);
            Assert.False(AWC.Startup_autoswitch());
        }

        [Fact(DisplayName="モード指定なしでfalseを返すこと")]
        public void NomalOtherInput()
        {
            var input = new StringReader("");
            Console.SetIn(input);
            Assert.False(AWC.Startup_autoswitch());
        }
    }
}

