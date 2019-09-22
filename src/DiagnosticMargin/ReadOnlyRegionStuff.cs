using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace DiagnosticMargin
{
    [Export(typeof(EditorFormatDefinition))]
    [Name("ReadOnlyRegion")]
    internal sealed class RorMarkerDefinition : MarkerFormatDefinition
    {
        public RorMarkerDefinition()
        {
            ZOrder = 1;
            Fill = new SolidColorBrush(Colors.Khaki);
            Fill.Opacity = 0.35;
            Border = new Pen(new SolidColorBrush(Colors.DarkGray), 0.5);
            Fill.Freeze();
            Border.Freeze();
        }
    }

    internal class ReadOnlyRegionTag : TextMarkerTag
    {
        public ReadOnlyRegionTag() : base("ReadOnlyRegion") { }
    }

    internal sealed class ReadOnlyRegionTagger : ITagger<ReadOnlyRegionTag>
    {
        private bool isActive;
        private ITextBuffer buffer;
        private ReadOnlyRegionTag mondoTag;

        public ReadOnlyRegionTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.mondoTag = new ReadOnlyRegionTag();
            this.isActive = false;
        }

        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (value != this.isActive)
                {
                    if (value)
                    {
                        this.buffer.ReadOnlyRegionsChanged += OnReadOnlyRegionsChanged;
                    }
                    else
                    {
                        this.buffer.ReadOnlyRegionsChanged -= OnReadOnlyRegionsChanged;
                    }
                    this.isActive = value;
                    OnReadOnlyRegionsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(this.buffer.CurrentSnapshot, 0, this.buffer.CurrentSnapshot.Length)));
                }
            }
        }

        public IEnumerable<ITagSpan<ReadOnlyRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.isActive)
            {
                var allReadOnlyExtents = this.buffer.GetReadOnlyExtents(new Span(0, this.buffer.CurrentSnapshot.Length));
                foreach (Span rorSpan in allReadOnlyExtents)
                {
                    foreach (SnapshotSpan querySpan in spans)
                    {
                        Span? overlap = rorSpan.Overlap(querySpan);
                        if (overlap.HasValue)
                        {
                            yield return new TagSpan<ReadOnlyRegionTag>(new SnapshotSpan(this.buffer.CurrentSnapshot, rorSpan), this.mondoTag);
                            break;
                        }
                    }
                }
            }
            else
            {
                yield break;
            }
        }

        private void OnReadOnlyRegionsChanged(object sender, SnapshotSpanEventArgs args)
        {
            EventHandler<SnapshotSpanEventArgs> handler = TagsChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(ReadOnlyRegionTag))]
    internal class ReadOnlyRegionTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(typeof(ReadOnlyRegionTagger), () => (new ReadOnlyRegionTagger(buffer))) as ITagger<T>;
        }
    }
}
