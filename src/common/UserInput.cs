namespace Common
{
    public class UserInput
    {
        public string? CharacterName { get; } 
        public string? WeaponType { get; } 
        public string? WeaponRefinerank { get; set; } 
        public string? ArtifactModeSel { get; set; } 

        public UserInput(string? CharacterName, string? WeaponType, string? WeaponRefinerank, string? ArtifactModeSel)
        {
            this.CharacterName = CharacterName;
            this.WeaponType = WeaponType;
            this.WeaponRefinerank = WeaponRefinerank;
            this.ArtifactModeSel = ArtifactModeSel;
        }

        public bool CheckNullAndEmpty()
        {
            if (string.IsNullOrEmpty(this.CharacterName) ||
                string.IsNullOrEmpty(this.WeaponType) ||
                string.IsNullOrEmpty(this.WeaponRefinerank) ||
                string.IsNullOrEmpty(this.ArtifactModeSel))
            {
                return false;
            }
            return true;
        }
    }
}