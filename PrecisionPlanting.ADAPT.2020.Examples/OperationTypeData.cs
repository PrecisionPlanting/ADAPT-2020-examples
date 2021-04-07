using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.LoggedData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.Products;

namespace PrecisionPlanting.ADAPT._2020.Examples
{
    public class OperationTypeData
    {
        public static void DescribeTypeRegionProduct(Catalog catalog, LoggedData loggedData)
        {
           Console.WriteLine();
           Console.WriteLine("-----------------");
           Console.WriteLine("Operation Type(s)");
           Console.WriteLine("-----------------");
           Console.WriteLine();

            //The loggedData may be of type SowingAndPlanting, Harvesting or Fertilizing
            //In the case of Planting, there is the possibility for a contemporaneous 
            //(liquid) Fertilizing and/or (insecticide) CropProtection operation,
            //in cases where the planter supports that configuration.

            //Each type of operation with be segemented into individual OperationData objects within the logged data.
            int operationTypeCount = 0;
            foreach (OperationTypeEnum operationType in loggedData.OperationData.Select(o => o.OperationType).Distinct())
            {
                string operationDescription = Enum.GetName(typeof(OperationTypeEnum), operationType);
               Console.WriteLine($"The field operation contains {operationDescription} data.");
                operationTypeCount++;
            }

           Console.WriteLine();
           Console.WriteLine("----------");
           Console.WriteLine("Region(s)");
           Console.WriteLine("----------");
           Console.WriteLine();

            //There may be more than one OperationData object of the same type in a LoggedData object
            //in this case, the field was broken into multiple regions.
            //A region is a part of a field operation that can be defined by consistent products and implement configurations.
            //In many cases, there was a pause in the field operation or some other trigger to spawn a new region,
            //but they are broken out separately as there is no guarantee that an implement will be configured the same
            //or that the same products will be applied in separate regions.

            int regionCount = loggedData.OperationData.Count() / operationTypeCount;
           Console.WriteLine($"The field operation contains {regionCount} region(s).");

            if (operationTypeCount > 1)
            {
                OperationData firstOperationData = loggedData.OperationData.First();
                List<int> firstRegionOperationIds = new List<int>() { firstOperationData.Id.ReferenceId };

                //The CoincicentOperationDataIds property contains an Id cross-reference,
                //defining other OperationData objects whose individual points are logged at the 
                //same place and time as a given OperationData.
                firstRegionOperationIds.AddRange(firstOperationData.CoincidentOperationDataIds);

               Console.WriteLine($"For example, the first region contains data from each of the {operationTypeCount} operation types,");
               Console.WriteLine($"and is reported in these OperationData Ids: {String.Join(',', firstRegionOperationIds)}.");
            }
            else
            {
               Console.WriteLine($"Each OperationData object includes all data for the region.");
            }

           Console.WriteLine();
           Console.WriteLine("----------");
           Console.WriteLine("Product(s)");
           Console.WriteLine("----------");
           Console.WriteLine();

            //Seeding
            //The loggedData may have one or multiple seed products, depending 
            //on whether the operator loaded different varieties into different parts of the planter
            //or whether the planter supports individual rows switching varieties dynamically.
            //Additionally, there may be a liquid fertilizer product and/or a dry insecticide product

            //Harvest
            //Where possible, loggedData object will report the seeding variety 

            //Side-dressing
            //Side-dress operations will contain a single liquid fertilizer product.

            IEnumerable<int> distinctProductIDs = loggedData.OperationData.SelectMany(o => o.ProductIds).Distinct();
            foreach (int productId in distinctProductIDs)
            {
                Product product = catalog.Products.SingleOrDefault(p => p.Id.ReferenceId == productId);
                string typeDescription = Enum.GetName(typeof(ProductTypeEnum), product.ProductType);
               Console.WriteLine($"The field operation includes \"{product.Description}\", a {typeDescription} product.");

                //Product Mixes
                if (product.ProductType == ProductTypeEnum.Mix)
                {
                   Console.WriteLine($"The mix contains the following components:");
                    foreach (ProductComponent component in product.ProductComponents)
                    {
                        //Product components are listed in the catalog as ingredients
                        int ingredientId = component.IngredientId;
                        if (component.IsProduct) //Product component.  I.e., something that is itself a product that may be applied on its own
                        {
                            Product componentProduct = catalog.Products.SingleOrDefault(i => i.Id.ReferenceId == ingredientId);

                            //Component Quantities are reported as RepresentationValues
                            //A Representation Value is a complex type that allows the value to 
                            //be augmented with unit of measure and other data.
                            double componentQuantity = component.Quantity?.Value?.Value ?? 0d;
                            string quantityUOMCode = component.Quantity?.Value?.UnitOfMeasure?.Code ?? "count";

                            Console.WriteLine($"{componentProduct.Description}, at a rate of {componentQuantity} {quantityUOMCode}");
                        }
                        else //Ingredient, in our case this is always a fertilizer nutrient
                        {
                            Ingredient ingredient = catalog.Ingredients.SingleOrDefault(i => i.Id.ReferenceId == ingredientId);
                            double componentQuantity = component.Quantity?.Value?.Value ?? 0d;
                            string quantityUOMCode = component.Quantity?.Value?.UnitOfMeasure?.Code ?? "count";

                            Console.WriteLine($"{ingredient.Description}, at a rate of {componentQuantity} {quantityUOMCode}");
                        }
                    }
                   Console.WriteLine();
                }
            }
        }
    }
}
