from promptflow import tool
from azure.storage.blob import BlobServiceClient
from promptflow.connections import CustomConnection

@tool
def get_blob_content(blob_names : list, conn: CustomConnection):
    concatenated_content = ""
    blob_service_client = BlobServiceClient.from_connection_string(conn.connstring)
    for blob_name in blob_names:
        blob_client = blob_service_client.get_blob_client(container=conn.container, blob=blob_name)
        blob_content = blob_client.download_blob()
        content = blob_content.readall().decode("utf-8")
        concatenated_content += content
    
    return concatenated_content