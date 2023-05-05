namespace Common
{
    public static class Message
    {
        public static class Notice
        {
            // モード選択
            public static string SelectMode = "mode selection(auto / manual) [a|m] :";

            // キャラクター選択
            public static string SelectCharctor = "Type the name of the character to calculate:";

            // 武器選択
            public static string SelectWeapon = "Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :";

            // 精錬選択
            public static string SelectRefinement = "Type the refinement rank of the weapon to calculate [0=auto][1-5] :";

            // 聖遺物最適化選択
            public static string SelectArtifactOptimization = "Do you want to use artifact mode? [y|n]:";

            // 開始メッセージ
            public static string ProcessStart = "Initialize calculation for artifact ";

            // 終了メッセージ
            public static string ProcessEnd = "Calculation completed for artifact ";
        }

        public static class Error
        {
            public static string SelectMode = "Invalid Input at Mode Selection!";

            // 入力値不備
            public static string StartupAutomode = "Invalid Input at Automode Startup!";

        }
    }
}