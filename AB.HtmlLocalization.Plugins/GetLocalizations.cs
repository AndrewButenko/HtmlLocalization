using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace AB.HtmlLocalization.Plugins
{
    public class GetLocalizations : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(null);

            var webResourcesString = (string)context.InputParameters["WebResources"];

            var webResources = (JsonConvert.DeserializeObject<List<string>>(webResourcesString)).ToArray<object>();

            var jsWebResourcesQuery = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "dependencyxml")
            };

            jsWebResourcesQuery.Criteria.AddCondition("name", ConditionOperator.In, webResources);
            jsWebResourcesQuery.Criteria.AddCondition("dependencyxml", ConditionOperator.NotNull);

            var jsWebResources = service.RetrieveMultiple(jsWebResourcesQuery).Entities.ToList();

            var dependentWebresources = new List<string>();

            jsWebResources.ForEach(t =>
            {
                var dependencyXml = t.GetAttributeValue<string>("dependencyxml");

                var xDocument = XDocument.Parse(dependencyXml);

                dependentWebresources.AddRange(xDocument.XPathSelectElements("Dependencies/Dependency/Library")
                    .Where(d => d.Attribute("name") != null).Select(d => d.Attribute("name").Value).Distinct());
            });

            if (dependentWebresources.Count == 0)
            {
                context.OutputParameters["Localizations"] = JsonConvert.SerializeObject(new Dictionary<string, string>());
                return;
            }

            var resxWebResourcesQuery = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("content", "name")
            };
            resxWebResourcesQuery.Criteria.AddCondition("name", ConditionOperator.In, dependentWebresources.ToList<object>().ToArray());
            //Webresource Type 12 - resx files - we need only those
            resxWebResourcesQuery.Criteria.AddCondition("webresourcetype", ConditionOperator.Equal, 12);

            var resxWebResources = service.RetrieveMultiple(resxWebResourcesQuery).Entities.ToList();

            var dependencyNameToGuidMap = new Dictionary<string, string>();

            var serializedLabels = new Dictionary<string, string>();

            resxWebResources.ForEach(t =>
            {
                dependencyNameToGuidMap.Add(t.GetAttributeValue<string>("name"), t.Id.ToString("N").ToUpper());

                var content = t.GetAttributeValue<string>("content");

                content = Encoding.Default.GetString(Convert.FromBase64String(content));

                var contentDocument = XDocument.Parse(content);

                var dataNodes = contentDocument.XPathSelectElements("root/data").Select(n => new { Name = n.Attribute("name").Value, Value = n.Element("value").Value }).ToDictionary(n => n.Name, n => n.Value);

                var labels = JsonConvert.SerializeObject(dataNodes);

                serializedLabels.Add($"LOCID_{t.Id:N}".ToUpper(), labels);
            });

            serializedLabels.Add("DependencyNameToGuidMap", JsonConvert.SerializeObject(dependencyNameToGuidMap));

            context.OutputParameters["Localizations"] = JsonConvert.SerializeObject(serializedLabels);
        }
    }

}