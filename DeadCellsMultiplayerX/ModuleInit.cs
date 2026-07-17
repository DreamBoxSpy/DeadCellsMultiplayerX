using System.Reflection;
using System.Runtime.CompilerServices;

namespace DeadCellsMultiplayerX;

internal static class ModuleInit
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255

    public static void Initialize()
    {
        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        foreach (var subDir in Directory.GetDirectories(modDir))
        {
            foreach (var dll in Directory.GetFiles(subDir, "*.dll"))
            {
                try
                {
                    Assembly.LoadFrom(dll);
                }
                catch{}
            }
        }
    }
}
