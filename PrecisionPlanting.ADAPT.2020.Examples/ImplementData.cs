using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.LoggedData;
using AgGateway.ADAPT.ApplicationDataModel.Equipment;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AgGateway.ADAPT.ApplicationDataModel.Representations;

namespace PrecisionPlanting.ADAPT._2020.Examples
{
    public class ImplementData
    {
        public static void DescribeImplement(Catalog catalog, LoggedData loggedData)
        {
           Console.WriteLine();
           Console.WriteLine("-----------------------");
           Console.WriteLine("Equipment Configuration");
           Console.WriteLine("-----------------------");
           Console.WriteLine();


            //A LoggedData will have a single EquipmentConfigurationGroup that contains any EquipmentConfigurations in the file
            EquipmentConfigurationGroup equipConfigGroup = loggedData.EquipmentConfigurationGroup;

            //The configuration of the equipment can vary by each region, although it is likely that the equipment configuration remains consistent across many regions
            //Any distinct configurations represented in the field operation will be EquipmentConfigurations within this group
            List<EquipmentConfiguration> distinctConfigurations = equipConfigGroup.EquipmentConfigurations;
           Console.WriteLine($"Field operation has {distinctConfigurations.Count} distinct equipment configuration(s).");
           Console.WriteLine();

            //While a single OperationData object supports a list of EquipmentConfigurations, the 2020 plugin will always reference a single EquipmentConfiguration on any one OperationData.
            //This allows the consumer to predictively map data based a known equipment definition for that data.

            //Going deeper on the first OperationData
           Console.WriteLine("The first region and OperationData within the field operation has this configuration:");
            EquipmentConfiguration equipConfig = distinctConfigurations.SingleOrDefault(c => c.Id.ReferenceId == loggedData.OperationData.First().EquipmentConfigurationIds.Single());

            //The equipment configuration maps to 2 connectors, explaining what machinery was hitched together
            Connector connector1 = catalog.Connectors.SingleOrDefault(c => c.Id.ReferenceId == equipConfig.Connector1Id);
            Connector connector2 = catalog.Connectors.SingleOrDefault(c => c.Id.ReferenceId == equipConfig.Connector2Id);

            //Each connector contains two pieces of information, the DeviceElementConfiguration that connector/hitch is a part of, and metadata on a specific hitch point.
            DeviceElementConfiguration deviceElementConfiguration1 = catalog.DeviceElementConfigurations.SingleOrDefault(c => c.Id.ReferenceId == connector1.DeviceElementConfigurationId);
            HitchPoint hitchPoint1 = catalog.HitchPoints.SingleOrDefault(h => h.Id.ReferenceId == connector1.HitchPointId);

            DeviceElementConfiguration deviceElementConfiguration2 = catalog.DeviceElementConfigurations.SingleOrDefault(c => c.Id.ReferenceId == connector2.DeviceElementConfigurationId);
            HitchPoint hitchPoint2 = catalog.HitchPoints.SingleOrDefault(h => h.Id.ReferenceId == connector2.HitchPointId);

            //DeviceElementConfigurations are a polymorphic object within ADAPT.
            //A DeviceElementConfiguration may be of type
            //  MachineConfiguration (describing a tractor/vehicle)
            //  ImplementConfiguration (describing an entire implement)
            //  SectionConfiguration (describing a subsection or individual row of an implement)

            //DeviceElementConfigurations are part of a 3-object hierarchy that describes a piece of equipment
            //1. DeviceModel - A high-level description of the equipment: brand, manufacturer, description.   Any single piece of equipment has only 1 device model.
            //2. DeviceElement -A hierarchical descripion part of the equipment: brand, manufacturer, description, and type of element (section, machine, implement, etc.).
            //                  A DeviceElement maps to a single DeviceModel, and may be a parent and/or child of other DeviceElements.  
            //                  E.g., each section is a child of the root implement DeviceElement
            //3. DeviceElementConfigurations - The DeviceElementConfiguration is an extension of the DeviceElement, each mapping to a single DeviceElement, 
            //                                  but having specific phyproperties such as width and offsets.

            //The 2020 equipment configuration will always have a Machine/Vehicle as the Connector1 and an Implement as the Connector2.
            MachineConfiguration vehicleConfiguration = deviceElementConfiguration1 as MachineConfiguration;
            ImplementConfiguration implementConfiguration = deviceElementConfiguration2 as ImplementConfiguration;

            HitchPoint vehicleHitch = hitchPoint1;
            HitchPoint implementHitch = hitchPoint2;

            //The DeviceElements expose the hierarchy between parts of the equipment
           Console.WriteLine();
           Console.WriteLine("Vehicle DeviceElement Hierarchy:");
            DeviceElement vehicleDeviceElement = catalog.DeviceElements.SingleOrDefault(d => d.Id.ReferenceId == vehicleConfiguration.DeviceElementId);
            DescribeDeviceHierarchy(catalog, vehicleDeviceElement, 0, new List<DeviceElement>());

           Console.WriteLine();
           Console.WriteLine("Implement DeviceElement Hierarchy:");
            List<DeviceElement> implementChildElements = new List<DeviceElement>();
            DeviceElement implementDeviceElement = catalog.DeviceElements.SingleOrDefault(d => d.Id.ReferenceId == implementConfiguration.DeviceElementId);
            DescribeDeviceHierarchy(catalog, implementDeviceElement, 0, implementChildElements);

           Console.WriteLine();


           Console.WriteLine();
           Console.WriteLine("-----------------------");
           Console.WriteLine("Implement Width Values");
           Console.WriteLine("-----------------------");
           Console.WriteLine();

            //The Implement and Section DeviceElementConfigurations carry width information.
           Console.WriteLine($"The {implementConfiguration.Description} is {implementConfiguration.PhysicalWidth.Value.Value} {implementConfiguration.PhysicalWidth.Value.UnitOfMeasure.Code} wide.");
            foreach (DeviceElement childElement in implementChildElements)
            {
                DeviceElementConfiguration deviceElementConfiguration = catalog.DeviceElementConfigurations.SingleOrDefault(c => c.DeviceElementId == childElement.Id.ReferenceId);
                if (deviceElementConfiguration != null)
                {
                    SectionConfiguration sectionConfiguration = deviceElementConfiguration as SectionConfiguration;
                    if (sectionConfiguration != null)
                    {
                       Console.WriteLine($"{sectionConfiguration.Description} is {sectionConfiguration.SectionWidth.Value.Value} {sectionConfiguration.SectionWidth.Value.UnitOfMeasure.Code} wide.");
                    }
                }
            }


           Console.WriteLine();
           Console.WriteLine("-----------------------");
           Console.WriteLine("Equipment Offset Values");
           Console.WriteLine("-----------------------");
           Console.WriteLine();

            //Various offset values describe where each device element is located vs. other elements via data on the device element configuration

            //Vehicle GPS Receiver
            DescribeOffset("GPS Receiver", vehicleConfiguration.GpsReceiverXOffset, vehicleConfiguration.GpsReceiverYOffset, "tractor reference point (center of rear axle)");

            //Tractor hitch offset
            DescribeOffset("vehicle hitch point", vehicleHitch.ReferencePoint.XOffset, vehicleHitch.ReferencePoint.YOffset, "tractor reference point (center of rear axle)");

            //Implement hitch offset
            DescribeOffset("implement hitch point", implementHitch.ReferencePoint.XOffset, implementHitch.ReferencePoint.YOffset, "implement reference point (center of implement)");

            //Implmement control point offset (inverse of the prior)
            DescribeOffset("implement control point offset", implementConfiguration.ControlPoint.XOffset, implementConfiguration.ControlPoint.YOffset, "tractor hitch point");

            //Section offsets (measured to center of each section)
            foreach (DeviceElement childElement in implementChildElements)
            {
                DeviceElementConfiguration deviceElementConfiguration = catalog.DeviceElementConfigurations.SingleOrDefault(c => c.DeviceElementId == childElement.Id.ReferenceId);
                if (deviceElementConfiguration != null)
                {
                    SectionConfiguration sectionConfiguration = deviceElementConfiguration as SectionConfiguration;
                    if (sectionConfiguration != null)
                    {
                        DescribeOffset($"{childElement.Description} offset", sectionConfiguration.InlineOffset, sectionConfiguration.LateralOffset, "tractor hitch point");
                    }
                }
            }
        }

