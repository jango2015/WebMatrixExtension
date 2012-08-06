using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace UmbracoExtension
{
    /// <summary>
    /// Interaction logic for Params.xaml
    /// </summary>
    public partial class Params : UserControl
    {
        public ObservableCollection<Field> Fields { get; set; }

        public Params(IEnumerable<Field> macroParams)
        {
            InitializeComponent();

            int left = 10;
            int top = 20;

            foreach (var Field in macroParams)
            {
                var lb = new TextBlock();
                lb.Text = Field.Name;
                lb.Width = Field.Length;
                Canvas.SetLeft(lb, left);
                Canvas.SetTop(lb, top);
                ParamsCanvas.Children.Add(lb);

                Binding paramBinding = new Binding("Value");

                switch (Field.DataType)
                {
                    case "String":
                    case "Int32":
                        var tb = new TextBox();
                        tb.Width = Field.Length;
                        tb.Name = Field.Alias;
                        Canvas.SetLeft(tb, left + 200);
                        Canvas.SetTop(tb, top);

                        paramBinding.Source = Field;
                        tb.SetBinding(TextBox.TextProperty, paramBinding);

                        ParamsCanvas.Children.Add(tb);
                        ParamsCanvas.RegisterName(tb.Name, tb);
                        break;

                    case "Boolean":
                        var chkb = new CheckBox();
                        chkb.Name = Field.Alias;
                        Canvas.SetLeft(chkb, left + 200);
                        Canvas.SetTop(chkb, top);

                        paramBinding.Source = Field;
                        chkb.SetBinding(CheckBox.IsCheckedProperty, paramBinding);

                        ParamsCanvas.Children.Add(chkb);
                        ParamsCanvas.RegisterName(chkb.Name, chkb);
                        break;

                    default:
                        break;
                }

                top += 30;
            }
            }
        }

        public class Field
        {
            public string Name { get; set; }
            public string Alias { get; set; }
            public string DataType { get; set; }
            public string Value { get; set; }
            public int Length { get; set; }
            public bool Required { get; set; }
        }
    }
