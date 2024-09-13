using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace CandidApply.CommonMethods
{
    /// <summary>
    /// File Helper class
    /// </summary>
    public class FileHelper
    {
        /// <summary>
        /// Logic to upload file to Azure Blob
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="file"> File as IFormFile</param>
        /// <param name="path">Path to Azure Blob Service</param>
        /// <param name="container">Container in Azure Blob Service</param>
        /// <return> Boolean, if proccess is success, flag is true, if not, flag is false
        public static async Task<bool> UploadFileAsync(string fileName, IFormFile file, string path, string container)
        {
            if (file == null || file.Length == 0)
            {
                return false;
            }

            try
            {
                // Create Azure BlobServiceClient
                BlobServiceClient serviceClient = new BlobServiceClient(path);

                // Create Azure BlobContainerClient
                BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container);

                // Check if target container exists
                if (!await containerClient.ExistsAsync())
                {
                    // If target container does not exist, create one
                    await containerClient.CreateIfNotExistsAsync();
                }

                // Set path to upload file
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Check if file exists
                if (await blobClient.ExistsAsync())
                {
                    // File exists, delete this to avoid duplicate the same file name
                    await blobClient.DeleteAsync();
                }

                // Upload file
                await containerClient.UploadBlobAsync(fileName, file.OpenReadStream());

                return true;
            } 
            catch (Azure.RequestFailedException ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Get mehotd, download files from Azure Blob
        /// </summary>
        /// <param name="file"> File as IFormFile</param>
        /// <param name="path">Path to Azure Blob Service</param>
        /// <param name="container">Container in Azure Blob Service</param>
        /// <return> File
        public static async Task<FileContentResult> DownloadFileAsync(string fileName, string path, string container)
        {
            try
            {
                // Create Azure BlobServiceClient instance with path
                BlobServiceClient serviceClient = new BlobServiceClient(path);

                // Create Azure BlobContainerClient instance
                BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container);

                // Crate Azure BlobClient with fileName
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Start transaction
                using (var response = await blobClient.OpenReadAsync())
                {
                    using (var memory = new MemoryStream())
                    {
                        // Copy to allocated memory
                        await response.CopyToAsync(memory);

                        var contentType = "application/octet-stream";

                        // Return File, downloading file will begin
                        return new FileContentResult(memory.ToArray(), contentType)
                        {
                            FileDownloadName = fileName
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while downloading the file.", ex);
            }
        }
    }
}
