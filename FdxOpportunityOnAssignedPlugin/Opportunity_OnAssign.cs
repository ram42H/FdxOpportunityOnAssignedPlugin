using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace FdxOpportunityOnAssignedPlugin
{
    public class Opportunity_OnAssign : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            //Extract the tracing service for use in debugging sandboxed plug-ins....
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //Obtain execution contest from the service provider....
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            int step = 0;

            if (context.InputParameters.Contains("Target"))
            {
                step = 2;
                Entity opportunityPreImageEntity = ((context.PreEntityImages != null) && context.PreEntityImages.Contains("opppre")) ? context.PreEntityImages["opppre"] : null;

                Entity opportunityEntity = new Entity
                {
                    LogicalName = "opportunity",
                    Id = opportunityPreImageEntity.Id
                };

                step = 3;
                if (opportunityPreImageEntity.LogicalName != "opportunity")
                    return;

                try
                {
                    step = 5;
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    //Get current user information....
                    step = 6;
                    WhoAmIResponse response = (WhoAmIResponse)service.Execute(new WhoAmIRequest());

                    step = 7;
                    opportunityEntity["fdx_lastassignedowner"] = new EntityReference("systemuser", ((EntityReference)opportunityPreImageEntity.Attributes["ownerid"]).Id);

                    step = 8;
                    opportunityEntity["fdx_lastassigneddate"] = DateTime.UtcNow;

                    step = 9;
                    service.Update(opportunityEntity);

                    //Update last assign date on account if exist....
                    step = 10;
                    Entity opportunity = new Entity();
                    opportunity = service.Retrieve("opportunity", opportunityPreImageEntity.Id, new ColumnSet(true));

                    step = 11;
                    if (opportunity.Attributes.Contains("parentaccountid"))
                    {
                        step = 12;
                        Entity account = new Entity
                        {
                            Id = ((EntityReference)opportunity.Attributes["parentaccountid"]).Id,
                            LogicalName = "account"
                        };

                        step = 13;
                        account["fdx_lastassigneddate"] = DateTime.UtcNow;

                        step = 14;
                        service.Update(account);
                    }
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException(string.Format("An error occurred in the Opportunity_OnAssign plug-in at Step {0}.", step), ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Opportunity_OnAssign: step {0}, {1}", step, ex.ToString());
                    throw;
                }
            }
        }
    }
}
