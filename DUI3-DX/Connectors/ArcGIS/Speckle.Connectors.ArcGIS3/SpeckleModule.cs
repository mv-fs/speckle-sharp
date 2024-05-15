using System.IO;
using System.Reflection;
using ArcGIS.Desktop.Framework;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.ArcGIS.HostApp;
using Speckle.Core.Kits;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace Speckle.Connectors.ArcGIS;

/// <summary>
/// This sample shows how to implement pane that contains an Edge WebView2 control using the built-in ArcGIS Pro SDK's WebBrowser control.  For details on how to utilize the WebBrowser control in an add-in see here: https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework#webbrowser  For details on how to utilize the Microsoft Edge web browser control in an add-in see here: https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Framework#webbrowser-control
/// </summary>
internal sealed class SpeckleModule : Module
{
  private static SpeckleModule? s_this;

  /// <summary>
  /// Retrieve the singleton instance to this module here
  /// </summary>
  public static SpeckleModule Current =>
    s_this ??= (SpeckleModule)FrameworkApplication.FindModule("ConnectorArcGIS_Module");

  public SpeckleContainer Container { get; }

  public SpeckleModule()
  {
    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

    var builder = SpeckleContainerBuilder.CreateInstance();

    // Register Settings
    var arcgisSettings = new ArcGISSettings(HostApplications.ArcGIS, HostAppVersion.v3);

    Container = builder.LoadAutofacModules(arcgisSettings.Modules).AddSingleton(arcgisSettings).Build();
  }

  /// <summary>
  /// Called by Framework when ArcGIS Pro is closing
  /// </summary>
  /// <returns>False to prevent Pro from closing, otherwise True</returns>
  protected override bool CanUnload()
  {
    //TODO - add your business logic
    //return false to ~cancel~ Application close
    return true;
  }

  private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
  {
    // POC: dupe code
    // POC: tight binding to files
    Assembly? assembly = null;
    string name = args.Name.Split(',')[0];
    string? path = Path.GetDirectoryName(typeof(SpeckleModule).Assembly.Location);

    if (path != null)
    {
      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
      {
        assembly = Assembly.LoadFrom(assemblyFile);
      }
    }

    return assembly;
  }
}
