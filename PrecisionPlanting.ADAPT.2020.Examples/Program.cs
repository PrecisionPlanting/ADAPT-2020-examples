﻿using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.LoggedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PrecisionPlanting.ADAPT._2020.Examples
{
    class Program
    {
        //For a graphical user interface, clone https://github.com/ADAPT/ADAPT-Visualizer and follow the usage instructions in http://2020.ag/developers.html 
        //This example code provides examples specific to data populated into the ADAPT model by the 2020 Plugin.

        //Import 20|20 data arguments:
        //Arg 0 - "-import"
        //Arg 1 - path to data for import
        //Optional args
        //-5: 5hz data frequency
        //-1: 1hz data frequency (default)
        //-all: include all optional data (default is none)

        //Export 20|20 data arguments
        //Arg 0 - "-export"
        //Arg 1 - path to export sample prescriptions/setup data
        static void Main(string[] args)
        {
            Console.WriteLine("Precision Planting 20|20 ADAPT Plugin Example Code");
            if (!args.Any() || args.Count() < 2)
            {
                Console.WriteLine();
                Console.WriteLine("Import Usage: -import {path to 20|20 data} optional args");
                Console.WriteLine("Export Usage: -export {export path}");
                Console.WriteLine();
                Console.WriteLine("Please enter required arguments");
                args = Console.ReadLine().Split(' ');
            }


            //Instantiate the 2020 plugin
            PrecisionPlanting.ADAPT._2020.Plugin _2020Plugin = new Plugin();
            string dataPath = args[1];

            if (args.Contains("-import"))
            {
                //Load any properties to customize the data import
                Properties pluginProperties = GetPluginProperties(args);

                //Import data from the given path.
                IList<ApplicationDataModel> admObjects = _2020Plugin.Import(dataPath, pluginProperties);

                if (admObjects != null && admObjects.Any())
                {
                    //A 2020 Plugin import will always contain 1 ApplicationDataModel (ADM) object.    All data in the import path is included within this object.
                    //The ADAPT Framework Plugin Import method returns a list of ADM objects to support other industry data types (e.g., ISOXML) 
                    //where there is a concept of multiple wholly-contained datasets in a given file path.
                    ApplicationDataModel adm = admObjects.Single();

                    //The Catalog contains definition data for this import
                    Catalog catalog = adm.Catalog;

                    //The LoggedDataobject corresponds to a single 2020 file and defines one or more operations on a particular field.
                    int loggedDataCount = adm.Documents.LoggedData.Count();
                    if (loggedDataCount > 0)
                    {
                        LoggedData exampleLoggedData = adm.Documents.LoggedData.First();
                        if (loggedDataCount > 1)
                        {
                            Console.WriteLine("This dataset contains multiple 2020 files.   Illustrating data from the first file only.");
                            Console.WriteLine();
                        }

                        //Grower, Farm, Field, and Crop illustration. 
                        SetupData.DescribeLogisticsData(catalog, exampleLoggedData);

                        //Operation Types, Regions and Product illustration
                        OperationTypeData.DescribeTypeRegionProduct(catalog, exampleLoggedData);

                        //Task Summary illustration
                        SummaryData.DescribeSummaryData(adm, exampleLoggedData);

                        //Implement illustration
                        ImplementData.DescribeImplement(catalog, exampleLoggedData);

                        //Spatial Data illustration, including discussion of multivariety, ADAPT representations and Units of Measure
                        SpatialData.DescribeSpatialData(catalog, exampleLoggedData);
                    }

                    Console.WriteLine("Done");

                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("No data found");
                    Console.ReadLine();
                }
            }
            else if (args.Contains("-export"))
            {
                //Export the sample 2020 file
                ExportData.Export(dataPath);
                Directory.CreateDirectory(dataPath);
                Console.WriteLine($"File written to {dataPath}");
            }
        }

        private static Properties GetPluginProperties(string[] args)
        {
            //The plugin properties provide a means to limit the amount of data returned, improving performance by eliminated data or detail not required.
            //By default, the plugin downsamples raw 5Hz machine data to 1Hz.   If you wish to obtain the full 5Hz data, pass the DataFrequency parameter = 5
            //Similarly, you can opt to include optional certain sensors (e.g., Downforce = true) to increase the amount of data returned
            //See http://2020.ag/developers.html for a list of properties and values.
            Properties pluginProperties = null;
            if (args.Any() && args.Count() > 1)
            {
                pluginProperties = new Properties();
                if (args.Contains("-5"))
                {
                    pluginProperties.SetProperty("DataFrequency", "5");
                }
                else
                {
                    pluginProperties.SetProperty("DataFrequency", "1");
                }

                if (args.Contains("-all"))
                {
                    pluginProperties.SetProperty("Downforce", "true");
                    pluginProperties.SetProperty("SeedingQuality", "true");
                    pluginProperties.SetProperty("SoilSensing", "true");
                    pluginProperties.SetProperty("GranularApplication", "true");
                    pluginProperties.SetProperty("LiquidApplication", "true");
                    pluginProperties.SetProperty("RowUnitDepthControl", "true");
                    pluginProperties.SetProperty("RowUnitClosingSystem", "true");
                    pluginProperties.SetProperty("RowTotals", "true");
                    pluginProperties.SetProperty("RowCleanerSystem", "true");
                }
               
            }
            return pluginProperties;
        }
    }
}
