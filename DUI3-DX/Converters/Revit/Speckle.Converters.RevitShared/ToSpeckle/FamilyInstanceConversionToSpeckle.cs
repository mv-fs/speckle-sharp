using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: bin for now? This is also a parent child relationship and may need a pattern for this
// so we don't end up with some god FamilyInstanceConversionToSpeckle converter
[NameAndRankValue(nameof(DB.FamilyInstance), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public sealed class FamilyInstanceConversionToSpeckle : BaseConversionToSpeckle<DB.FamilyInstance, Base>
{
  private readonly IRawConversion<DB.Element, SOBR.RevitElement> _elementConverter;
  private readonly IRawConversion<DB.FamilyInstance, SOBR.RevitBeam> _beamConversion;
  private readonly IRawConversion<DB.FamilyInstance, SOBR.RevitColumn> _columnConversion;
  private readonly IRawConversion<DB.FamilyInstance, SOBR.RevitBrace> _braceConversion;

  public FamilyInstanceConversionToSpeckle(
    IRawConversion<DB.Element, SOBR.RevitElement> elementConverter,
    IRawConversion<DB.FamilyInstance, SOBR.RevitBeam> beamConversion,
    IRawConversion<DB.FamilyInstance, SOBR.RevitColumn> columnConversion,
    IRawConversion<DB.FamilyInstance, SOBR.RevitBrace> braceConversion
  )
  {
    _elementConverter = elementConverter;
    _beamConversion = beamConversion;
    _columnConversion = columnConversion;
    _braceConversion = braceConversion;
  }

  public override Base RawConvert(DB.FamilyInstance target)
  {
    return target.StructuralType switch
    {
      DB.Structure.StructuralType.Beam => _beamConversion.RawConvert(target),
      DB.Structure.StructuralType.Column => _columnConversion.RawConvert(target),
      DB.Structure.StructuralType.Brace => _braceConversion.RawConvert(target),

      // POC: return generic element conversion or throw?
      //
      //throw new SpeckleConversionException(
      //  $"No conditional converters registered that could convert object of type {target.GetType()}"
      //);
      _ => _elementConverter.RawConvert(target)
    };
  }
}
