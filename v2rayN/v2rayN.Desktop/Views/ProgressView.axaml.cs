using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace v2rayN.Desktop.Views
{
    public partial class ProgressView : UserControl
    {
        public ProgressView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
