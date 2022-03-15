using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using System.Windows.Media.Media3D;

namespace DiagnosticMargin
{
    public partial class BufferBar : StackPanel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ITextEditorFactoryService factory;
        private readonly IEditorOperations editorOperations;
        private readonly ITextBuffer textBuffer;
        private readonly IProjectionBuffer projBuffer;
        private readonly IElisionBuffer elBuffer;
        private readonly IWpfTextView textView;
        private readonly IWpfTextViewHost textViewHost;
        private readonly ITextDocument document;

        private ComboBox SelectionCombo = new ComboBox();
        private Label SelectionLabel = new Label();
        private bool selectionLabelInstalled;

        private string contentTypeText;
        private string versionText;
        private string reiteratedText;
        private string positionText;
        private string lengthText;
        private string spansText;
        private string encodingText;

        public ITextBuffer Buffer
        {
            get { return this.textBuffer; }
        }

        public string ContentTypeText
        {
            get { return this.contentTypeText; }
            set
            {
                this.contentTypeText = value;
                OnPropertyChanged("ContentTypeText");
            }
        }

        public string VersionText
        {
            get { return this.versionText; }
            set
            {
                this.versionText = value;
                OnPropertyChanged("VersionText");
            }
        }

        public string ReiteratedText
        {
            get { return this.reiteratedText; }
            set
            {
                this.reiteratedText = value;
                OnPropertyChanged("ReiteratedText");
            }
        }

        public string PositionText
        {
            get { return this.positionText; }
            set
            {
                this.positionText = value;
                OnPropertyChanged("PositionText");
            }
        }

        public string LengthText
        {
            get { return this.lengthText; }
            set
            {
                this.lengthText = value;
                OnPropertyChanged("LengthText");
            }
        }

        public string SpansText
        {
            get { return this.spansText; }
            set
            {
                this.spansText = value;
                OnPropertyChanged("SpansText");
            }
        }

        public string EncodingText
        {
            get { return this.encodingText; }
            set
            {
                this.encodingText = value;
                OnPropertyChanged("EncodingText");
            }
        }

        public string TipText { get; private set; }

        private List<WeakReference> snapshots = new List<WeakReference>();
        private ITextVersion prevVersion;

        public BufferBar(ITextBuffer textBuffer, IWpfTextViewHost textViewHost, IEditorOperations editorOperations, ITextEditorFactoryService factory)
        {
            InitializeComponent();
            DataContext = this;
            this.textBuffer = textBuffer;
            this.projBuffer = textBuffer as IProjectionBuffer;
            this.elBuffer = textBuffer as IElisionBuffer;
            this.prevVersion = textBuffer.CurrentSnapshot.Version;
            this.textView = textViewHost.TextView;
            this.textViewHost = textViewHost;

            this.factory = factory;
            this.editorOperations = editorOperations;
            this.snapshots.Add(new WeakReference(textBuffer.CurrentSnapshot));

            SolidColorBrush brush = new SolidColorBrush(textBuffer is IElisionBuffer ? Colors.LightBlue : (textBuffer is IProjectionBuffer ? Colors.LightGreen : Colors.LightGray));
            brush.Freeze();
            //Background = brush;

            ITextDataModel tdm = this.textView.TextDataModel;
            ITextViewModel tvm = this.textView.TextViewModel;

            StringBuilder tip = new StringBuilder();
            if (textBuffer == tdm.DocumentBuffer)
            {
                tip.Append("Document Buffer,");
            }
            if (textBuffer == tdm.DataBuffer)
            {
                tip.Append("Data Buffer,");
            }
            if (textBuffer == tvm.EditBuffer)
            {
                tip.Append("Edit Buffer,");
            }
            if (textBuffer == tvm.VisualBuffer)
            {
                tip.Append("Visual Buffer,");
            }
            if (tip.Length > 0)
            {
                tip.Remove(tip.Length - 1, 1);
                TipText = tip.ToString();
            }
            else
            {
                TipText = "Uncategorized Buffer";
            }

            if (this.projBuffer != null || this.elBuffer != null)
            {
                if (this.projBuffer != null)
                {
                    this.projBuffer.SourceSpansChanged += OnProjectionSourceSpansChanged;
                }
                else
                {
                    this.elBuffer.SourceSpansChanged += OnElisionSourceSpansChanged;
                }
            }
            else
            {
                this.SpansLabel.Visibility = Visibility.Collapsed;
            }

            if (this.textBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out this.document))
            {
                UpdateEncoding();
                this.document.EncodingChanged += OnEncodingChanged;
            }
            else
            {
                this.EncodingLabel.Visibility = Visibility.Collapsed;
            }

            this.textBuffer.ContentTypeChanged += OnContentTypeChanged;
            this.textBuffer.Changed += OnTextChanged;
            this.textView.Caret.PositionChanged += OnCaretPositionChanged;
            this.textView.Selection.SelectionChanged += OnSelectionChanged;

