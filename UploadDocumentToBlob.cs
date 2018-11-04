using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

using System.Text;
namespace LinkDev.CreateBlob
{

    public class UploadDocumentToBlob : IPlugin
    {
        IOrganizationService organizationService = null;
        /// <summary>
        /// Dynamics CRM Plugin to create a record of Documents after upload it into Azure Blob Storage, rather than attach Notes in Dynamics Space
        /// </summary>
        //<Author>Ossama Abdallah</Author>
        /// <param name="serviceProvider"></param>
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                organizationService = serviceFactory.CreateOrganizationService(context.UserId);
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    
                    // Obtain the target entity from the input parameters.
                    Entity noteEntity = (Entity)context.InputParameters["Target"];
                    string title ="";
                    EntityReference ObjectEntity = ((EntityReference)noteEntity.Attributes["objectid"]);
                    byte[] doumentBody = Convert.FromBase64String(noteEntity.Attributes["documentbody"].ToString());
                    string content = Encoding.UTF8.GetString(doumentBody);
                    string fileName = noteEntity.Attributes["filename"].ToString().Replace(' ', '_');
                    if (noteEntity.Attributes.Contains("subject"))
                    {
                        string Subject = null;
                        if (noteEntity.Attributes["subject"] != null)
                       Subject = noteEntity.Attributes.Contains("subject") ? noteEntity.Attributes["subject"].ToString() : "";
                        if (Subject != null)
                        {
                            title = Subject.ToString();
                        }
                    }      
                    // Upload the attached text file to Azure Blog Container

                   UploadFileToAzureBlobStorage(doumentBody, fileName, title, noteEntity, organizationService, ObjectEntity, tracingService); 
                 

                }
            }
            catch (Exception ex)
            {
                  throw new InvalidPluginExecutionException("Upload Plugin: " + ex.Message);
            }
        }
        /// <summary>
        /// Upload the Document file after put it in annotation uploader on Dynamics CRM
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        /// <param name="Note"></param>
        /// <param name="orgserv"></param>
        /// <param name="ObjectEntity"></param>
        /// <param name="tracing"></param>
        void UploadFileToAzureBlobStorage(byte[] content, string fileName,string title,Entity Note, IOrganizationService orgserv, EntityReference ObjectEntity, ITracingService tracing)
        {
            try
            {
            //Get the Keys from Blob Configuration Entity
                string storageKey = GetConfiguration("storageKey");
                string storageAccount = GetConfiguration("storageAccount");
                string containerName = GetConfiguration("containerName");
                                                                         
                string blobName = fileName;

                string method = "PUT";
                int contentLength = content.Length;
                string requestUri = $"https://{storageAccount}.blob.core.windows.net/{containerName}/{blobName}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);

                string now = DateTime.UtcNow.ToString("R");

                request.Method = method;
                if (!blobName.Contains(".pdf"))
                {
                    request.ContentType = "text/plain; charset=UTF-8";
                }
                else
                {
                    request.ContentType = "application/pdf";
                }
                request.ContentLength = contentLength;

                request.Headers.Add("x-ms-version", "2015-12-11");
                request.Headers.Add("x-ms-date", now);
                request.Headers.Add("x-ms-blob-type", "BlockBlob");
                request.Headers.Add("Authorization", AuthorizationHeader(method, now, request, storageAccount, storageKey, containerName, blobName));

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(content, 0, contentLength);
                }
                string EntityCollectionXML = "<fetch version='1.0' output-format='xml-platform' no-lock='true'  mapping='logical' distinct='true'>" +
                                   "  <entity name='ldv_blobstorageentity'>    " +
                                   "    <attribute name='ldv_name' />    " +
                                   "  </entity>" +
                                   "</fetch>";
                EntityCollection Entitiesvalues = organizationService.RetrieveMultiple(new FetchExpression(EntityCollectionXML));
                if (Entitiesvalues.Entities != null && Entitiesvalues.Entities.Count > 0)
                {
                  
                    foreach (var item in Entitiesvalues.Entities)
                    {
                        if (item.Attributes["ldv_name"].ToString() == ObjectEntity.LogicalName)
                        {
                            Entity BlobEntity = new Entity("ldv_blobstorage")
                            {
                                ["ldv_uri"] = requestUri,
                                ["subject"] = title,
                                ["regardingobjectid"] = new EntityReference(ObjectEntity.LogicalName, ((EntityReference)Note.Attributes["objectid"]).Id),
                                ["new_attachmentname"] = fileName
                            };
                            orgserv.Create(BlobEntity);
                        }
                    }
                }
                using (HttpWebResponse resp = (HttpWebResponse)request.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        

                    }
                }
            }
            catch (Exception exc)
            {
                throw new InvalidPluginExecutionException("Upload to Blop: " + exc.Message);
            }

        }


        public string AuthorizationHeader(string method, string now, HttpWebRequest request, string storageAccount, string storageKey, string containerName, string blobName)
        {
            string headerResource = $"x-ms-blob-type:BlockBlob\nx-ms-date:{now}\nx-ms-version:2015-12-11";
            string urlResource = $"/{storageAccount}/{containerName}/{blobName}";
            string stringToSign = $"{method}\n\n\n{request.ContentLength}\n\n{request.ContentType}\n\n\n\n\n\n\n{headerResource}\n{urlResource}";

            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(storageKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            String AuthorizationHeader = String.Format("{0} {1}:{2}", "SharedKey", storageAccount, signature);
            return AuthorizationHeader;
        }
        
        /// <summary>
        /// Get the General Configuration from the Blob Configuration Entity in Dynamics CRM
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private string GetConfiguration(string Key)
        {
            String ReturnedValue = "";
            string fetchXML = "<fetch version='1.0' output-format='xml-platform' no-lock='true'  mapping='logical' distinct='true'>" +
                                    "  <entity name='ldv_blobconfiguration'>    " +
                                    "    <attribute name='ldv_key' />" +
                                    "    <attribute name='ldv_name' />    " +
                                    "    <filter type='and'>      " +
                                    "      <condition attribute='ldv_name' operator='eq' value='" + Key + "'>" +
                                    "      </condition>" +
                                    "    </filter>" +
                                    "  </entity>" +
                                    "</fetch>";

            EntityCollection values = organizationService.RetrieveMultiple(new FetchExpression(fetchXML));
            if (values.Entities != null && values.Entities.Count > 0)
            {
                if (values.Entities[0].Contains("ldv_name"))
                {
                    ReturnedValue = values.Entities[0].Attributes["ldv_key"].ToString();
                }
            }

            return ReturnedValue;
        }
    }

}
