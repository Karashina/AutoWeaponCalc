using CalcsheetGenerator.Common;

namespace CalcsheetGenerator.Interfaces
{
    public interface ISettingFileReader
    {
        public abstract List<WeaponData> GetWeaponList(UserInput InitialSetting, IStreamReaderFactory? _StreamReaderFactory=null);

        public abstract List<ArtifactData> GetArtifactList(IStreamReaderFactory? _StreamReaderFactory=null);

        public abstract string GetTextFileContent(string TextFilePath, IStreamReaderFactory? _StreamReaderFactory=null);
    }

}