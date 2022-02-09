﻿using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.BuiltElements.TeklaStructures;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public void ContourPlateToNative(BE.Area area)
    {
      if (!(area.outline is Polyline)) { }
      var ContourPlate = new ContourPlate();
      ToNativeContour((Polyline)area.outline, ContourPlate.Contour);
      if (area is TeklaContourPlate)
      {
        var contour = (TeklaContourPlate)area;
        ContourPlate.Name = contour.name;
        ContourPlate.Profile.ProfileString = contour.profile.name;
        ContourPlate.Finish = contour.finish;
        ContourPlate.Class = ContourPlate.Class;
        ContourPlate.Material.MaterialString = contour.material.name;
      }
      ContourPlate.Insert();
      //Model.CommitChanges();

    }
    public TeklaContourPlate ContourPlateToSpeckle(Tekla.Structures.Model.ContourPlate plate)
    {
      var specklePlate = new TeklaContourPlate();
      var units = GetUnitsFromModel();
      specklePlate.name = plate.Name;
      specklePlate.profile = GetProfile(plate.Profile.ProfileString);
      specklePlate.material = GetMaterial(plate.Material.MaterialString);
      specklePlate.finish = plate.Finish;
      specklePlate.classNumber = plate.Class;
            specklePlate.position = GetPositioning(plate.Position);

      Polygon teklaPolygon = null;
      plate.Contour.CalculatePolygon(out teklaPolygon);
      if (teklaPolygon != null)
        specklePlate.outline = ToSpecklePolyline(teklaPolygon);

            GetAllUserProperties(specklePlate, plate);

      var solid = plate.GetSolid();
      specklePlate.displayMesh = GetMeshFromSolid(solid);

      return specklePlate;
    }
        /// <summary>
        /// Create a contour plate without a display mesh for boolean parts
        /// </summary>
        /// <param name="plate"></param>
        /// <returns></returns>
        public TeklaContourPlate AntiContourPlateToSpeckle(Tekla.Structures.Model.ContourPlate plate)
        {
            var specklePlate = new TeklaContourPlate();
            specklePlate.name = plate.Name;
            specklePlate.profile = GetProfile(plate.Profile.ProfileString);
            specklePlate.material = GetMaterial(plate.Material.MaterialString);

            specklePlate.classNumber = plate.Class;
            specklePlate.position = GetPositioning(plate.Position);

            Polygon teklaPolygon = null;
            plate.Contour.CalculatePolygon(out teklaPolygon);
            if (teklaPolygon != null)
                specklePlate.outline = ToSpecklePolyline(teklaPolygon);

            var units = GetUnitsFromModel();
            specklePlate.applicationId = plate.Identifier.GUID.ToString();
            specklePlate["units"] = units;
            return specklePlate;
        }
    }

}
