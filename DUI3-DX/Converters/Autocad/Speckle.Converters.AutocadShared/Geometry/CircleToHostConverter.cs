using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBCircleToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Circle, ADB.Circle>
{
  private readonly IRawConversion<SOG.Circle, AG.CircularArc3d> _circleConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBCircleToHostConverter(
    IRawConversion<SOG.Circle, AG.CircularArc3d> circleConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _circleConverter = circleConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Circle)target);

  public Circle RawConvert(SOG.Circle target)
  {
    CircularArc3d circle = _circleConverter.RawConvert(target);
    return new(circle.Center, circle.Normal, circle.Radius);
  }
}

public class CircleToHostConverter : IRawConversion<SOG.Circle, CircularArc3d>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;

  public CircleToHostConverter(
    IConversionContextStack<Document, UnitsValue> contextStack,
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  public CircularArc3d RawConvert(SOG.Circle target)
  {
    Point3d center = _pointConverter.RawConvert(target.plane.origin);
    Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);

    CircularArc3d arc = new(center, normal, 0);
    if (target.radius is double targetRadius)
    {
      double radius = targetRadius * f;
      arc.Radius = radius;
    }
    return arc;
  }
}
