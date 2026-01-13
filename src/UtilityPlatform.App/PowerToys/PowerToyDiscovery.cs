using System.Reflection;
using UtilityPlatform.Core.PowerToys;

namespace UtilityPlatform.App.PowerToys;

public static class PowerToyDiscovery
{
    public static IReadOnlyList<IPowerToy> DiscoverPowerToys()
    {
        LoadAssembliesFromBaseDirectory();

        var toys = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.IsDynamic is false)
                .SelectMany(GetLoadableTypes)
                    .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IPowerToy).IsAssignableFrom(t))
                        .Select(CreateInstance)
                            .Where(t => t is not null)
                                .Cast<IPowerToy>()
                                    .OrderBy(t => t.Descriptor.DisplayName)
                                        .ToList();

        return toys;

        static void LoadAssembliesFromBaseDirectory()
        {
            var baseDirectory = AppContext.BaseDirectory;

            foreach (var assemblyPath in Directory.EnumerateFiles(baseDirectory, "UtilityPlatform*.dll"))
            {
                try
                {
                    Assembly.LoadFrom(assemblyPath);
                }
                catch
                {
                }
            }
        }

        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null)!;
            }
        }

        static IPowerToy? CreateInstance(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            return constructor is null ? null : Activator.CreateInstance(type) as IPowerToy;
        }
    }
}

