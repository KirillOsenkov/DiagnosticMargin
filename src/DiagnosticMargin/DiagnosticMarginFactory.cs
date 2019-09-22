using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace DiagnosticMargin
{
    public interface IDiagnosticPanelFactoryMetadataView : IOrderable
    {
        IEnumerable<string> ContentTypes { get; }
        IEnumerable<string> TextViewRoles { get; }
    }

    /// <summary>
    /// Defines the option to enable the diagnostic margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DiagnosticMarginFactory.DiagnosticMarginName)]
    public sealed class DiagnosticMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default => false;

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key => DiagnosticMarginFactory.DiagnosticMarginId;
    }

    [Export]
    internal sealed class PanelState : IPartImportsSatisfiedNotification
    {
        [ImportMany]
        List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> PanelFactories { get; set; }

        private IList<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> orderedPanelFactories;

        public void OnImportsSatisfied()
        {
            this.orderedPanelFactories = Orderer.Order(PanelFactories);
        }

        public List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>> GetApplicablePanels(IContentType contentType, ITextViewRoleSet viewRoles)
        {
            var applicableFactories = new List<Lazy<IDiagnosticPanelFactory, IDiagnosticPanelFactoryMetadataView>>();

            foreach (var factoryHandle in this.orderedPanelFactories)
            {
                bool contentTypeMatch = false;
                foreach (string contentTypeName in factoryHandle.Metadata.ContentTypes)
                {
                    if (contentType.IsOfType(contentTypeName))
                    {
                        contentTypeMatch = true;
                        break;
                    }
                }

                if (contentTypeMatch)
                {
                    if (viewRoles.ContainsAny(factoryHandle.Metadata.TextViewRoles))
                    {
                        applicableFactories.Add(factoryHandle);
                    }
                }
            }
            return applicableFactories;
        }
    }

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(DiagnosticMargin.MarginName)]
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [Order(After = PredefinedMarginNames.HorizontalScrollBarContainer)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [DeferCreation(OptionName = DiagnosticMarginName)]
    internal sealed class DiagnosticMarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        PanelState PanelState { get; set; }

        /// <summary>
        /// Determines whether to have a diagnostic margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> DiagnosticMarginId = new EditorOptionKey<bool>(DiagnosticMarginName);
        public const string DiagnosticMarginName = "TextViewHost/DiagnosticMargin";

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            return new DiagnosticMargin(textViewHost, PanelState.GetApplicablePanels(textViewHost.TextView.TextDataModel.ContentType,
                                                                                     textViewHost.TextView.Roles));
        }
    }

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(DiagnosticMarginMenu.MarginName)]
    [Order(Before = PredefinedMarginNames.HorizontalScrollBarContainer, After = "ZoomControlMarginProvider" )]
    [MarginContainer(PredefinedMarginNames.BottomControl)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [DeferCreation(OptionName = DiagnosticMarginFactory.DiagnosticMarginName)]
    internal sealed class DiagnosticMarginMenuFactory : IWpfTextViewMarginProvider
    {
        [Import]
        PanelState PanelState { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            return new DiagnosticMarginMenu(textViewHost, PanelState.GetApplicablePanels(textViewHost.TextView.TextDataModel.ContentType,
                                                                                         textViewHost.TextView.Roles));
        }
    }
}
