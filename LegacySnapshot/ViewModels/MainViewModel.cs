using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LegacySnapshot.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CounterText))]
        public partial int Count { get; set; }

        public string CounterText => Count switch
        {
            0 => "Click me",
            1 => "Clicked 1 time",
            _ => $"Clicked {Count} times"
        };

        [RelayCommand]
        private void Increment()
        {
            Count++;
        }
    }
}
