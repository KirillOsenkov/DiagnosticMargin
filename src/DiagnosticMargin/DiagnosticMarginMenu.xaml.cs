using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows.Media;

namespace DiagnosticMargin
{
    public partial class DiagnosticMarginMenu : Menu, IWpfTextViewMargin
    {
        public const string MarginName = "DiagnosticMarginMenu";
        internal List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> factories;
        private bool isDisposed = false;
        private bool isInitialized = false;
        private DiagnosticMargin diagnosticMargin;
        private IWpfTextViewHost textViewHost;

        public DiagnosticMarginMenu(IWpfTextViewHost textViewHost,
                                    List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> factories)
        {
            this.textViewHost = textViewHost;
            this.factories = factories;
            IsVisibleChanged += OnVisibilityChanged;
            IsMainMenu = false;
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && !this.isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            InitializeComponent();
            this.diagnosticMargin = this.textViewHost.GetTextViewMargin(DiagnosticMargin.MarginName) as DiagnosticMargin;

            MarginMenuItem outer = Items[0] as MarginMenuItem;

            if (this.diagnosticMargin == null)
            {
                Label errorHeader = new Label();
                errorHeader.Padding = new Thickness(0.0);
                errorHeader.Foreground = Brushes.Red;
                errorHeader.Content = "?";
                outer.Header = errorHeader;
                outer.ToolTip = "Error constructing Diagnostic margin";
            }
            else
            {

                for (int r = 0; r < this.factories.Count; ++r)
                {
                    MarginMenuItem item = new MarginMenuItem();
                    item.IsCheckable = true;
                    item.Header = this.factories[r].Metadata.Name;
                    item.Click += this.diagnosticMargin.PanelManagers[r].Click;
                    outer.Items.Add(item);
                }
            }
            this.isInitialized = true;
        }

        private void Close()
        {
        }

        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        #region IWpfTextViewMargin Members

        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin Members

        public double MarginSize
        {
            get { return 100.0; }
        }

        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, DiagnosticMarginMenu.MarginName, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
                Close();
            }
        }
        #endregion
    }
}
