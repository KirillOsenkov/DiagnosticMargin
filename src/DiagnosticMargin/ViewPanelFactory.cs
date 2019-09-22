using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace DiagnosticMargin
{
    [Export(typeof(IDiagnosticPanelFactory))]
    [Name("View")]
    [ContentType("Text")]
    [TextViewRole("Interactive")]
    internal sealed class ViewPanelFactory : IDiagnosticPanelFactory
    {
        public IDiagnosticPanel CreatePanel(IWpfTextViewHost textViewHost)
        {
            return new ViewPanel(textViewHost.TextView);
        }
    }

}
