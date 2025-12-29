using ReactiveUI;
using ServiceLib.Base;

namespace ServiceLib.ViewModels
{
    public class ProgressViewModel : MyReactiveObject
    {
        private string _message;
        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public void UpdateMessage(string msg)
        {
            Message = msg;
        }
    }
}
