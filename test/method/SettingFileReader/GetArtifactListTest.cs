using System.Text;

using CalcsheetGenerator;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
        public class GetArtifactListTest
    {
        static readonly SettingFileReader _SettingFileReader = SettingFileReader.GetInstance();

        [Fact(DisplayName="聖遺物のCSVから聖遺物セット情報が取得できること")]
        public void Nomal()
        {
            Mock<IStreamReaderFactory> MockStreamReaderFactory = new Mock<IStreamReaderFactory>();
            string fakeCSVContents = String.Join(Environment.NewLine, new[]
            {
                "is4pc,artiname1,artiname2(0 if null)",
                "1,vv,0",
                "0,gd,vv"
            });
            byte[] fakeFileBytes = Encoding.UTF8.GetBytes(fakeCSVContents);
            MemoryStream fakeMemoryStream = new MemoryStream(fakeFileBytes);
            MockStreamReaderFactory.Setup(o => o.Create(It.IsAny<String>()))
                                    .Returns(() => new StreamReader(fakeMemoryStream));

            List<ArtifactData> result = _SettingFileReader.GetArtifactList(MockStreamReaderFactory.Object);

            List<ArtifactData> expectList = new List<ArtifactData>();
            expectList.Add(new ArtifactData("1", "vv", "4pc"));
            expectList.Add(new ArtifactData("0", "gd", "vv"));
            Assert.NotEmpty(result);
            Assert.Equivalent(expectList, result);
        }

        [Fact(DisplayName="聖遺物のCSVがない場合DirectoryNotFoundExceptionを投げること")]
        public void ErrorNotFoundFile()
        {
            var exception = Assert.Throws<DirectoryNotFoundException>(() => 
                _SettingFileReader.GetArtifactList());
            Assert.NotEmpty(exception.Message);
        }
    }
}