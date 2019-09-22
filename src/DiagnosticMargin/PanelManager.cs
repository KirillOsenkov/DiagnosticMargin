using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DiagnosticMargin
{
    internal class PanelManager
    {
        enum State { PreActive, Active, Inactive }

        private State state;
        private DiagnosticMargin margin;
        private int rowNumber;
        private IDiagnosticPanel panel;
        private UIElement ui;

        public PanelManager(DiagnosticMargin margin, int rowNumber)
        {
            this.state = State.PreActive;
            this.margin = margin;
            this.rowNumber = rowNumber;
        }

        public void Click(object sender, RoutedEventArgs e)
        {
            switch (this.state)
            {
                case State.PreActive:
                    this.panel = this.margin.orderedFactories[this.rowNumber].Value.CreatePanel(this.margin.textViewHost);
                    this.ui = this.panel.UI;
                    Grid.SetRow(this.ui, this.rowNumber);
                    Grid.SetColumn(this.ui, 0);
                    this.margin.Children.Add(this.ui);
                    this.panel.Activate();
                    this.state = State.Active;
                    break;
                case State.Active:
                    this.panel.Inactivate();
                    this.margin.Children.Remove(this.ui);
                    this.state = State.Inactive;
                    break;
                case State.Inactive:
                    this.panel.Activate();
                    this.margin.Children.Add(this.ui);
                    this.state = State.Active;
                    break;
            }
        }
    }

}
