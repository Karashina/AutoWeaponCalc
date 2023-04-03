using CalcsheetGenerator;

namespace CalcsheetGeneratorTest {
    public class AutoWeaponCalcTest : IDisposable
    {
        //setup
        public AutoWeaponCalcTest()
        {
            //pass
        }

        // teardown
        public void Dispose()
        {
            //pass
        }

        // モード指定Autoでtrueを返すこと
        [Fact]
        public void nomal_Auto_Startup_autoswitch()
        {
            var output = new StringWriter();
            Console.SetOut(output);

            var input = new StringReader("a");
            Console.SetIn(input);
            Assert.Equal(true, AWC.Startup_autoswitch());
        }

        // モード指定Manualでfalseを返すこと
        [Fact]
        public void nomal_Manual_Startup_autoswitch()
        {
            var output = new StringWriter();
            Console.SetOut(output);

            var input = new StringReader("m");
            Console.SetIn(input);
            Assert.Equal(false, AWC.Startup_autoswitch());
        }

        // モード指定なしでfalseを返すこと
        [Fact]
        public void nomal_Other_Startup_autoswitch()
        {
            var output = new StringWriter();
            Console.SetOut(output);

            var input = new StringReader("");
            Console.SetIn(input);
            Assert.Equal(false, AWC.Startup_autoswitch());
        }
    }

}