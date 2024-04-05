using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBArcToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Arc, ADB.Arc>
{
  private readonly IRawConversion<SOG.Arc, AG.CircularArc3d> _arcConverter;
  private readonly IRawConversion<SOG.Plane, AG.Plane> _planeConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBArcToHostConverter(
    IRawConversion<SOG.Arc, AG.CircularArc3d> arcConverter,
    IRawConversion<SOG.Plane, AG.Plane> planeConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _arcConverter = arcConverter;
    _planeConverter = planeConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Arc)target);

  public ADB.Arc RawConvert(SOG.Arc target)
  {
    // the most reliable method to convert to autocad convention is to calculate from start, end, and midpoint
    // because of different plane & start/end angle conventions
    AG.CircularArc3d circularArc = _arcConverter.RawConvert(target);

    // calculate adjusted start and end angles from circularArc reference
    AG.Plane plane = _planeConverter.RawConvert(target.plane);
    double angle = circularArc.ReferenceVector.AngleOnPlane(plane);
    double startAngle = circularArc.StartAngle + angle;
    double endAngle = circularArc.EndAngle + angle;

    return new(circularArc.Center, circularArc.Normal, circularArc.Radius, startAngle, endAngle);
  }
}

public class ArcToHostConverter : IRawConversion<SOG.Arc, AG.CircularArc3d>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;

  public ArcToHostConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Arc)target);

  public AG.CircularArc3d RawConvert(SOG.Arc target)
  {
    Point3d start = _pointConverter.RawConvert(target.startPoint);
    Point3d end = _pointConverter.RawConvert(target.endPoint);
    Point3d mid = _pointConverter.RawConvert(target.midPoint);
    CircularArc3d arc = new(start, mid, end);

    AG.Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    AG.Vector3d xdir = _vectorConverter.RawConvert(target.plane.xdir);
    arc.SetAxes(normal, xdir);

    if (target.startAngle is double startAngle && target.endAngle is double endAngle)
    {
      arc.SetAngles(startAngle, endAngle);
    }

    return arc;
  }
}
