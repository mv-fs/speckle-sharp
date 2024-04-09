using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBEllipseToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Ellipse, ADB.Ellipse>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBEllipseToHostConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<Vector, Vector3d> vectorConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Ellipse)target);

  public ADB.Ellipse RawConvert(SOG.Ellipse target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    AG.Point3d origin = _pointConverter.RawConvert(target.plane.origin);
    AG.Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    AG.Vector3d xAxis = _vectorConverter.RawConvert(target.plane.xdir);
    AG.Vector3d majorAxis = f * target.firstRadius * xAxis.GetNormal();
    double radiusRatio = target.secondRadius / target.firstRadius;

    return new(origin, normal, majorAxis, radiusRatio, 0, 2 * Math.PI);
  }
}
