using CalcsheetGenerator.Common;
using CalcsheetGenerator.Module;

namespace CalcsheetGenerator.Interfaces
{
    public interface ISettingFileReader
    {
        public abstract List<WeaponData> GetWeaponList(UserInput InitialSetting, StreamReaderFactory _StreamReader);

        public abstract List<ArtifactData> GetArtifactList(StreamReaderFactory _StreamReader);
    }

}