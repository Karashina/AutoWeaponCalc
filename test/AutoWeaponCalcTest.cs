using CalcsheetGenerator;

namespace CalcsheetGeneratorTest {
    public class AutoWeaponCalcTest
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

            // モード指定Autoでtrueを返すこと
            [Fact]
            public void NomalAuto()
            {
                var input = new StringReader("a");
                Console.SetIn(input);
                Assert.True(AWC.Startup_autoswitch());
            }

            // モード指定Manualでfalseを返すこと
            [Fact]
            public void NomalManual()
            {
                var input = new StringReader("m");
                Console.SetIn(input);
                Assert.False(AWC.Startup_autoswitch());
            }

            // モード指定なしでfalseを返すこと
            [Fact]
            public void NomalOtherInput()
            {
                var input = new StringReader("");
                Console.SetIn(input);
                Assert.False(AWC.Startup_autoswitch());
            }
        }
    }
}