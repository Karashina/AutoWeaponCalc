namespace CalcsheetGenerator.Common
{
    public class UserInput
    {
        public string CharacterName { get; } 
        public string WeaponType { get; } 
        public string WeaponRefineRank { get; set; } 
        public string ArtifactModeSel { get; set; } 
        public string MainstatSel { get; set; } 

        public UserInput(string CharacterName, string WeaponType, string WeaponRefineRank, string ArtifactModeSel, string MainstatSel)
        {
            this.CharacterName = CharacterName;
            this.WeaponType = WeaponType;
            this.WeaponRefineRank = WeaponRefineRank;
            this.ArtifactModeSel = ArtifactModeSel;
            this.MainstatSel = MainstatSel;
        }

        public bool IsSetPropertyNullOrEmpty()
        {
            return (string.IsNullOrEmpty(this.CharacterName) ||
                string.IsNullOrEmpty(this.WeaponType) ||
                string.IsNullOrEmpty(this.WeaponRefineRank) ||
                string.IsNullOrEmpty(this.ArtifactModeSel)) ||
                string.IsNullOrEmpty(this.MainstatSel);
        }
    }
}