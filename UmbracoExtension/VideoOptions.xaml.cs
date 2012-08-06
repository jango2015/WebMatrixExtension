using System.Windows.Controls;

namespace UmbracoExtension
{
    public partial class VideoOptions : UserControl
    {
        public string VideoFile { get; set; }
        public int VideoWidth { get; set; }
        public int VideoHeight { get; set; }

        public VideoOptions()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
