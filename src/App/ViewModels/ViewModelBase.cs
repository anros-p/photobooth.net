using CommunityToolkit.Mvvm.ComponentModel;

namespace Photobooth.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Raises PropertyChanged for all properties (used for language refresh).</summary>
    public void RefreshAllProperties() => OnPropertyChanged(string.Empty);
}
