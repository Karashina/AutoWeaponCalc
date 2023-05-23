using System.Text;

using CalcsheetGenerator;
using CalcsheetGenerator.Common;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
        public class GetWeaponListTest
    {
        static readonly SettingFileReader _SettingFileReader = SettingFileReader.GetInstance();
        UserInput InitialSetting;

        public GetWeaponListTest()
        {
            this.InitialSetting = new UserInput("Jean", "sword", "y", "0");
        }
        [Fact(DisplayName="武器のCSVから武器情報が取得できること")]
        public void Nomal()
        {
            Mock<IStreamReaderFactory> MockStreamReaderFactory = new Mock<IStreamReaderFactory>();
            string fakeCSVContents = String.Join(Environment.NewLine, new[]
            {
                "weapontype,weaponname,rarity",
                "霧切の廻光,mistsplitterreforged,1",
                "風鷹剣,aquilafavonia,1",
                "斬山の刃,summitshaper,1"
            });
            byte[] fakeFileBytes = Encoding.UTF8.GetBytes(fakeCSVContents);
            MemoryStream fakeMemoryStream = new MemoryStream(fakeFileBytes);
            MockStreamReaderFactory.Setup(o => o.Create(It.IsAny<String>()))
                                    .Returns(() => new StreamReader(fakeMemoryStream));

            List<WeaponData> result = _SettingFileReader.GetWeaponList(InitialSetting, MockStreamReaderFactory.Object);

            List<WeaponData> expectList = new List<WeaponData>();
            expectList.Add(new WeaponData("霧切の廻光", "mistsplitterreforged", "1"));
            expectList.Add(new WeaponData("風鷹剣", "aquilafavonia", "1"));
            expectList.Add(new WeaponData("斬山の刃", "summitshaper", "1"));
            Assert.NotEmpty(result);
            Assert.Equivalent(expectList, result);
        }

        [Fact(DisplayName="武器のCSVがない場合DirectoryNotFoundExceptionを投げること")]
        public void ErrorNotFoundFile()
        {
            var exception = Assert.Throws<DirectoryNotFoundException>(() => 
                _SettingFileReader.GetWeaponList(InitialSetting));
            Assert.NotEmpty(exception.Message);
        }
    }
}