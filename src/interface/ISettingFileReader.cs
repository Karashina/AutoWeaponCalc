using CalcsheetGenerator.Common;

namespace CalcsheetGenerator.Interfaces
{
    public interface ISettingFileReader
    {
        public abstract List<WeaponData> GetWeaponList(UserInput InitialSetting, IStreamReader _StreamReader);

        public abstract List<ArtifactData> GetArtifactList(IStreamReader _StreamReader);

        public abstract string GetTextFileContet(string TextFilePath, IStreamReader _StreamReader);
    }

}