            this.SelectionLabel.Background = Brushes.Transparent;
            this.SelectionLabel.BorderBrush = Brushes.Transparent;
            this.SelectionLabel.Padding = new Thickness(3.0);
            this.SelectionCombo.Background = Brushes.Transparent;
            this.SelectionCombo.BorderBrush = Brushes.Transparent;
            this.SelectionCombo.Padding = new Thickness(3.0);
            this.SelectionPanel.Children.Add(this.SelectionLabel);
            this.selectionLabelInstalled = true;

            UpdateAll();
        }

        public void Close()
        {
            if (this.document != null)
            {
                this.document.EncodingChanged -= OnEncodingChanged;
            }
            this.textBuffer.ContentTypeChanged -= OnContentTypeChanged;
            this.textBuffer.Changed -= OnTextChanged;
            this.textView.Caret.PositionChanged -= OnCaretPositionChanged;
            this.textView.Selection.SelectionChanged -= OnSelectionChanged;
            RorUncheck(null, null);
        }

        private void OnEncodingChanged(object sender, EncodingChangedEventArgs e)
        {
            UpdateEncoding();
        }

        private void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            this.snapshots.Add(new WeakReference(e.After));
            UpdateContentType();
            UpdateVersion(e.After);
        }

        private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            this.snapshots.Add(new WeakReference(e.After));
            UpdateVersion(e.After);
            UpdatePositionAndLength(this.textView.Caret.Position);
            UpdateSelection(this.textView.Selection);
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdatePositionAndLength(e.NewPosition);
        }

        public void OnSelectionChanged(object sender, EventArgs e)
        {
            UpdateSelection(this.textView.Selection);
        }

        private void OnProjectionSourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs e)
        {
            UpdateSpans(e.After);
        }

        void OnElisionSourceSpansChanged(object sender, ElisionSourceSpansChangedEventArgs e)
        {
            UpdateSpans(e.After);
        }

        void RorCheck(object sender, RoutedEventArgs e)
        {
            ReadOnlyRegionTagger tagger;
            if (this.textBuffer.Properties.TryGetProperty<ReadOnlyRegionTagger>(typeof(ReadOnlyRegionTagger), out tagger))
            {
                tagger.IsActive = true;
            }
        }

        void RorUncheck(object sender, RoutedEventArgs e)
        {
            ReadOnlyRegionTagger tagger;
            if (this.textBuffer.Properties.TryGetProperty<ReadOnlyRegionTagger>(typeof(ReadOnlyRegionTagger), out tagger))
            {
                tagger.IsActive = false;
            }
        }

        private void UpdateContentType()
        {
            ContentTypeText = this.textBuffer.ContentType.DisplayName;
        }

        private void UpdateVersion(ITextSnapshot snapshot)
        {
            ITextVersion v = snapshot.Version;
            VersionText = "V " + v.VersionNumber.ToString();
            ReiteratedText = "R " + v.ReiteratedVersionNumber.ToString();
            this.prevVersion = v;
        }

        private void UpdateEncoding()
        {
            EncodingText = this.document.Encoding.CodePage.ToString() + ": " + this.document.Encoding.EncodingName;
        }

        private void UpdatePositionAndLength(CaretPosition pos)
        {
            SnapshotPoint? point = pos.Point.GetPoint(this.textBuffer, pos.Affinity);
            PositionText = point.HasValue ? point.Value.Position.ToString() : "-";
            LengthText = this.textBuffer.CurrentSnapshot.Length.ToString();
        }

        private void UpdateSelection(ITextSelection sel)
        {
            List<Span> spanList = new List<Span>();

            for (int s = 0; s < sel.SelectedSpans.Count; ++s)
            {
                SnapshotSpan selectedSpan = sel.SelectedSpans[s];
                IMappingSpan m = this.textView.BufferGraph.CreateMappingSpan(selectedSpan, SpanTrackingMode.EdgeExclusive);
                // todo: this should be on ITextSelection!

                NormalizedSnapshotSpanCollection spans = m.GetSpans(this.textBuffer);
                // TODO: add NormalizedSpanCollection property to NormalizedSnapshotSpanCollection
                foreach (var sp in spans)
                {
                    spanList.Add(sp.Span);
                }
            }

            this.SelectionCombo.ItemsSource = spanList;

            if (spanList.Count < 2)
            {
                if (!this.selectionLabelInstalled)
                {
                    this.SelectionPanel.Children.Clear();
                    this.SelectionPanel.Children.Add(this.SelectionLabel);
                    this.selectionLabelInstalled = true;
                }
                if (spanList.Count == 0)
                {
                    this.SelectionLabel.Content = "-";
                }
                else
                {
                    this.SelectionLabel.Content = spanList[0];
                }
            }
            else
            {
                if (this.selectionLabelInstalled)
                {
                    this.SelectionPanel.Children.Clear();
                    this.SelectionPanel.Children.Add(this.SelectionCombo);
                    this.selectionLabelInstalled = false;
                }
                this.SelectionCombo.Text = spanList[0].ToString();
            }
        }

        private void UpdateSpans(IProjectionSnapshot snapshot)
        {
            SpansText = "Spans: " + snapshot.SpanCount.ToString();
        }

        private void UpdateAll()
        {
            UpdateContentType();
            UpdateVersion(this.textBuffer.CurrentSnapshot);
            UpdatePositionAndLength(this.textView.Caret.Position);
            UpdateSelection(this.textView.Selection);
            if (this.projBuffer != null)
            {
                UpdateSpans(this.projBuffer.CurrentSnapshot);
            }
            else if (this.elBuffer != null)
            {
                UpdateSpans(this.elBuffer.CurrentSnapshot);
            }
        }

        void PropertiesButtonClick(object sender, RoutedEventArgs e)
        {
            Window win = new Window()
            {
                Content = PropertyDumper.PropertyDisplay(this.textBuffer),
                Title = "Buffer property bag",
                Height = 500,
                Width = 800
            };
            win.ShowDialog();
        }

        void SnapshotsButtonClick(object sender, RoutedEventArgs e)
        {
            Window win = new Window()
            {
                Content = DumpSnapshots(),
                Title = "Uncollected snapshots",
                Height = 100,
                Width = 500
            };
            win.ShowDialog();
        }

        void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog();
            //saveDialog.Filter = "All Files|*.*";
            //saveDialog.Title = "Save buffer contents";

            //if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(saveDialog.FileName))
            //    {
            //        this.textBuffer.CurrentSnapshot.Write(writer);
            //        writer.Close();
            //    }
            //}
        }

        void ShowButtonClick(object sender, RoutedEventArgs e)
        {
            IWpfTextView localView = this.factory.CreateTextView(this.textBuffer, this.factory.NoRoles);
            IWpfTextViewHost localHost = this.factory.CreateTextViewHost(localView, false);
            Window window = new Window();
            window.Content = localHost.HostControl;
            window.Show();
        }
