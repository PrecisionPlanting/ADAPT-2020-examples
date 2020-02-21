using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Equipment;
using AgGateway.ADAPT.ApplicationDataModel.LoggedData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AgGateway.ADAPT.ApplicationDataModel.Representations;
using AgGateway.ADAPT.ApplicationDataModel.Shapes;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.Products;

namespace PrecisionPlanting.ADAPT._2020.Examples
{
    public class SpatialData
    {
        public static void DescribeSpatialData(Catalog catalog, LoggedData loggedData)
        {
            //Coincident Operations
            DescribeCoincidentOperations(loggedData);

            Console.WriteLine();
            Console.WriteLine("-----------------------");
            Console.WriteLine("Spatial Data");
            Console.WriteLine("-----------------------");
            Console.WriteLine();

            foreach (OperationData operationData in loggedData.OperationData)
            {
                //1.  Create some collections for tracking high-level/header information on the implement devices and sensors.
                List<WorkingData> operationDataWorkingDatas = new List<WorkingData>();
                Dictionary<int, int> useToDeviceConfigurationMapping = new Dictionary<int, int>();

                //ADAPT models spatial data in multiple depths according to a virtual hierarchy describing the data as it relates to the physical layout of the implement.
                //In the 2020 implementation, 
                //  depth 0 refers to a single device representing the entire width of the implement and any sensors reporting data across the implement.
                //  depth 1 generally refers to row-by-row data, where multiple DeviceElements representing individual rows contain one or more sensors
                //  depth 2 is present on planting data where there are multiple varieties configured in specific parts of the implement.  In that case
                //depth 1 contains a description of the varieties
                //depth 2 contains the row-level data
                for (int depth = 0; depth <= operationData.MaxDepth; depth++)  //MaxDepth defines the maximum depth of data on an OperationData
                {
                    //A DeviceElementUse is an instance of a DeviceElement/DeviceElementConfiguration within a specific OperationData.
                    //It contains the collection of all data elements (WorkingData objects) reported on that DeviceElement during the Operation.
                    IEnumerable<DeviceElementUse> deviceElementUses = operationData.GetDeviceElementUses(depth);
                    foreach (DeviceElementUse deviceElementUse in deviceElementUses)
                    {
                        //Track the DeviceConfiguration that this DeviceElementUse relates to for reconciling data values to implement offsets for precise location
                        useToDeviceConfigurationMapping.Add(deviceElementUse.Id.ReferenceId, deviceElementUse.DeviceConfigurationId);

                        //A WorkingData is essentially a Sensor.   It is the definition of some object that will report data per spatial point.
                        //List all such sensors on this DeviceElementUse within this Operation
                        IEnumerable<WorkingData> workingDatas = deviceElementUse.GetWorkingDatas();

                        //Track these in the comprehensive list
                        operationDataWorkingDatas.AddRange(workingDatas);
                    }
                }

                //2. Illustrate any multivariety data present here.
                //If an OperationData from the 2020 plugin contains a maxdepth of 2, then level 1 describes parts of the planter with specific varieties
                if (operationData.OperationType == OperationTypeEnum.SowingAndPlanting &&
                    operationData.MaxDepth == 2)
                {
                    //------------------
                    //Split Planter data
                    //-------------------
                    Console.WriteLine($"OperationData {operationData.Id.ReferenceId} is planter data containing multiple varieties assigned to specific rows:");
                    IEnumerable<DeviceElementUse> levelOneDeviceElementUses = operationData.GetDeviceElementUses(1);
                    foreach (DeviceElementUse use in levelOneDeviceElementUses)
                    {
                        //Retrieve the DeviceElementConfiguration object that matches the DeviceElementUse on this specific operation
                        DeviceElementConfiguration deviceElementConfig = catalog.DeviceElementConfigurations.First(d => d.Id.ReferenceId == use.DeviceConfigurationId);

                        //We've named the Level 1 device elements with the varieties in this situation
                        Console.WriteLine(deviceElementConfig.Description);

                        //All rows planting that variety will be children of this device element, 
                        //and the level 1 DeviceElementUse will have a WorkingData called "vrProductIndex" that will map to the variety
                    }
                    Console.WriteLine();
                }
                else if (operationData.OperationType == OperationTypeEnum.SowingAndPlanting && 
                         operationData.MaxDepth == 1 && 
                         operationDataWorkingDatas.Select(w => w.Representation.Code).Any(c => c == "vrProductIndex"))
                {
                    //------------------------------------------------------
                    //vSetSelect & mSet data (variable multi-hybrid planting)
                    //------------------------------------------------------
                    Console.WriteLine($"OperationData {operationData.Id.ReferenceId} is planter data containing multiple varieties dynamically assigned to each row.");

                    //Make a dictionary of product names
                    Dictionary<int, string> productNames = new Dictionary<int, string>();
                    foreach (int productID in operationData.ProductIds)
                    {
                        List<DeviceElementUse> productDeviceElementUses = new List<DeviceElementUse>();
                        Product product = catalog.Products.First(p => p.Id.ReferenceId == productID);
                        productNames.Add(productID, product.Description);
                    }

                    Console.WriteLine($"The following varieties are planted at various points in the field: {string.Join(", ", productNames.Values)}");

                    SpatialRecord firstPoint = operationData.GetSpatialRecords().First();
                    Console.WriteLine("For example, on the first point...");

                    //Examine the content of each DeviceElementUse at the row level with a product index working data
                    foreach (DeviceElementUse deviceElementUse in operationData.GetDeviceElementUses(1)) //1 is the row level where OperationData.MaxDepth == 1.
                    {
                        foreach (WorkingData productIndexWorkingData in deviceElementUse.GetWorkingDatas().Where(w => w.Representation.Code == "vrProductIndex"))
                        {
                            NumericRepresentationValue productValue = firstPoint.GetMeterValue(productIndexWorkingData) as NumericRepresentationValue;
                            int productIndex = (int)productValue.Value.Value;
                            DeviceElementConfiguration deviceElementConfiguration = catalog.DeviceElementConfigurations.First(d => d.Id.ReferenceId == deviceElementUse.DeviceConfigurationId);
                            Console.WriteLine($"{deviceElementConfiguration.Description} planted {productNames[productIndex]}.");
                        }
                    }

                    Console.WriteLine();
                }


                //3. Read the point-by-point data
                //With the data definition of the OperationData now in-hand, we can iterate the collection of physical points on the field to read the data.
                //Rather than writing out each data value to the screen, we will assemble them into collections to summarize after iterating all data
                Dictionary <WorkingData, List<object>> operationSpatialDataValues = new Dictionary<WorkingData, List<object>>();
                Dictionary<WorkingData, string> numericDataUnitsOfMeasure = new Dictionary<WorkingData, string>();

                //Similarly, we will track the geospatial envelope of the data to illustrate lat/lon data present
                double maxLat = Double.MinValue;
                double minLat = Double.MaxValue;
                double maxLon = Double.MinValue;
                double minLon = Double.MaxValue;

                Console.WriteLine("Reading point-by-point data...");
                Console.WriteLine();
                
                //IMPORTANT            
                //To effectively manage memory usage, avoid invoking the iterator multiple times or iterate the entire list in a Linq expression.
                //The linq expressions below do not necessarily take this advice as the focus here is illustrative.
                foreach (SpatialRecord spatialRecord in operationData.GetSpatialRecords())
                {
                    //2020 data will always be in point form
                    Point point = spatialRecord.Geometry as Point;

                    //Track the lat/lon to illustrate the envelope of the dataset
                    double latitude = point.Y;
                    double longitude = point.X;
                    if (latitude < minLat) minLat = latitude;
                    if (latitude > maxLat) maxLat = latitude;
                    if (longitude < minLon) minLon = longitude;
                    if (longitude > maxLon) maxLon = longitude;

                    //Examine the actual data on the points
                    foreach (WorkingData operationWorkingData in operationDataWorkingDatas)
                    {
                        //Create a List for data values on the first encounter with this WorkingData
                        if (!operationSpatialDataValues.ContainsKey(operationWorkingData))
                        {
                            operationSpatialDataValues.Add(operationWorkingData, new List<object>());
                        }

                        //---------------
                        //Representations
                        //---------------
                        //ADAPT publishes standard representations that often equate to ISO11783-11 Data Dictionary Identifiers (DDIs) 
                        //These representations define a common type of agricultural measurement.
                        //Where Precision Planting has implemented representations that are not published with ADAPT, they are marked as UserDefined
                        //and the Name describes what each is.

                        //--------------------
                        //RepresentationValues
                        //--------------------
                        //A Representation Value is a complex type that allows the value to 
                        //be augmented the full representation, the unit of measure and other data.

                        RepresentationValue representationValue = spatialRecord.GetMeterValue(operationWorkingData);

                        //Values reported may be of type Numeric or Enumerated
                        if (representationValue is NumericRepresentationValue)
                        {
                            NumericRepresentationValue numericRepresentationValue = representationValue as NumericRepresentationValue;
                            operationSpatialDataValues[operationWorkingData].Add(numericRepresentationValue.Value.Value); //Value is a double

                            //--------------------
                            //Units of Measure
                            //--------------------
                            //Store the UOM on the first encounter
                            if (!numericDataUnitsOfMeasure.ContainsKey(operationWorkingData))
                            {
                                numericDataUnitsOfMeasure.Add(operationWorkingData, numericRepresentationValue.Value.UnitOfMeasure.Code);

                                //ADAPT units of measure are documented in the Resources/UnitSystem.xml that is installed in any ADAPT project
                                //They take the form of unitCode[postive exponent]unitcode[negative exponent]
                                //Where the exponents allow for complex units.   E.g.s,
                                //lb = pounds
                                //ac = acres
                                //lb1ac-1 = pounds per acre
                                //mm3m-2 = cubic millimeters per square meter
                            }
                        }
                        else if (representationValue is EnumeratedValue)
                        {
                            EnumeratedValue enumeratedValue = representationValue as EnumeratedValue;
                            operationSpatialDataValues[operationWorkingData].Add(enumeratedValue.Value.Value); //Value is a string
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine("-----------------------");
                Console.WriteLine($"{Enum.GetName(typeof(OperationTypeEnum), operationData.OperationType)} data");
                Console.WriteLine("-----------------------");
                Console.WriteLine();

                Console.WriteLine($"Data logged within envelope bounded by {minLat},{minLon} and {maxLat},{maxLon}.");
                Console.WriteLine();

                foreach (WorkingData workingData in operationSpatialDataValues.Keys)
                {
                    //We can obtain a reference to the part of the machine that logged the data via the DeviceConfigurationId property.
                    DeviceElementConfiguration deviceElementConfig = catalog.DeviceElementConfigurations.FirstOrDefault(d => d.Id.ReferenceId == useToDeviceConfigurationMapping[workingData.DeviceElementUseId]);
                    string deviceElementName = deviceElementConfig.Description;

                    if (operationSpatialDataValues[workingData].Any())
                    {
                        if (workingData is NumericWorkingData)
                        {
                            double max = operationSpatialDataValues[workingData].Cast<double>().Max();
                            double average = operationSpatialDataValues[workingData].Cast<double>().Average();
                            double min = operationSpatialDataValues[workingData].Cast<double>().Min();
                            string uom = numericDataUnitsOfMeasure[workingData];
                            Console.WriteLine($"Numeric Working Data {deviceElementConfig.Description}-{workingData.Representation.Description} had a minimum value of {min}, and average of {average} and a maximum value of {max} {uom}.");
                            Console.WriteLine();
                        }
                        else if (workingData is EnumeratedWorkingData)
                        {
                            EnumeratedWorkingData enumeratedWorkingData = workingData as EnumeratedWorkingData;
                            EnumeratedRepresentation enumeratedRepresentation = enumeratedWorkingData.Representation as EnumeratedRepresentation;
                            IEnumerable<string> enumerationValues = enumeratedRepresentation.EnumeratedMembers.Select(e => e.Value);

                            foreach (string enumerationValue in enumerationValues)
                            {
                                int count = operationSpatialDataValues[workingData].Cast<string>().Count(v => v == enumerationValue);
                                Console.WriteLine($"Enumerated Working Data {deviceElementConfig.Description}-{workingData.Representation.Description} had {count} values of {enumerationValue}.");
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
        }

        public static void DescribeCoincidentOperations(LoggedData loggedData)
        {
            Console.WriteLine();
            Console.WriteLine("-----------------------");
            Console.WriteLine("Coincident Operations");
            Console.WriteLine("-----------------------");
            Console.WriteLine();

            //The OperationData.CoincidentOperationDataIDs property contains a reference to all related OperationData objects that represent
            //parallel operations, logged at the same place and time.
            //E.g., a planter may have the capability to plant seed, apply a granular insecticide and a liquid fertilizer all in one pass.
            //This example would be represented as 3 OperationDatas coincident to one another.
            //In such case, an implementer may wish to iterate the collections in parallel or otherwise join the data.

            List<List<int>> coincidentGroupings = new List<List<int>>();
            foreach (OperationData operationData in loggedData.OperationData)
            {
                if (operationData.CoincidentOperationDataIds.Any())
                {
                    List<int> coincidentList = new List<int>() { operationData.Id.ReferenceId }; //Add the id for the given item
                    operationData.CoincidentOperationDataIds.ForEach(i => coincidentList.Add(i));
                    if (!coincidentGroupings.Any(g => g.Contains(operationData.Id.ReferenceId)))
                    {
                        coincidentGroupings.Add(coincidentList);
                    }
                }
            }

            if (coincidentGroupings.Any())
            {
                Console.WriteLine($"The following operations are coincident to one another.");
                foreach (List<int> grouping in coincidentGroupings)
                {
                    Console.WriteLine(string.Join(",", grouping.ToArray()));
                }
            }
            else
            {
                Console.WriteLine($"There were no coincident operations.");
            }
        }
    }
}
