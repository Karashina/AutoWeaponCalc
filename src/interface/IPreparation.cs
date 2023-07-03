using CalcsheetGenerator.Common;

namespace CalcsheetGenerator.Interfaces
{
    public interface IPreparation
    {
        public abstract void SelectMode();

        public abstract UserInput Startup();
    }

}