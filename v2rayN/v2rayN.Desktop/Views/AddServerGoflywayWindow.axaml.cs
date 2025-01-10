using ReactiveUI;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;

namespace v2rayN.Desktop.Views
{
    public partial class AddServerGoflywayWindow : ReactiveWindow<AddServerGoflywayViewModel>
    {
        public AddServerGoflywayWindow()
        {
            InitializeComponent();
        }

        public AddServerGoflywayWindow(ProfileItem profileItem)
        {
            InitializeComponent();

            this.Loaded += Window_Loaded;
            btnCancel.Click += (s, e) => this.Close();

            if (profileItem.IndexId.IsNullOrEmpty())
            {
                profileItem.ConfigType = EConfigType.Goflyway;
            }

            ViewModel = new AddServerGoflywayViewModel(profileItem, UpdateViewHandler);

            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.Address, v => v.txtAddress.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.Port, v => v.txtPort.Text).DisposeWith(disposables);
                this.Bind(ViewModel, vm => vm.SelectedSource.Id, v => v.txtId3.Text).DisposeWith(disposables);
                this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);
            });

            this.Title = $"{profileItem.ConfigType}";
        }

        private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
        {
            switch (action)
            {
                case EViewAction.CloseWindow:
                    this.Close(true);
                    break;
            }
            return await Task.FromResult(true);
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            txtRemarks.Focus();
        }

    }
}