using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace DiagnosticMargin
{
    public partial class DiagnosticMargin : Grid, IWpfTextViewMargin
    {
        public const string MarginName = "Diagnostic";

        internal List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> orderedFactories;
        internal readonly IWpfTextViewHost textViewHost;
        internal PanelManager[] PanelManagers { get; private set; }

        private RowDefinition[] panelRowDefinitions;
        private bool isDisposed = false;
        private bool isInitialized = false;

        public DiagnosticMargin(IWpfTextViewHost textViewHost,
                                List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> orderedFactories)
        {
            this.textViewHost = textViewHost;
            this.orderedFactories = orderedFactories;
            PanelManagers = new PanelManager[this.orderedFactories.Count];
            for (int r = 0; r < this.orderedFactories.Count; ++r)
            {
                PanelManagers[r] = new PanelManager(this, r);
            }
            IsVisibleChanged += OnVisibilityChanged;
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
            this.panelRowDefinitions = new RowDefinition[this.orderedFactories.Count];
            for (int r = 0; r < this.orderedFactories.Count; ++r)
            {
                this.panelRowDefinitions[r] = new RowDefinition();
                this.panelRowDefinitions[r].Height = new GridLength(0, GridUnitType.Auto);
                RowDefinitions.Add(this.panelRowDefinitions[r]);

                PanelManagers[r] = new PanelManager(this, r);
            }

            Background = new SolidColorBrush(Colors.GreenYellow);
            this.isInitialized = true;
        }

        private void Close()
        {
        }

        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(MarginName);
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
            return string.Compare(marginName, DiagnosticMargin.MarginName, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
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
