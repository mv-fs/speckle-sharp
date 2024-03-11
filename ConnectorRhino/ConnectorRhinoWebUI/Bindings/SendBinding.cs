using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConnectorRhinoWebUI.Utils;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Models.Card;
using DUI3.Operations;
using DUI3.Settings;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Kits;

namespace ConnectorRhinoWebUI.Bindings;

public class SendBinding : ISendBinding, ICancelable
{
  public string Name { get; set; } = "sendBinding";
  public IBridge Parent { get; set; }
  private readonly DocumentModelStore _store;
  public CancellationManager CancellationManager { get; } = new();

  private HashSet<string> ChangedObjectIds { get; set; } = new();

  public SendBinding(DocumentModelStore store)
  {
    _store = store;
    
    RhinoDoc.LayerTableEvent += (_, _) =>
    {
      SendBindingUiCommands.RefreshSendFilters(Parent);
    };

    RhinoDoc.AddRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.DeleteRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.ObjectId.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };

    RhinoDoc.ReplaceRhinoObject += (_, e) =>
    {
      // NOTE: This does not work if rhino starts and opens a blank doc;
      if (!_store.IsDocumentInit)
      {
        return;
      }

      ChangedObjectIds.Add(e.NewRhinoObject.Id.ToString());
      ChangedObjectIds.Add(e.OldRhinoObject.Id.ToString());
      RhinoIdleManager.SubscribeToIdle(RunExpirationChecks);
    };
  }

  public List<ISendFilter> GetSendFilters()
  {
    return new List<ISendFilter>()
    {
      new RhinoEverythingFilter(), 
      new RhinoSelectionFilter() { IsDefault = true }, 
      new RhinoLayerFilter()
    };
  }

  public List<CardSetting> GetSendSettings()
  {
    return new List<CardSetting>()
    {
      new()
      {
        Id = "includeAttributes",
        Title = "Include Attributes",
        Value = true,
        Type = "boolean"
      },
    };
  }

  private readonly Dictionary<string, ObjectReference> _convertedObjectReferences = new();
  
  public async void Send(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get model

      if (_store.GetModelById(modelCardId) is not SenderModelCard model)
      {
        throw new InvalidOperationException("No publish model card was found.");
      }
      
      // 2 - Check account exist
      Account account = Accounts.GetAccount(model.AccountId);

      // 3 - Get elements to convert, throw early if nothing is selected
      List<RhinoObject> rhinoObjects = GetObjectsFromDocument(model);

      if (rhinoObjects.Count == 0)
      {
        throw new InvalidOperationException("No objects were found. Please update your send filter!");
      }

      // 4 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(RhinoDoc.ActiveDoc, "Rhino7");

      // 5 - Convert objects
      var transport = new ServerTransport(account, model.ProjectId);
      List<ITransport> transports = new() { transport };
      
      Base commitObject = await ConvertObjects(rhinoObjects, converter, modelCardId, cts, transport).ConfigureAwait(true);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }
      
      // 7 - Serialize and Send objects
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Uploading..." });
      //.Send(converted, transport, true, null, cts.Token)
      var sendResult = await Speckle.Core.Api.Operations
        .Send(commitObject, transport, true, null, true, cts.Token)
        .ConfigureAwait(true);

      foreach (var kvp in sendResult.convertedReferences)
      {
        _convertedObjectReferences[kvp.Key] = kvp.Value;
      }
      
      ChangedObjectIds = new HashSet<string>();
      
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress { Status = "Linking version to model..." });
      
      // 8 - Create the version (commit)
      var apiClient = new Client(account);
      string versionId = await apiClient.CommitCreate(new CommitCreateInput()
      {
        streamId = model.ProjectId, branchName = model.ModelId, sourceApplication = "Rhino", objectId = sendResult.rootObjId
      }, cts.Token).ConfigureAwait(true);
      
      SendBindingUiCommands.SetModelCreatedVersionId(Parent, modelCardId, versionId);
      apiClient.Dispose();
    }
#pragma warning disable CA1031
    catch (Exception e)
