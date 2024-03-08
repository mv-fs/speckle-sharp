using DUI3.Objects;
using Rhino.DocObjects;

namespace ConnectorRhinoWebUI.Objects;

public record SpeckleRhinoObject : SpeckleHostObject<RhinoObject>
{
  public override RhinoObject NativeObject { get; }
  public new string ApplicationId { get; }
  public new string SpeckleId { get; }
  public new bool IsExpired { get; private set; }

  public SpeckleRhinoObject(RhinoObject rhinoObject, string applicationId, string speckleId, bool isExpired = false)
  {
    NativeObject = rhinoObject;
    ApplicationId = rhinoObject.Id.ToString();
    SpeckleId = speckleId;
    IsExpired = isExpired;
  }
  
  public override SpeckleHostObject<RhinoObject> WithExpiredStatus(bool status = true)
  {
    return this with { IsExpired = status };
  }
}
