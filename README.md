# CreateBlobRecord
In this Solution we will create a blob record after upload file from Notes "Annotation" in Dynamics CRM, then we will remove the annotation from CRM and just display the URL of Documents after upload it in Azure Blob Storage
#How To Work:
The component to integrate with Azure Blob Storage (without Azure Blob Storage Dlls), but this Solution implemented on V 9.0 so you must work with this solution in Dynamics 365 V9.0
This solution Includes:
-	Blob Configuration: to declare the blob Key, container, and storage account.
-	Blob Storage: The Entity which create a records after upload it by using Annotations (Notes) .
-	Blob Storage Entity: to declare the Entities which Working with Blob Storage.
-	Plugin to create record from Annotation to Blob Storage
-	WF to delete the Annotation after created in Blob Storage.
Now, here are the steps:
-	First, Declare three records in Blob Configuration by these Keys: “storageKey”,” storageAccount”,and “containerName”,and get the values of these keys from Azure Blob storage.
-	In Blob Storage entity, put the logical name of any entity you want to work with Blob storage.
-	After that, you will work with Annotation as an uploader, so put a sub-grid in Entity you will declared in the previous step, Note that the “Blob Storage” is an Activity, so you will enable activity in the target entity to work with it.
