using Microsoft.VisualStudio.Text.Editor;

namespace DiagnosticMargin
{
    public interface IDiagnosticPanelFactory
    {
        IDiagnosticPanel CreatePanel(IWpfTextViewHost textViewHost);
    }
}
