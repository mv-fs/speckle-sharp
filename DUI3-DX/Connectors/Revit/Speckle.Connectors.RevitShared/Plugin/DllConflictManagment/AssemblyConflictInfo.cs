using System.IO;
using System.Reflection;

namespace Speckle.Connectors.Revit.Plugin.DllConflictManagment;

public class AssemblyConflictInfo
{
  public AssemblyConflictInfo(AssemblyName speckleDependencyAssemblyName, Assembly conflictingAssembly)
  {
    SpeckleDependencyAssemblyName = speckleDependencyAssemblyName;
    ConflictingAssembly = conflictingAssembly;
  }

  public AssemblyName SpeckleDependencyAssemblyName { get; set; }
  public Assembly ConflictingAssembly { get; set; }

  public string GetConflictingExternalAppName() =>
    new DirectoryInfo(Path.GetDirectoryName(ConflictingAssembly.Location)).Name;
}
