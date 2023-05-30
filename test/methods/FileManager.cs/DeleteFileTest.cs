using CalcsheetGenerator.Module;

namespace Test.Methods
{
    public class DeleteFileTest
    {

        [Fact(DisplayName="ファイル削除処理が実行されること")]
        public void Nomal()
        {
            Mock<_File> MockFile = new Mock<_File>();
            _File.Current = MockFile.Object;

            _File.Current.Delete(CalcsheetGenerator.Config.Path.File.TempSimConfigText);

            MockFile.Verify(e => e.Delete(CalcsheetGenerator.Config.Path.File.TempSimConfigText));
        }
    }
}