using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;

namespace LinkDev.DeleteAnnotation
{
    public class DeleteAnnotation : CodeActivity
    {
        /// <summary>
        /// Custome Activity to Delete Annotation from Dynamics CRM after upload it to Azure Blob Storage
        /// </summary>
        /// <param name="executionContext"></param>
        protected override void Execute(CodeActivityContext executionContext)
        {
            try
            {
                // Get the context service.
                var context = executionContext.GetExtension<IWorkflowContext>();
                var serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
                // Use the context service to create an instance of IOrganizationService.
                IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.InitiatingUserId);
                if ((context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity))
                {
                    Entity noteEntity = (Entity)context.InputParameters["Target"];
                    EntityReference ObjectEntity = ((EntityReference)noteEntity.Attributes["objectid"]);
                    string EntityCollectionXML = "<fetch version='1.0' output-format='xml-platform' no-lock='true'  mapping='logical' distinct='true'>" +
                                   "  <entity name='ldv_blobstorageentity'>    " +
                                   "    <attribute name='ldv_name' />    " +
                                   "  </entity>" +
                                   "</fetch>";
                    EntityCollection Entitiesvalues = orgService.RetrieveMultiple(new FetchExpression(EntityCollectionXML));
                    if (Entitiesvalues.Entities != null && Entitiesvalues.Entities.Count > 0)
                    {
                        foreach (var item in Entitiesvalues.Entities)
                        {
                            if (item.Attributes["ldv_name"].ToString() == ObjectEntity.LogicalName)
                            {
                                orgService.Delete(noteEntity.LogicalName, noteEntity.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                throw new InvalidPluginExecutionException("Custom Step: " + exc.Message);
            }
        }


    }
}
