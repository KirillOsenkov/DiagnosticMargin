using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace DiagnosticMargin
{
    [Export(typeof(IDiagnosticPanelFactory))]
    [Name("Performance")]
    [Order(After = "Buffer")]
    [ContentType("Text")]
    [TextViewRole("Interactive")]
    internal sealed class PerformancePanelFactory : IDiagnosticPanelFactory
    {
        [Import]
        IEditorOperationsFactoryService EditorOperationsFactory { get; set; }

        public IDiagnosticPanel CreatePanel(IWpfTextViewHost textViewHost)
        {
            return new PerformancePanel(textViewHost, EditorOperationsFactory);
        }
    }

}