        private static void DescribeDeviceHierarchy(Catalog catalog, DeviceElement element, int depth, List<DeviceElement> allChildElements)
        {
            string indentText = "--";
            string indent = string.Empty;
            for (int i = 0; i < depth; i++)
            {
                indent = string.Concat(indent, indentText);
            }

           Console.WriteLine($"{indent}{element.Description}");
            foreach (DeviceElement child in catalog.DeviceElements.Where(d => d.ParentDeviceId == element.Id.ReferenceId))
            {
                allChildElements.Add(child);  //Fill in this list for use further down in our code
                DescribeDeviceHierarchy(catalog, child, depth + 1, allChildElements);
            }
        }

        private static void DescribeOffset(string offsetDescription, NumericRepresentationValue xOffset, NumericRepresentationValue yOffset, string referencePoint)
        {
            double inlineOffset = xOffset.Value.Value;
            string inlineOffsetUnit = xOffset.Value.UnitOfMeasure.Code;
            string inlineOffsetDirection = inlineOffset >= 0d ? "front" : "back";

            double lateralOffset = yOffset.Value.Value;
            string lateralOffsetUnit = yOffset.Value.UnitOfMeasure.Code;
            string lateralOffsetDirection = lateralOffset >= 0d ? "right" : "left";

           Console.WriteLine($"The {offsetDescription} is located {Math.Abs(inlineOffset)} {inlineOffsetUnit} in {inlineOffsetDirection} and {Math.Abs(lateralOffset)} {lateralOffsetUnit} to {lateralOffsetDirection} of the {referencePoint}.");
        }
    }
}
