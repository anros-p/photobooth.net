namespace Photobooth.App.Localisation;

/// <summary>Provides localised strings keyed by name.</summary>
public interface IStringLocalizer
{
    /// <summary>Returns the localised string for <paramref name="key"/>, or the key itself if missing.</summary>
    string this[string key] { get; }
}
