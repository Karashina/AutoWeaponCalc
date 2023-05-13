using CalcsheetGenerator.Common;
using CalcsheetGenerator.Enum;

namespace CalcsheetGenerator.Interfaces
{
    interface IPreparation
    {
        public abstract void SelectMode();

        public abstract UserInput Startup();
    }

}