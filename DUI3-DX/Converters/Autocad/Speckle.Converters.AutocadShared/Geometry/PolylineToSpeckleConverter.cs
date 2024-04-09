using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(ADB.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBPolylineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<ADB.Polyline, SOG.Polyline>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBPolylineToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline)target);

  public SOG.Polyline RawConvert(ADB.Polyline target)
  {
    List<double> points = new();
    for (int i = 0; i < target.NumberOfVertices; i++)
    {
      points.AddRange(_pointConverter.RawConvert(target.GetPoint3dAt(i)).ToList());
    }

    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Polyline polyline =
      new(points, _contextStack.Current.SpeckleUnits)
      {
        closed = target.Closed || target.StartPoint.Equals(target.EndPoint),
        length = target.Length,
        bbox = bbox
      };
    return polyline;
  }
}

[NameAndRankValue(nameof(ADB.Polyline3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBPolyline3dToSpeckleConverter
  : IHostObjectToSpeckleConversion,
    IRawConversion<ADB.Polyline3d, SOG.Polyline>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, UnitsValue> _contextStack;

  public DBPolyline3dToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Polyline3d)target);

  public SOG.Polyline RawConvert(ADB.Polyline3d target)
  {
    List<double> points = new();

    // if this polyline is a new object, retrieve approximate vertices from spline nurbs data
    // (should only be used for curve display value so far)
    if (target.IsNewObject)
    {
      foreach (Point3d vertex in target.Spline.NurbsData.GetControlPoints())
      {
        points.AddRange(_pointConverter.RawConvert(vertex).ToList());
      }
    }
    // otherwise retrieve actual vertices from transaction
    else
    {
      // get the transaction for reading the target
      // TODO: This should be injected or part of the context stack!!
      Transaction tr = _contextStack.Current.Document.TransactionManager.TopTransaction;
      if (tr == null)
      {
        tr = _contextStack.Current.Document.TransactionManager.StartTransaction();
      }

      foreach (ObjectId id in target)
      {
        var vertex = (PolylineVertex3d)tr.GetObject(id, OpenMode.ForRead);
        points.AddRange(_pointConverter.RawConvert(vertex.Position).ToList());
      }
    }

    SOG.Box bbox = _boxConverter.RawConvert(target.GeometricExtents);

    SOG.Polyline polyline =
      new(points, _contextStack.Current.SpeckleUnits)
      {
        closed = target.Closed || target.StartPoint.Equals(target.EndPoint),
        length = target.Length,
        bbox = bbox
      };

    return polyline;
  }
}
