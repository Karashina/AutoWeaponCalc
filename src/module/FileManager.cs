using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module
{
    public class FileManager : IFileManager
    {
        private static FileManager? Instance;

        private FileManager()
        {
            // pass
        }

        public static FileManager GetInstance()
        {
            if (Instance == null)
            {
                FileManager.Instance = new FileManager();
            }
            return FileManager.Instance;
        }
        public void DeleteFile(string Path)
        {
            _File.Current.Delete(Path);
        }
    }
}