namespace CalcsheetGenerator.Module
{
    public abstract class _File
    {
        public static _File Current
        {
            get
            {
                return _current;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _current = value;
            }
        }

        public abstract void Delete(string Path);

        public static void ResetToDefault()
        {
            _current = Default_File.Instance;
        }

        static _File _current = Default_File.Instance;
    }

    public class Default_File : _File
    {
        public override void Delete(string Path)
        {
            File.Delete(Path);
        }

        public static Default_File Instance => _instance.Value;

        static readonly Lazy<Default_File> _instance = new Lazy<Default_File>(() => new Default_File());
    }
}