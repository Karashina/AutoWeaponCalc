using CalcsheetGenerator.Common;

namespace Test.Methods
{
    public class IsSetPropertyNullOrEmptyTest
    {
        [Fact(DisplayName="空文字列が含まれていない場合Falseを返す")]
        public void Nomal()
        {
            UserInput InitialSetting = new UserInput("Jean", "sword", "y", "0");

            bool actual = InitialSetting.IsSetPropertyNullOrEmpty();

            Assert.False(actual);
        }

        [Theory(DisplayName="空文字列が含まれている場合Trueを返す")]
        [InlineData("", "sword", "y", "0")]
        [InlineData("Jean", "", "y", "0")]
        [InlineData("Jean", "sword", "", "0")]
        [InlineData("Jean", "sword", "y", "")]
        public void Anomaly(string CharacterName, string WeaponType, string WeaponRefineRank, string ArtifactModeSel)
        {
            UserInput InitialSetting = new UserInput(CharacterName, WeaponType, WeaponRefineRank, ArtifactModeSel);

            bool actual = InitialSetting.IsSetPropertyNullOrEmpty();

            Assert.True(actual);
        }
    }
}