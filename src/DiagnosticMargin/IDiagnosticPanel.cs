using System.Windows;

namespace DiagnosticMargin
{
    public interface IDiagnosticPanel
    {
        UIElement UI { get; }

        void Activate();

        void Inactivate();

        void Close();
    }
}
