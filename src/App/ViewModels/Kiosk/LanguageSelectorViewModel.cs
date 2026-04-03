using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class LanguageSelectorViewModel : ViewModelBase
{
    private readonly LocalisationService _locService;

    [ObservableProperty]
    private IReadOnlyList<LanguageOption> _availableLanguages = [];

    [ObservableProperty]
    private string _selectedLanguage;

    public string SelectLabel => _locService["Language.Select"];
    public string CloseLabel  => _locService["Language.Close"];

    public event EventHandler? CloseRequested;

    public LanguageSelectorViewModel(LocalisationService locService)
    {
        _locService = locService;
        _selectedLanguage = locService.CurrentLanguage;
    }

    public void SetAvailableLanguages(IReadOnlyList<string> langCodes)
    {
        AvailableLanguages = langCodes.Select(c => new LanguageOption(c, DisplayName(c))).ToList();
    }

    [RelayCommand]
    private void Select(LanguageOption option)
    {
        _locService.SetLanguage(option.Code);
        SelectedLanguage = option.Code;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private static string DisplayName(string code) => code switch
    {
        "en" => "English",
        "fr" => "Français",
        "de" => "Deutsch",
        "es" => "Español",
        "nl" => "Nederlands",
        "it" => "Italiano",
        "pt" => "Português",
        _    => code.ToUpperInvariant()
    };
}

public sealed record LanguageOption(string Code, string DisplayName);
