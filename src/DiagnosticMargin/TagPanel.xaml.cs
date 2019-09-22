using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DiagnosticMargin
{
    public partial class TagPanel : StackPanel, IDiagnosticPanel
    {
        private IWpfTextView textView;
        private ITagAggregator<ITag> aggregator;
        private bool heightManuallySet;

        public TagPanel(IWpfTextView textView, ITagAggregator<ITag> aggregator)
        {
            InitializeComponent();
            DataContext = this;

            this.textView = textView;
            this.aggregator = aggregator;
            this.viewer.Height = Math.Min(textView.ViewportHeight / 4, 150);
        }

        void OnThumbDragCompleted(object sender, DragCompletedEventArgs args)
        {
            this.heightManuallySet = true;
            double verticalChange = Math.Min(-args.VerticalChange, this.textView.ViewportHeight);
            double newHeight = (this.viewer.Height + verticalChange);
            this.viewer.Height = Math.Max(newHeight, 0.0);
        }

        void OnViewportHeightChanged(object sender, EventArgs args)
        {
            if (!this.heightManuallySet && this.viewer.Height == 0 && this.textView.ViewportHeight > 150)
            {
                this.viewer.Height = Math.Min(this.textView.ViewportHeight / 4, 150);
            }
        }

        void OnSelectionChanged(object sender, EventArgs args)
        {
            UpdateVisuals();
        }

        void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs args)
        {
            if (args.OldSnapshot != args.NewSnapshot)
            {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            var tagger = CurrentTagMarkerProvider.GetCurrentTagMarkerForView(this.textView);
            Debug.Assert(tagger != null);

            tagger.RemoveTagSpans(s => true);
            this.treeView.Items.Clear();

            if (this.textView.Selection.SelectedSpans.Count == 0)
            {
                return;
            }

            // Don't do any work if we aren't visible
            if (ActualHeight < 5)
            {
                return;
            }

            Dictionary<Type, List<IMappingTagSpan<ITag>>> tagSpansByType = new Dictionary<Type, List<IMappingTagSpan<ITag>>>();

            foreach (var tagSpan in this.aggregator.GetTags(this.textView.Selection.SelectedSpans))
            {
                Type type = tagSpan.Tag.GetType();

                // Ignore our own tags.
                if (type == typeof(CurrentTagMarker))
                    continue;

                if (!tagSpansByType.ContainsKey(type))
                    tagSpansByType[type] = new List<IMappingTagSpan<ITag>>();

                tagSpansByType[type].Add(tagSpan);
            }

            foreach (var type in tagSpansByType.Keys.OrderBy(type => type.Name))
            {
                List<IMappingTagSpan<ITag>> tagSpans = tagSpansByType[type];
                TreeViewItem typeItem = new TreeViewItem() { Header = string.Format("{0} ({1})", type.Name, tagSpans.Count) };

                List<SnapshotSpan> snapshotSpans = new List<SnapshotSpan>();

                foreach (var tagSpan in tagSpans)
                {
                    TreeViewItem tagSpanItem = new TreeViewItem();
                    var spansInView = tagSpan.Span.GetSpans(this.textView.TextSnapshot);

                    // This should never happen, since we asked for tags over the selection, which
                    // we know is in the view.
                    if (spansInView.Count == 0)
                        continue;

                    var snapshotSpan = new SnapshotSpan(spansInView[0].Start, spansInView[spansInView.Count - 1].End);
                    snapshotSpans.Add(snapshotSpan);

                    object toolTipContent;
                    string displayString = DisplayStringForTag(tagSpan.Tag, out toolTipContent);
                    if (displayString != null)
                    {
                        tagSpanItem.Header = string.Format("Tag: {0}, Span: {1}", displayString, snapshotSpan.Span);
                        if (toolTipContent != null)
                            tagSpanItem.ToolTip = toolTipContent;
                    }
                    else
                    {
                        tagSpanItem.Header = string.Format("Span: {0}", snapshotSpan.Span);
                    }

                    tagSpanItem.Selected += (sender, args) =>
                    {
                        tagger.RemoveTagSpans(s => true);
                        tagger.CreateTagSpan(snapshotSpan.Snapshot.CreateTrackingSpan(snapshotSpan, SpanTrackingMode.EdgeExclusive), new CurrentTagMarker());
                        args.Handled = true;
                    };

                    typeItem.Items.Add(tagSpanItem);
                }

                typeItem.Selected += (sender, args) =>
                {
                    tagger.RemoveTagSpans(s => true);

                    foreach (var snapshotSpan in snapshotSpans)
                    {
                        tagger.CreateTagSpan(snapshotSpan.Snapshot.CreateTrackingSpan(snapshotSpan, SpanTrackingMode.EdgeExclusive), new CurrentTagMarker());
                    }
                };

                this.treeView.Items.Add(typeItem);
            }
        }

        string DisplayStringForTag(ITag tag, out object toolTipContent)
        {
            toolTipContent = null;

            List<string> content = new List<string>();

            ClassificationTag classification = tag as ClassificationTag;
            if (classification != null)
            {
                content.Add(classification.ClassificationType.ToString());
            }

            ErrorTag error = tag as ErrorTag;
            if (error != null)
            {
                toolTipContent = error.ToolTipContent;
                content.Add(error.ErrorType);
            }

            IOutliningRegionTag region = tag as IOutliningRegionTag;
            if (region != null)
            {
                toolTipContent = region.CollapsedHintForm;
                content.Add(string.Format("IsImplementation: {0}, IsDefaultCollapsed: {1}", region.IsImplementation, region.IsDefaultCollapsed));
            }

            TextMarkerTag marker = tag as TextMarkerTag;
            if (marker != null)
            {
                content.Add(marker.Type);
            }

            SpaceNegotiatingAdornmentTag snat = tag as SpaceNegotiatingAdornmentTag;
            if (snat != null)
            {
                content.Add(string.Format("TextHeight: {0}, Width: {1}, Affinity: {2}", snat.TextHeight, snat.Width, snat.Affinity));
            }

            IntraTextAdornmentTag itat = tag as IntraTextAdornmentTag;
            if (itat != null)
            {
                content.Add("Content: " + itat.Adornment.ToString());
            }

            IUrlTag url = tag as IUrlTag;
            if (url != null)
            {
                content.Add(url.Url.ToString());
            }

            if (content.Count > 0)
            {
                return string.Join(Environment.NewLine, content);
            }
            else
            {
                return null;
            }
        }

        public UIElement UI
        {
            get { return this; }
        }

        public void Activate()
        {
            this.textView.ViewportHeightChanged += OnViewportHeightChanged;
            this.textView.Selection.SelectionChanged += OnSelectionChanged;
            this.textView.LayoutChanged += OnLayoutChanged;
            UpdateVisuals();
        }

        public void Inactivate()
        {
            this.textView.ViewportHeightChanged -= OnViewportHeightChanged;
            this.textView.Selection.SelectionChanged -= OnSelectionChanged;
            this.textView.LayoutChanged -= OnLayoutChanged;
            this.treeView.Items.Clear();

            var tagger = CurrentTagMarkerProvider.GetCurrentTagMarkerForView(this.textView);
            Debug.Assert(tagger != null);

            tagger.RemoveTagSpans(s => true);
        }

        public void Close()
        {
            Inactivate();
        }
    }

    internal class CurrentTagMarker : TextMarkerTag
    {
        public CurrentTagMarker() : base("CurrentTagMarker") { }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name("CurrentTagMarker")]
    [UserVisible(false)]
    internal sealed class CurrentTagMarkerFormat : MarkerFormatDefinition
    {
        CurrentTagMarkerFormat()
        {
            Fill = Brushes.LightGreen;
            Border = new Pen(Brushes.DarkGreen, 1);
        }
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(TextMarkerTag))]
    internal sealed class CurrentTagMarkerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer != textView.TextBuffer)
                return null;

            return GetCurrentTagMarkerForView(textView) as ITagger<T>;
        }

        public static SimpleTagger<CurrentTagMarker> GetCurrentTagMarkerForView(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(() => new SimpleTagger<CurrentTagMarker>(view.TextBuffer));
        }
    }

}
