using Common;
using CalcsheetGenerator;

namespace Interfaces
{
    interface ISettingFileReader
    {
        public abstract List<WeaponData> GetWeaponList(UserInput InitialSetting);

        public abstract List<ArtifactData> GetArtifactList();
    }

}