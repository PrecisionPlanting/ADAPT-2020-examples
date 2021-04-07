using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.FieldBoundaries;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using AgGateway.ADAPT.ApplicationDataModel.Prescriptions;
using AgGateway.ADAPT.ApplicationDataModel.Products;
using AgGateway.ADAPT.ApplicationDataModel.Representations;
using AgGateway.ADAPT.ApplicationDataModel.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using RepresentationInstance = AgGateway.ADAPT.Representation.RepresentationSystem;
using UnitInstance = AgGateway.ADAPT.Representation.UnitSystem;
using AgGateway.ADAPT.Representation.RepresentationSystem.ExtensionMethods;

namespace PrecisionPlanting.ADAPT._2020.Examples
{
    /// <summary>
    /// This class provides an example of how to create 
    /// 1. A Setup file with GFF, Crop, Product information
    /// 2. Prescriptions for seed rate, seed depth, liquid rate and granular rate.
    /// </summary>
    public class ExportData
    {
        public static void Export(string path)
        {
            ApplicationDataModel adm = new ApplicationDataModel();
            adm.Catalog = new Catalog();
            adm.Documents = new Documents();

            //--------------------------
            //Setup information
            //--------------------------
            //Add a crop
            Crop corn = new Crop() { Name = "Corn" };
            adm.Catalog.Crops.Add(corn);

            //Add some seed varieties
            CropVarietyProduct seedVariety1 = new CropVarietyProduct() { CropId = corn.Id.ReferenceId, Description = "Variety 1" };
            CropVarietyProduct seedVariety2 = new CropVarietyProduct() { CropId = corn.Id.ReferenceId, Description = "Variety 2" };
            adm.Catalog.Products.Add(seedVariety1);
            adm.Catalog.Products.Add(seedVariety2);

            //Add a liquid product
            CropNutritionProduct fertilizer = new CropNutritionProduct() { Description = "Starter", Form = ProductFormEnum.Liquid };
            fertilizer.ProductType = ProductTypeEnum.Fertilizer;
            adm.Catalog.Products.Add(fertilizer);

            //Add a granular product
            CropProtectionProduct insecticide = new CropProtectionProduct() { Description = "Insecticide", Form = ProductFormEnum.Solid };
            insecticide.ProductType = ProductTypeEnum.Chemical;
            adm.Catalog.Products.Add(insecticide);

            //GFF
            Grower grower = new Grower() { Name = "Example Grower" };
            adm.Catalog.Growers.Add(grower);

            Farm farm = new Farm() { Description = "Example Farm", GrowerId = grower.Id.ReferenceId };
            adm.Catalog.Farms.Add(farm);

            Field field = new Field() { Description = "Example Field", FarmId = farm.Id.ReferenceId, GrowerId = grower.Id.ReferenceId };
            field.Area = GetNumericRepresentationValue(23d, "ha", "vrReportedFieldArea");
            adm.Catalog.Fields.Add(field);

            //Crop zone
            TimeScope season = new TimeScope() { DateContext = DateContextEnum.CropSeason, TimeStamp1 = new DateTime(2021, 1, 1) };
            CropZone cropZone = new CropZone() { CropId = corn.Id.ReferenceId, FieldId = field.Id.ReferenceId, TimeScopes = new List<TimeScope>() { season } };
            adm.Catalog.CropZones.Add(cropZone);

            //Field boundary 
            FieldBoundary boundary = new FieldBoundary() 
            { 
                SpatialData = new MultiPolygon() 
                { 
                    Polygons = new List<Polygon>() 
                    {
                         new Polygon() 
                         {
                             ExteriorRing = new LinearRing()
                             { 
                                 Points = new List<Point>()
                                 {
                                    new Point() { X = -89.488565, Y = 40.478304 },
                                    new Point() { X = -89.485439, Y = 40.478304 },
                                    new Point() { X = -89.485439, Y = 40.475010 },
                                    new Point() { X = -89.488565, Y = 40.475010 }
                                 }
                             },
                             InteriorRings = new List<LinearRing>()
                             {
                                new LinearRing()
                                {
                                    Points = new List<Point>()
                                    {
                                        new Point() { X = -89.487719, Y = 40.478091 },
                                        new Point() { X = -89.487536, Y = 40.478091 },
                                        new Point() { X = -89.487536, Y = 40.477960 },
                                        new Point() { X = -89.487719, Y = 40.477960 },
                                        new Point() { X = -89.487719, Y = 40.478091 }
                                    }
                                },
                                new LinearRing()
                                {
                                    Points = new List<Point>()
                                    {
                                        new Point() { X = -89.486732, Y = 40.478172 },
                                        new Point() { X = -89.486453, Y = 40.478172 },
                                        new Point() { X = -89.486453, Y = 40.478082 },
                                        new Point() { X = -89.486732, Y = 40.478082 },
                                        new Point() { X = -89.486732, Y = 40.478172 }
                                    }
                                }
                             }
                         }
                    } 
                },
                FieldId = field.Id.ReferenceId
            };
            adm.Catalog.FieldBoundaries.Add(boundary);
            field.ActiveBoundaryId = boundary.Id.ReferenceId;

            //--------------------------
            //Prescription
            //--------------------------

            //Prescription setup data
            //Setup the representation and units for seed rate & seed depth prescriptions
            NumericRepresentation seedRate = GetNumericRepresentation("vrSeedRateSeedsTarget");
            UnitOfMeasure seedUOM = UnitInstance.UnitSystemManager.GetUnitOfMeasure("seeds1ac-1");
            RxProductLookup seedVariety1RateLookup = new RxProductLookup() { ProductId = seedVariety1.Id.ReferenceId, Representation = seedRate, UnitOfMeasure = seedUOM };
            RxProductLookup seedVariety2RateLookup = new RxProductLookup() { ProductId = seedVariety2.Id.ReferenceId, Representation = seedRate, UnitOfMeasure = seedUOM };

            NumericRepresentation seedDepth = GetNumericRepresentation("vrSeedDepthTarget");
            UnitOfMeasure depthUOM = UnitInstance.UnitSystemManager.GetUnitOfMeasure("cm");
            RxProductLookup seedVariety1DepthLookup = new RxProductLookup() { ProductId = seedVariety1.Id.ReferenceId, Representation = seedDepth, UnitOfMeasure = depthUOM };
            RxProductLookup seedVariety2DepthLookup = new RxProductLookup() { ProductId = seedVariety2.Id.ReferenceId, Representation = seedDepth, UnitOfMeasure = depthUOM };

            //Setup liquid rx representation/units
            NumericRepresentation fertilizerRate = GetNumericRepresentation("vrAppRateVolumeTarget");
            UnitOfMeasure fertilizerUOM = UnitInstance.UnitSystemManager.GetUnitOfMeasure("gal1ac-1");
            RxProductLookup fertilizerRateLookup = new RxProductLookup() { ProductId = fertilizer.Id.ReferenceId, Representation = fertilizerRate, UnitOfMeasure = fertilizerUOM };

            //Setup granular rx representation/units
            NumericRepresentation insecticideRate = GetNumericRepresentation("vrAppRateMassTarget");
            UnitOfMeasure insecticideUOM = UnitInstance.UnitSystemManager.GetUnitOfMeasure("lb1ac-1");
            RxProductLookup insecticideRateLookup = new RxProductLookup() { ProductId = insecticide.Id.ReferenceId, Representation = insecticideRate, UnitOfMeasure = insecticideUOM };


            //Prescription zones
            //Zone 1 - Variety 1 at 32000 seeds/acre, 4 cm depth target; Starter at 7 gal/ac; Insecticide at 5 lb/ac
            RxShapeLookup zone1 = new RxShapeLookup()
            {
                Rates = new List<RxRate>()
                {
                    new RxRate()
                    {
                        Rate = 32000d,
                        RxProductLookupId = seedVariety1RateLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 4d,
                        RxProductLookupId = seedVariety1DepthLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 7d,
                        RxProductLookupId = fertilizerRateLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 5d,
                        RxProductLookupId = insecticideRateLookup.Id.ReferenceId
                    }
                },
                Shape = new MultiPolygon()
                {
                    Polygons = new List<Polygon>()
                    {
                        new Polygon()
                        {
                            ExteriorRing = new LinearRing()
                            {
                                Points = new List<Point>()
                                {
                                     new Point() { X = -89.488565, Y = 40.478304 },
                                     new Point() { X = -89.485439, Y = 40.478304 },
                                     new Point() { X = -89.485439, Y = 40.477404 },
                                     new Point() { X = -89.488565, Y = 40.477756 },
                                     new Point() { X = -89.488565, Y = 40.478304 }
                                }
                            },
                            InteriorRings = new List<LinearRing>()
                            {
                                new LinearRing()
                                {
                                    Points = new List<Point>()
                                    {
                                        new Point() { X = -89.487719, Y = 40.478091 },
                                        new Point() { X = -89.487536, Y = 40.478091 },
                                        new Point() { X = -89.487536, Y = 40.477960 },
                                        new Point() { X = -89.487719, Y = 40.477960 },
                                        new Point() { X = -89.487719, Y = 40.478091 }
                                    }
                                },
                                new LinearRing()
                                {
                                    Points = new List<Point>()
                                    {
                                        new Point() { X = -89.486732, Y = 40.478172 },
                                        new Point() { X = -89.486453, Y = 40.478172 },
                                        new Point() { X = -89.486453, Y = 40.478082 },
                                        new Point() { X = -89.486732, Y = 40.478082 },
                                        new Point() { X = -89.486732, Y = 40.478172 }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            //Zone 2 - Variety 1 at 34000 seeds/acre, depth target 5cm; Starter at 4 gal/ac; Insecticide at 2.5 lb/ac
            RxShapeLookup zone2 = new RxShapeLookup()
            {
                Rates = new List<RxRate>()
                {
                    new RxRate()
                    {
                        Rate = 34000d,
                        RxProductLookupId = seedVariety1RateLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 5d,
                        RxProductLookupId = seedVariety1DepthLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 4d,
                        RxProductLookupId = fertilizerRateLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 2.5,
                        RxProductLookupId = insecticideRateLookup.Id.ReferenceId
                    }
                },
                Shape = new MultiPolygon()
                {
                    Polygons = new List<Polygon>()
                    {
                        new Polygon()
                        {
                            ExteriorRing = new LinearRing()
                            {
                                Points = new List<Point>()
                                {
                                    new Point() { X = -89.488565, Y = 40.477756 },
                                    new Point() { X = -89.485439, Y = 40.477404 },
                                    new Point() { X = -89.485439, Y = 40.476688 },
                                    new Point() { X = -89.488565, Y = 40.476688 },
                                    new Point() { X = -89.488565, Y = 40.477756 }
                                }
                            }
                        }
                    }
                }
            };

            //Zone 3 - Variety 2 at 29000 seeds/acre, depth target 6 cm; Starter at 6 gal/ac ; Insecticide at 2.75 lb/ac
            RxShapeLookup zone3 = new RxShapeLookup()
            {
                Rates = new List<RxRate>()
                {
                    new RxRate()
                    {
                        Rate = 29000d,
                        RxProductLookupId = seedVariety2RateLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 6d,
                        RxProductLookupId = seedVariety2DepthLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 6d,
                        RxProductLookupId = fertilizerRateLookup.Id.ReferenceId
                    },
                    new RxRate()
                    {
                        Rate = 2.75,
                        RxProductLookupId = insecticideRateLookup.Id.ReferenceId
                    }
                },
                Shape = new MultiPolygon()
                {
                    Polygons = new List<Polygon>()
                    {
                        new Polygon()
                        {
                            ExteriorRing = new LinearRing()
                            {
                                Points = new List<Point>()
                                {
                                    new Point() { X = -89.488565, Y = 40.476688 },
                                    new Point() { X = -89.485439, Y = 40.476688 },
                                    new Point() { X = -89.485439, Y = 40.475010 },
                                    new Point() { X = -89.488565, Y = 40.475010 },
                                    new Point() { X = -89.488565, Y = 40.476688 }
                                }
                            }
                        }
                    }
                }
            };

            //Assembled Rx
            VectorPrescription vectorPrescription = new VectorPrescription()
            {
                Description = "Test Prescription",
                RxProductLookups = new List<RxProductLookup>() { seedVariety1RateLookup,
                                                                 seedVariety2RateLookup,
                                                                 fertilizerRateLookup,
                                                                 seedVariety1DepthLookup,
                                                                 seedVariety2DepthLookup,
                                                                 insecticideRateLookup },
                RxShapeLookups = new List<RxShapeLookup>() { zone1, zone2, zone3 },
                CropZoneId = cropZone.Id.ReferenceId,
                FieldId = field.Id.ReferenceId
            };

            (adm.Catalog.Prescriptions as List<Prescription>).Add(vectorPrescription);

            //--------------------------
            //Export data to file via the Plugin
            //--------------------------
            PrecisionPlanting.ADAPT._2020.Plugin plugin = new Plugin();
            plugin.Export(adm, path);
        }

        public static NumericRepresentation GetNumericRepresentation(string code)
        {
            RepresentationInstance.NumericRepresentation numericRepresentation = RepresentationInstance.RepresentationManager.Instance.Representations.Single(x => x.DomainId == code) as RepresentationInstance.NumericRepresentation;
            return numericRepresentation.ToModelRepresentation();
        }

        public static NumericRepresentationValue GetNumericRepresentationValue(double d, string uomCode, string numericRepresentationCode)
        {
            NumericRepresentationValue numericRepresentationValue = new NumericRepresentationValue();
            numericRepresentationValue.Representation = GetNumericRepresentation(numericRepresentationCode);
            
            UnitOfMeasure uom = UnitInstance.UnitSystemManager.GetUnitOfMeasure(uomCode);
            numericRepresentationValue.Value = new NumericValue(uom, d);

            return numericRepresentationValue;
        }
    }
}
