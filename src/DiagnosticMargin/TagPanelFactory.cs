using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;

namespace DiagnosticMargin
{
    [Export(typeof(IDiagnosticPanelFactory))]
    [Name("Tag")]
    [Order(After = "Buffer")]
    [ContentType("Text")]
    [TextViewRole("Interactive")]
    public sealed class TagPanelFactory : IDiagnosticPanelFactory
    {
        [Import]
        IViewTagAggregatorFactoryService ViewTaggerFactory = null;

        public IDiagnosticPanel CreatePanel(IWpfTextViewHost textViewHost)
        {
            return new TagPanel(textViewHost.TextView, this.ViewTaggerFactory.CreateTagAggregator<ITag>(textViewHost.TextView));
        }
    }

}
