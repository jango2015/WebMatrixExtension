using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UmbracoExtension
{
    /// <summary>
    /// Interaction logic for MacroParameters.xaml
    /// </summary>
    public partial class MacroParameters : UserControl
    {
        public string Parameter1 { get; set; }
        public int Parameter2 { get; set; }
        public int Parameter3 { get; set; }

        public MacroParameters()
        {
            InitializeComponent();
            DataContext = this;
            
            if (!this._contentLoaded)
            {
                // add controls for paramaters
                TextBox textbox1 = new TextBox();

                textbox1.Text = "Macro Param1";
                textbox1.Width = 100;

                


                TextBlock textBlock1 = new TextBlock();
                TextBlock textBlock2 = new TextBlock();

                textBlock1.TextWrapping = textBlock2.TextWrapping = TextWrapping.Wrap;
                textBlock2.Background = Brushes.AntiqueWhite;
                textBlock2.TextAlignment = TextAlignment.Center;

                textBlock1.Inlines.Add(new Bold(new Run("TextBlock")));
                textBlock1.Inlines.Add(new Run(" is designed to be "));
                textBlock1.Inlines.Add(new Italic(new Run("lightweight")));
                textBlock1.Inlines.Add(new Run(", and is geared specifically at integrating "));
                textBlock1.Inlines.Add(new Italic(new Run("small")));
                textBlock1.Inlines.Add(new Run(" portions of flow content into a UI."));

                textBlock2.Text =
                    "By default, a TextBlock provides no UI beyond simply displaying its contents.";
            }



        }
    }
}
