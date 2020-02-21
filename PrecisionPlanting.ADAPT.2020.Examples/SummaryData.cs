using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Documents;
using AgGateway.ADAPT.ApplicationDataModel.LoggedData;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgGateway.ADAPT.ApplicationDataModel.Representations;
using AgGateway.ADAPT.ApplicationDataModel.Products;

namespace PrecisionPlanting.ADAPT._2020.Examples
{
    public class SummaryData
    {
        public static void DescribeSummaryData(ApplicationDataModel adm, LoggedData loggedData)
        {
           Console.WriteLine();
           Console.WriteLine("-----------------------");
           Console.WriteLine("Task Summary Information");
           Console.WriteLine("-----------------------");
           Console.WriteLine();

            //Each LoggedData object will map to a Summary object via the SummaryId property.
            Summary summary = adm.Documents.Summaries.SingleOrDefault(s => s.Id.ReferenceId == loggedData.SummaryId);
            if (summary != null)
            {
                //Each summary will have a start date timescope
                TimeScope timeScope = summary.TimeScopes.SingleOrDefault(t => t.DateContext == DateContextEnum.ActualStart);
                DateTime startTime = timeScope.TimeStamp1.Value;

                //In the 2020 Plugin, we include the end date as TimeStamp2.
                DateTime endTime = timeScope.TimeStamp2.Value;

               Console.WriteLine($"The field operation started at {startTime.ToString()} and ended at {endTime.ToString()}");
               Console.WriteLine();
               Console.WriteLine($"The following are totals/averages for the field operation:");


                //The 2020 plugin will summarize all field Operation data into a single StampedMetersValues collection.
                //The ADAPT framework allows for multiple StampedMetersValues in cases where multiple timestamps govern multiple data values
                foreach (MeteredValue meteredValue in summary.SummaryData.Single().Values)
                {
                    NumericRepresentationValue representationValue = meteredValue.Value as NumericRepresentationValue;
                    string summaryValueName = representationValue.Representation.Description;
                    string uomCode = representationValue.Value.UnitOfMeasure.Code;
                    double value = representationValue.Value.Value;

                   Console.WriteLine($"{summaryValueName}: {value} {uomCode}");
                }

               Console.WriteLine();

                //For seed and liquid fertilizer products, the Plugin will summarize data by product

               Console.WriteLine($"The following are totals/averages by product:");
                foreach (OperationSummary operationSummary in summary.OperationSummaries)
                {
                    Product product = adm.Catalog.Products.FirstOrDefault(p => p.Id.ReferenceId == operationSummary.ProductId);
                   Console.WriteLine($"{product.Description}:");
                    foreach (MeteredValue meteredValue in operationSummary.Data.Single().Values)
                    {
                        NumericRepresentationValue representationValue = meteredValue.Value as NumericRepresentationValue;
                        string summaryValueName = representationValue.Representation.Description;
                        string uomCode = representationValue.Value.UnitOfMeasure.Code;
                        double value = representationValue.Value.Value;

                       Console.WriteLine($"{summaryValueName}: {value} {uomCode}");
                    }
                   Console.WriteLine();
                } 
            }
        }
    }
}
