using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace DiagnosticMargin
{
    public partial class PerformancePanel : StackPanel, IDiagnosticPanel
    {
        IWpfTextView textView;
        IWpfTextViewHost textViewHost;
        IEditorOperations editorOperations;

        public PerformancePanel(IWpfTextViewHost textViewHost, IEditorOperationsFactoryService editorOperationsFactory)
        {
            InitializeComponent();
            this.textViewHost = textViewHost;
            this.textView = textViewHost.TextView;
            this.editorOperations = editorOperationsFactory.GetEditorOperations(this.textView);
        }

        public UIElement UI
        {
            get { return this; }
        }

        public void Activate()
        {
        }

        public void Inactivate()
        {
        }

        public void Close()
        {
        }

        void MasticateClick(object sender, RoutedEventArgs e)
        {
        }

        void MasticateDiffClick(object sender, RoutedEventArgs e)
        {
        }

        void ScrollTest(Action prepare, Action perform)
        {
            prepare();
            DateTime startTimeStamp = DateTime.Now;
            perform();
            DateTime endTimeStamp = DateTime.Now;

            TimeSpan span = endTimeStamp - startTimeStamp;
            System.Windows.Forms.MessageBox.Show(string.Format("Scroll from start to end took {0} to complete.", span.ToString()), "Scroll Performance");
        }

        void ScrollDownButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollTest(prepare: () => { this.editorOperations.MoveToStartOfDocument(false); },
                       perform: () =>
                       {
                           for (int line = 0; line < this.textView.VisualSnapshot.LineCount; ++line)
                           {
                               this.textViewHost.HostControl.Dispatcher.Invoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(delegate
                               {
                                   this.textView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Down, 1);
                                   return null;
                               }), null);
                           }
                       });
        }

        void LineScrollDownButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollTest(prepare: () => { this.editorOperations.MoveToStartOfDocument(false); },
                       perform: () =>
                       {
                           for (int line = 0; line < this.textView.VisualSnapshot.LineCount; ++line)
                           {
                               this.textViewHost.HostControl.Dispatcher.Invoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(delegate
                               {
                                   this.editorOperations.MoveLineDown(false);
                                   return null;
                               }), null);
                           };
                       });
        }

        void PageScrollDownButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollTest(prepare: () => { this.editorOperations.MoveToStartOfDocument(false); },
                       perform: () =>
                       {
                           int line = 0;
                           while (line < this.textView.VisualSnapshot.LineCount - 1)
                           {
                               this.textViewHost.HostControl.Dispatcher.Invoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(delegate
                               {
                                   this.editorOperations.PageDown(false);
                                   return null;
                               }), null);
                               line = this.textView.TextViewLines.LastVisibleLine.ExtentAsMappingSpan.End.GetPoint(this.textView.VisualSnapshot.TextBuffer, PositionAffinity.Predecessor).Value.Position;
                           };
                       });
        }

        void ScrollUpButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollTest(prepare: () => { this.editorOperations.MoveToEndOfDocument(false); },
                       perform: () =>
                       {
                           for (int line = 0; line < this.textView.VisualSnapshot.LineCount; ++line)
                           {
                               this.textViewHost.HostControl.Dispatcher.Invoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(delegate
                               {
                                   this.textView.ViewScroller.ScrollViewportVerticallyByLines(ScrollDirection.Up, 1);
                                   return null;
                               }), null);
                           }
                       });
        }

        void LineScrollUpButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollTest(prepare: () => { this.editorOperations.MoveToEndOfDocument(false); },
                       perform: () =>
                       {
                           for (int line = 0; line < this.textView.VisualSnapshot.LineCount; ++line)
                           {
                               this.textViewHost.HostControl.Dispatcher.Invoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(delegate
                               {
                                   this.editorOperations.MoveLineUp(false);
                                   return null;
                               }), null);
                           };
                       });
        }

        void PageScrollUpButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollTest(prepare: () => { this.editorOperations.MoveToEndOfDocument(false); },
                       perform: () =>
                       {
                           int line = this.textView.VisualSnapshot.LineCount - 1;
                           while (line > 0)
                           {
                               this.textViewHost.HostControl.Dispatcher.Invoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(delegate
                               {
                                   this.editorOperations.PageUp(false);
                                   return null;
                               }), null);
                               line = this.textView.TextViewLines.FirstVisibleLine.ExtentAsMappingSpan.Start.GetPoint(this.textView.VisualSnapshot.TextBuffer, PositionAffinity.Predecessor).Value.Position;
                           };
                       });
        }
    }
}
