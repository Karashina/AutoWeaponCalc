using CalcsheetGenerator.Common;
using CalcsheetGenerator.Module;

namespace CalcsheetGenerator.Interfaces
{
    public interface ISettingFileReader
    {
        public abstract List<WeaponData> GetWeaponList(UserInput InitialSetting, IStreamReaderFactory StreamReaderFactory);

        public abstract List<ArtifactData> GetArtifactList(IStreamReaderFactory StreamReaderFactory);
    }

}