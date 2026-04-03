using System.Reflection;
using Photobooth.Drivers.Models;

namespace Photobooth.Plugins;

/// <summary>
/// Manages the lifecycle of all registered plugins and dispatches events to them.
/// Built-in plugins are registered at construction; external plugins are loaded from
/// a <c>plugins/</c> directory on disk.
/// </summary>
public sealed class PluginHost
{
    private readonly List<IPhotoboothPlugin> _plugins;

    /// <param name="builtIn">Built-in plugin instances to register immediately.</param>
    /// <param name="pluginsDirectory">
    /// Optional path to a directory containing external plugin assemblies (*.dll).
    /// Each assembly is scanned for types implementing <see cref="IPhotoboothPlugin"/>
    /// with a public parameterless constructor.
    /// </param>
    public PluginHost(IEnumerable<IPhotoboothPlugin> builtIn, string? pluginsDirectory = null)
    {
        _plugins = [.. builtIn];

        if (!string.IsNullOrEmpty(pluginsDirectory) && Directory.Exists(pluginsDirectory))
            LoadExternal(pluginsDirectory);
    }

    /// <summary>All registered plugins.</summary>
    public IReadOnlyList<IPhotoboothPlugin> Plugins => _plugins;

    /// <summary>Returns share plugins that handle one of the enabled channels.</summary>
    public IReadOnlyList<ISharePlugin> GetSharePlugins(ShareChannel enabledChannels) =>
        _plugins
            .OfType<ISharePlugin>()
            .Where(p => (p.Channel & enabledChannels) != 0)
            .ToList();

    /// <summary>
    /// Dispatches <see cref="IPhotoboothPlugin.OnSessionCompletedAsync"/> to every
    /// registered plugin concurrently. Individual plugin failures are caught and logged
    /// to <see cref="Console.Error"/> so one bad plugin does not block others.
    /// </summary>
    public async Task DispatchSessionCompletedAsync(Session session, CancellationToken ct = default)
    {
        var tasks = _plugins.Select(p => SafeInvokeAsync(p, session, ct));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Private
    // -----------------------------------------------------------------------

    private void LoadExternal(string directory)
    {
        foreach (var dll in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (!type.IsAbstract
                        && type.IsAssignableTo(typeof(IPhotoboothPlugin))
                        && type.GetConstructor(Type.EmptyTypes) is not null)
                    {
                        var instance = (IPhotoboothPlugin)Activator.CreateInstance(type)!;
                        _plugins.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PluginHost] Failed to load {dll}: {ex.Message}");
            }
        }
    }

    private static async Task SafeInvokeAsync(
        IPhotoboothPlugin plugin, Session session, CancellationToken ct)
    {
        try
        {
            await plugin.OnSessionCompletedAsync(session, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[PluginHost] Plugin '{plugin.Id}' threw on OnSessionCompletedAsync: {ex.Message}");
        }
    }
}
