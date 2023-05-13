namespace CalcsheetGenerator.Common
{
    public class UserInput
    {
        public string? CharacterName { get; } 
        public string? WeaponType { get; } 
        public string? WeaponRefineRank { get; set; } 
        public string? ArtifactModeSel { get; set; } 

        public UserInput(string? CharacterName, string? WeaponType, string? WeaponRefineRank, string? ArtifactModeSel)
        {
            this.CharacterName = CharacterName;
            this.WeaponType = WeaponType;
            this.WeaponRefineRank = WeaponRefineRank;
            this.ArtifactModeSel = ArtifactModeSel;
        }

        public bool CheckNullAndEmpty()
        {
            return (string.IsNullOrEmpty(this.CharacterName) ||
                string.IsNullOrEmpty(this.WeaponType) ||
                string.IsNullOrEmpty(this.WeaponRefineRank) ||
                string.IsNullOrEmpty(this.ArtifactModeSel));
        }
    }
}