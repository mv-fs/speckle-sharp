using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace DUI3
{

  /// <summary>
  /// Wraps a binding class, and manages its calls from the Frontend to .NET, and sending events from .NET to the the Frontend. 
  /// <para>See also: https://github.com/johot/WebView2-better-bridge</para>
  /// </summary>
  /// <typeparam name="TBrowser">The browser type (CefSharp or WebView2 currently supported.)</typeparam>
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public class BrowserBridge : IBridge
  {
    /// <summary>
    /// The name under which we expect the frontend to hoist this bindings class to the global scope.
    /// e.g., `receiveBindings` should be available as `window.receiveBindings`. 
    /// </summary>
    public string FrontendBoundName { get; }

    public object Browser { get; }

    public IBinding Binding { get; }

    public Action<string> ExecuteScriptAsync { get; set; }
    public Action ShowDevToolsAction { get; set; }

    private Type BindingType { get; set; }
    private Dictionary<string, MethodInfo> BindingMethodCache { get; set; }

    /// <summary>
    /// Creates a new bridge.
    /// </summary>
    /// <param name="browser">The host browser instance.</param>
    /// <param name="binding">The actual binding class.</param>
    /// <param name="executeScriptAsync">A simple action that does the browser's version of executeScriptAsync(string).</param>
    public BrowserBridge(object browser, IBinding binding, Action<string> executeScriptAsync, Action showDevToolsAction)
    {
      FrontendBoundName = binding.Name;
      Browser = browser;
      Binding = binding;
      
      BindingType = Binding.GetType(); 
      BindingMethodCache = new Dictionary<string, MethodInfo>();
      // Note: we need to filter out getter and setter methods here because they are not really nicely
      // supported across browsers, hence the !method.IsSpecialName. 
      foreach(var m in BindingType.GetMethods().Where(method => !method.IsSpecialName))
      {
        BindingMethodCache[m.Name] = m;
      }

      Binding.Parent = this;

      ExecuteScriptAsync = executeScriptAsync;
      ShowDevToolsAction = showDevToolsAction;
    }

    /// <summary>
    /// Used by the Frontend bridge logic to understand which methods are available.
    /// </summary>
    /// <returns></returns>
    public string[] GetBindingsMethodNames() => BindingMethodCache.Keys.ToArray();

    /// <summary>
    /// Used by the Frontend brdige to call into .NET.
    /// TODO: Check and test
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public async Task<string> RunMethod(string methodName, string args)
    {
      if (!BindingMethodCache.ContainsKey(methodName))
        throw new SpeckleException($"Cannot find method {methodName} in bindings class {BindingType.AssemblyQualifiedName}.");

      var method = BindingMethodCache[methodName];
      var parameters = method.GetParameters();
      var jsonArgsArray = JsonSerializer.Deserialize<string[]>(args);

      if (parameters.Length != jsonArgsArray.Length)
        throw new SpeckleException($"Wrong number of arguments when invoking binding function {methodName}, expected {parameters.Length}, but got {jsonArgsArray.Length}.");

      var typedArgs = new object[jsonArgsArray.Length];

      for (int i = 0; i < typedArgs.Length; i++)
      {
        var typedObj = JsonSerializer.Deserialize(jsonArgsArray[i], parameters[i].ParameterType);
        typedArgs[i] = typedObj;
      }
      var resultTyped = method.Invoke(Binding, typedArgs);

      // Was it an async method (in bridgeClass?)
      var resultTypedTask = resultTyped as Task;

      string resultJson;

      // Was the method called async?
      if (resultTypedTask == null)
      {
        // Regular method: no need to await things
        resultJson = JsonSerializer.Serialize(resultTyped);
      }
      else // It's an async call
      {
        await resultTypedTask;

        // If has a "Result" property return the value otherwise null (Task<void> etc)
        var resultProperty = resultTypedTask.GetType().GetProperty("Result");
        var taskResult = resultProperty != null ? resultProperty.GetValue(resultTypedTask) : null;
        resultJson = JsonSerializer.Serialize(taskResult);
      }

      return resultJson;
    }

    /// <summary>
    /// Notifies the Frontend about something by doing the browser specific way for `browser.ExecuteScriptAsync("window.FrontendBoundName.on(eventName, etc.)")`. 
    /// </summary>
    /// <param name="eventData"></param>
    public void SendToBrowser(string eventName, object data = null)
    {
      string script;
      if (data != null)
      {
        var payload = JsonSerializer.Serialize(data);
        script = $"{FrontendBoundName}.emit('{eventName}', '{payload}')";
      } 
      else
      {
        script = $"{FrontendBoundName}.emit('{eventName}')";
      }
      ExecuteScriptAsync(script);
    }

    /// <summary>
    /// Shows the dev tools
    /// </summary>
    public void ShowDevTools()
    {
      ShowDevToolsAction();
    }
  }

}
