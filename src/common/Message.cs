namespace CalcsheetGenerator.Common
{
    public static class Message
    {
        public static class Notice
        {
            // モード選択
            public static readonly string SelectMode = "mode selection(auto / manual) [a|m] :";

            // キャラクター選択
            public static readonly string SelectCharctor = "Type the name of the character to calculate:";

            // 武器選択
            public static readonly string SelectWeapon = "Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :";

            // 精錬選択
            public static readonly string SelectRefinement = "Type the refinement rank of the weapon to calculate [0=auto][1-5] :";

            // 聖遺物最適化選択
            public static readonly string SelectArtifactOptimization = "Do you want to use artifact mode? [y|n]:";

            // 開始メッセージ
            public static readonly string ProcessStart = "Initialize calculation for artifact ";

            // 終了メッセージ
            public static readonly string ProcessEnd = "Calculation completed for artifact ";
            
            // サブステの最適化gcshim起動一回目
            public static readonly string SubstatOptimizationStart = "Substat optimization in progress...";
            // サブステの最適化gcshim起動一回目
            public static readonly string SubstatOptimizationEnd = "Substat optimization completed";
        }

        public static class Error
        {
            public static readonly string SelectMode = "Invalid Input at Mode Selection!";

            // 入力値不備
            public static readonly string StartupAutomode = "Invalid Input at Automode Startup!";
            
            public static readonly string GcsimOutputNone = "ERROR: unrecognized weapon";

        }
    }
}