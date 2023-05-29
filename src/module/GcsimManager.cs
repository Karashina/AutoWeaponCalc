using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module
{

    public class GcsimManager : IGcsimManager
    {
        private static IGcsimManager? Instance;

        private GcsimManager()
        {
            //pass
        }

        public static IGcsimManager GetInstance()
        {
            if (Instance == null)
            {
                GcsimManager.Instance = new GcsimManager();
            }
            return GcsimManager.Instance;
        }

        public IGcsim CreateGcsimInstance()
        {
            return new Gcsim();
        }
    }
}