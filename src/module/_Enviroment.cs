namespace CalcsheetGenerator.Module
{
    public abstract class _Environment
    {
        public static _Environment Current
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

        public abstract void Exit(int value);

        public static void ResetToDefault()
        {
            _current = Default_Environment.Instance;
        }

        static _Environment _current = Default_Environment.Instance;
    }

    public class Default_Environment : _Environment
    {
        public override void Exit(int value)
        {
            Environment.Exit(value);
        }

        public static Default_Environment Instance => _instance.Value;

        static readonly Lazy<Default_Environment> _instance = new Lazy<Default_Environment>(() => new Default_Environment());
    }
}