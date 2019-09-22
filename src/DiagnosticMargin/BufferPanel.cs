using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace DiagnosticMargin
{
    [Export(typeof(IDiagnosticPanelFactory))]
    [Name("Buffer")]
    [Order(After = "View")]
    [ContentType("Text")]
    [TextViewRole("Interactive")]
    internal sealed class BufferPanelFactory : IDiagnosticPanelFactory
    {
        [Import]
        ITextEditorFactoryService EditorFactory { get; set; }

        [Import]
        IEditorOperationsFactoryService EditorOperationsFactory { get; set; }

        public IDiagnosticPanel CreatePanel(IWpfTextViewHost textViewHost)
        {
            return new BufferPanel(textViewHost, EditorOperationsFactory, EditorFactory);
        }
    }

    internal sealed class BufferPanel : StackPanel, IDiagnosticPanel
    {
        private Dictionary<ITextBuffer, BufferBar> barMap = new Dictionary<ITextBuffer, BufferBar>();
        private Queue<GraphBuffersChangedEventArgs> graphQ = new Queue<GraphBuffersChangedEventArgs>();
        private IWpfTextView textView;
        private IWpfTextViewHost textViewHost;
        private IEditorOperations editorOperations;
        private ITextEditorFactoryService editorFactory;

        public BufferPanel(IWpfTextViewHost textViewHost, IEditorOperationsFactoryService editorOperationsFactory, ITextEditorFactoryService editorFactory)
        {
            this.textViewHost = textViewHost;
            this.textView = textViewHost.TextView;
            this.editorOperations = editorOperationsFactory.GetEditorOperations(this.textView);
            this.editorFactory = editorFactory;
        }

        public void Activate()
        {
            foreach (ITextBuffer buffer in this.textView.BufferGraph.GetTextBuffers((b) => true))
            {
                BufferBar bar = new BufferBar(buffer, this.textViewHost, this.editorOperations, this.editorFactory);
                this.barMap.Add(buffer, bar);
            }
            AddChildren();

            this.textView.BufferGraph.GraphBuffersChanged += OnGraphBuffersChanged;
        }

        private void AddChildren()
        {
            List<ITextBuffer> sortedBuffers = SortGraph(this.textView.TextViewModel.VisualBuffer, this.textView.TextViewModel.DataModel.DocumentBuffer);
            for (int b = sortedBuffers.Count - 1; b >= 0; --b)
            {
                BufferBar bb;
                if (this.barMap.TryGetValue(sortedBuffers[b], out bb))
                {
                    Children.Add(bb);
                }
                else
                {
                    //System.Diagnostics.Debug.Fail("Buffer not in bar map???");
                }
            }
        }

        public void Inactivate()
        {
            this.textView.BufferGraph.GraphBuffersChanged -= OnGraphBuffersChanged;
            foreach (var pair in this.barMap)
            {
                pair.Value.Close();
                Children.Remove(pair.Value);
            }
            this.barMap.Clear();
            this.graphQ.Clear();
        }

        public void Close()
        {
            Inactivate();
        }

        public UIElement UI
        {
            get { return this; }
        }

        private void OnGraphBuffersChanged(object sender, GraphBuffersChangedEventArgs e)
        {
            foreach (ITextBuffer removedBuffer in e.RemovedBuffers)
            {
                BufferBar bar = this.barMap[removedBuffer];
                this.barMap.Remove(removedBuffer);
                bar.Close();
            }
            foreach (ITextBuffer addedBuffer in e.AddedBuffers)
            {
                BufferBar bar = new BufferBar(addedBuffer, this.textViewHost, this.editorOperations, this.editorFactory);
                this.barMap.Add(addedBuffer, bar);
            }
            Children.Clear();
            AddChildren();
        }

        private List<ITextBuffer> SortGraph(ITextBuffer visualBuffer, ITextBuffer documentBuffer)
        {
            List<ITextBuffer> result = new List<ITextBuffer>();
            HashSet<ITextBuffer> processed = new HashSet<ITextBuffer>();
            // we want the document buffer to be first in this list
            // todo this is wrong when building views over intermediate buffers; we think some random buffer is the
            // document buffer.
            if (!(documentBuffer is IProjectionBufferBase))
            {
                result.Add(documentBuffer);
                processed.Add(documentBuffer);
                if (visualBuffer != documentBuffer)
                {
                    Traverse(visualBuffer, result, processed);
                }
            }
            else
            {
                Traverse(visualBuffer, result, processed);
            }

            return result;
        }

        private void Traverse(ITextBuffer buffer, List<ITextBuffer> result, HashSet<ITextBuffer> processed)
        {
            processed.Add(buffer);
            IProjectionBufferBase proj = buffer as IProjectionBufferBase;
            if (proj != null)
            {
                foreach (ITextBuffer sourceBuffer in proj.SourceBuffers)
                {
                    if (!processed.Contains(sourceBuffer))
                    {
                        Traverse(sourceBuffer, result, processed);
                    }
                }
            }
            result.Add(buffer);
        }

    }
}
