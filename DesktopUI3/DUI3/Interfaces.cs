using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Credentials;

namespace DUI3
{
  /// <summary>
  /// Describes the most basic binding.
  /// </summary>
  public interface IBinding
  {
    /// <summary>
    /// This will be the name under which it will be available in the Frontend, e.g. 
    /// window.superBinding, or window.mapperBinding. Please use camelCase even if
    /// it hurts. 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Bindings will be wrapped by a browser specific bridge, and they will need 
    /// to use that bridge to send events to the Frontend, via <see cref="IBridge.SendToBrowser(IHostAppEvent)">SendToBrowser(IHostAppEvent)</see> or <see cref="IBridge.SendToBrowser(string)">SendToBrowser(string)</see>.
    /// </summary>
    public IBridge Parent { get; set; }

  }

  /// <summary>
  /// Describes a bridge - a wrapper class around a specific browser host.
  /// </summary>
  public interface IBridge
  {
    /// <summary>
    /// This method is called by the Frontend bridge to understand what it can actually call. It should return the method names of the bindings that this bridge wraps around.
    /// </summary>
    /// <returns></returns>
    public string[] GetBindingsMethodNames();

    /// <summary>
    /// This method is called by the Frontend bridge when invoking any of the wrapped binding's methods.
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public Task<string> RunMethod(string methodName, string args);

    /// <summary>
    /// Sends to the Frontend a IHostAppEvent. If you want to send an empty event (no data), try the SendToBrowser(string eventName).
    /// </summary>
    /// <param name="eventData"></param>
    public void SendToBrowser(IHostAppEvent eventData);

    /// <summary>
    /// Sends to the Frontend a simple notification to do something. 
    /// </summary>
    /// <param name="eventName"></param>
    public void SendToBrowser(string eventName);
  }

  public interface IHostAppEvent
  {
    public string EventName { get; set; }

    /// <summary>
    /// Note: Data needs to be serializable.
    /// </summary>
    public object Data { get; set; }
  }

  public interface IBasicConnectorBinding : IBinding
  {
    public string GetSourceApplicationName();

    public string GetSourceApplicationVersion();

    public Account[] GetAccounts();

    // ETC. 
  }

  /// <summary>
  /// Just a sketch for now.
  /// </summary>
  public interface ReceiveBindings : IBinding
  {
    public void Receive(string modelId, string projectId);

    // ETC
  }
}