#pragma warning restore CA1031
    {
      if (e is OperationCanceledException) // We do not want to display an error, we just stop sending.
      {
        return;
      }

      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId, e);
    }
  }

  public void CancelSend(string modelCardId) => CancellationManager.CancelOperation(modelCardId);
  
  private async Task<Base> ConvertObjects(List<RhinoObject> rhinoObjects, ISpeckleConverter converter, string modelCardId, CancellationTokenSource cts, ITransport transport)
  {
    var modelWithLayers = new Collection { name = RhinoDoc.ActiveDoc.Name };
    int count = 0;
    
    Dictionary<int, Collection> layerCollectionCache = new();
    
    foreach (RhinoObject rhinoObject in rhinoObjects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 1. get object layer
      var layer = RhinoDoc.ActiveDoc.Layers[rhinoObject.Attributes.LayerIndex];
      
      // 2. get or create a nested collection for it
      var collectionHost = GetAndCreateObjectHostCollection(layerCollectionCache, layer, modelWithLayers);
      var applicationId = rhinoObject.Id.ToString();
      
      // 3. get from cache or convert
      Base converted;
      if (!ChangedObjectIds.Contains(applicationId) && _convertedObjectReferences.TryGetValue(applicationId, out ObjectReference value))
      {
        converted = value;
      }
      else
      {
        converted = converter.ConvertToSpeckle(rhinoObject);
        converted.applicationId = applicationId;
      }
      
      // 4. add to host
      collectionHost.elements.Add(converted);
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress(){ Status = "Converting", Progress = (double)++count / rhinoObjects.Count});
      
      // NOTE: useful for testing ui states, pls keep for now so we can easily uncomment 
      // Thread.Sleep(550); 
    }
    
    // 5. profit
    return modelWithLayers;
  }

  private Collection GetAndCreateObjectHostCollection(Dictionary<int, Collection> layerCollectionCache, Layer layer, Collection modelWithLayers)
  {
    if (layerCollectionCache.TryGetValue(layer.Index, out Collection value))
    {
      return value;
    }
    
    var names = layer.FullPath.Split(new[] {Layer.PathSeparator}, StringSplitOptions.None);
    var path = names[0];
    var index = 0;
    var previousCollection = modelWithLayers;
    foreach (var layerName in names)
    {
      var existingLayerIndex = RhinoDoc.ActiveDoc.Layers.FindByFullPath(path, -1);
      Collection childCollection = null;
      if (layerCollectionCache.ContainsKey(existingLayerIndex))
      {
        childCollection = layerCollectionCache[existingLayerIndex];
      }
      else
      {
        childCollection = new Collection(layerName, "layer")
        {
          applicationId = RhinoDoc.ActiveDoc.Layers[existingLayerIndex].Id.ToString()
        };
        previousCollection.elements.Add(childCollection);
        layerCollectionCache[existingLayerIndex] = childCollection;
      }
          
      previousCollection = childCollection;
          
      if (index < names.Length - 1)
      {
        path += Layer.PathSeparator + names[index+1];
      }
      index++;
    }
    
    layerCollectionCache[layer.Index] = previousCollection;
    return previousCollection;
    // var collectionHost = modelLayers.Traverse(m => m is not Collection).FirstOrDefault(obj => (obj as Collection)?.name == layer.Name) as Collection; // works, but it's better with a cache
  }

  private List<RhinoObject> GetObjectsFromDocument(SenderModelCard model)
  {
    List<string> objectsIds = model.SendFilter.GetObjectIds();
    return objectsIds.Select((id) => RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id))).Where(obj => obj!=null).ToList();
  }
  
  private void RunExpirationChecks()
  {
    List<SenderModelCard> senders = _store.GetSenders();
    string[] objectIdsList = ChangedObjectIds.ToArray();
    List<string> expiredSenderIds = new();

    foreach (SenderModelCard sender in senders)
    {
      bool isExpired = sender.SendFilter.CheckExpiry(objectIdsList);
      if (isExpired)
      {
        expiredSenderIds.Add(sender.ModelCardId);
      }
    }
    SendBindingUiCommands.SetModelsExpired(Parent, expiredSenderIds);
    // ChangedObjectIds = new HashSet<string>();
  }
}