#if false
        void ShowButtonClick3D(object sender, RoutedEventArgs e)
        {
            // todo: why is zooming weird when that role is supplied?

            ITextViewRoleSet roles = this.factory.CreateTextViewRoleSet("INTERACTIVE", "STRUCTURED", "DOCUMENT");
            IWpfTextView localView = this.factory.CreateTextView(this.textBuffer, roles);
            IWpfTextViewHost localHost = this.factory.CreateTextViewHost(localView, false);

            // figure out coordinates based on current view size.
            double height = this.textViewHost.HostControl.ActualHeight;
            double width = this.textViewHost.HostControl.ActualWidth;

            localHost.HostControl.Height = height;
            localHost.HostControl.Width = width;

            Point3D upperLeft3D = new Point3D(-width / 2, height / 2, 0);
            Point3D upperRight3D = new Point3D(width / 2, height / 2, 0);
            Point3D lowerLeft3D = new Point3D(-width / 2, -height / 2, 0);
            Point3D lowerRight3D = new Point3D(width / 2, -height / 2, 0);

            Viewport3D viewport = new Viewport3D();

            PerspectiveCamera camera = new PerspectiveCamera();
            camera.Position = new Point3D(0, 0, 2000);
            viewport.Camera = camera;

            Viewport2DVisual3D zanyView = new Viewport2DVisual3D();
            zanyView.Visual = localHost.HostControl;

            RotateTransform3D transform = new RotateTransform3D();
            transform.Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -60);
            zanyView.Transform = transform;

            MeshGeometry3D geo = new MeshGeometry3D();
            // vertices forming a square in the XY plane
            //geo.Positions = new Point3DCollection() { new Point3D(-1, 1, 0), new Point3D(-1, -1, 0), new Point3D(1, -1, 0), new Point3D(1, 1, 0)};
            geo.Positions = new Point3DCollection() { upperLeft3D, lowerLeft3D, lowerRight3D, upperRight3D };

            // two triangles that form the square, counterclockwise vertex list, indices into Positions
            geo.TriangleIndices = new Int32Collection() { 0, 1, 2, 0, 2, 3 };

            // these are brush coordinates
            geo.TextureCoordinates = new PointCollection() { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 0) };
            zanyView.Geometry = geo;

            DiffuseMaterial frontMaterial = new DiffuseMaterial();
            Viewport2DVisual3D.SetIsVisualHostMaterial(frontMaterial, true);
            zanyView.Material = frontMaterial;

            viewport.Children.Add(zanyView);

            ModelVisual3D model = new ModelVisual3D();
            model.Content = new AmbientLight(Colors.White); //, new Vector3D(0, 0, -20));
            viewport.Children.Add(model);

            Window window = new Window();
            Grid grid = new Grid();
            grid.Children.Add(viewport);
            window.Content = grid;
            window.Show();
        }
#endif
        private string DumpSnapshots()
        {
            System.GC.Collect(2);
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect(2);

            StringBuilder builder = new StringBuilder();
            foreach (var w in this.snapshots)
            {
                ITextSnapshot snap = w.Target as ITextSnapshot;
                if (snap != null)
                {
                    builder.Append(snap.Version.VersionNumber);
                    builder.Append(' ');
                }
            }
            for (int i = this.snapshots.Count - 1; i >= 0; --i)
            {
                if (!this.snapshots[i].IsAlive)
                {
                    this.snapshots.RemoveAt(i);
                }
            }
            return builder.ToString();
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
