using System.Collections.Generic;
using System.Linq;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.AutocadShared.ToHost.Geometry;

/// <summary>
/// POC: reconsider how to use values in the new AutocadPolycurve class.
/// DUP CODE of PolycurveToHostConverter! Only Name attribute is different.
/// </summary>
[NameAndRankValue(nameof(SOG.Autocad.AutocadPolycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class AutocadPolycurveToHostConverter : ISpeckleObjectToHostConversion
{
  private readonly IRawConversion<SOG.Polycurve, ADB.Polyline> _polylineConverter;
  private readonly IRawConversion<SOG.Polycurve, List<ADB.Entity>> _splineConverter;

  public AutocadPolycurveToHostConverter(
    IRawConversion<SOG.Polycurve, ADB.Polyline> polylineConverter,
    IRawConversion<SOG.Polycurve, List<ADB.Entity>> splineConverter
  )
  {
    _polylineConverter = polylineConverter;
    _splineConverter = splineConverter;
  }

  public object Convert(Base target)
  {
    SOG.Polycurve polycurve = (SOG.Polycurve)target;
    bool convertAsSpline = polycurve.segments.Any(s => s is not SOG.Line and not SOG.Arc);
    bool isPlanar = IsPolycurvePlanar(polycurve);

    if (convertAsSpline || !isPlanar)
    {
      return _splineConverter.RawConvert(polycurve);
    }
    else
    {
      return _polylineConverter.RawConvert(polycurve);
    }
  }

  private bool IsPolycurvePlanar(SOG.Polycurve polycurve)
  {
    double? z = null;
    foreach (var segment in polycurve.segments)
    {
      switch (segment)
      {
        case SOG.Line o:
          z ??= o.start.z;

          if (o.start.z != z || o.end.z != z)
          {
            return false;
          }

          break;
        case SOG.Arc o:
          z ??= o.startPoint.z;

          if (o.startPoint.z != z || o.midPoint.z != z || o.endPoint.z != z)
          {
            return false;
          }

          break;
        case SOG.Curve o:
          z ??= o.points[2];

          for (int i = 2; i < o.points.Count; i += 3)
          {
            if (o.points[i] != z)
            {
              return false;
            }
          }

          break;
        case SOG.Spiral o:
          z ??= o.startPoint.z;

          if (o.startPoint.z != z || o.endPoint.z != z)
          {
            return false;
          }

          break;
      }
    }
    return true;
  }
}
