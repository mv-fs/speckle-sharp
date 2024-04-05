using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(DBPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBPointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<DBPoint, SOG.Point>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;

  public DBPointToSpeckleConverter(IRawConversion<AG.Point3d, SOG.Point> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((DBPoint)target);

  public SOG.Point RawConvert(DBPoint target) => _pointConverter.RawConvert(target.Position);
}

public class PointToSpeckleConverter : IRawConversion<AG.Point3d, SOG.Point>
{
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<Document, UnitsValue> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((AG.Point3d)target);

  public SOG.Point RawConvert(AG.Point3d target) =>
    new(target.X, target.Y, target.Z, _contextStack.Current.SpeckleUnits);
}
