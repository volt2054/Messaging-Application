using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

using static SharedLibrary.WebSocketMetadata;      
                                               
namespace SharedLibrary {

    public static class ContentDeliveryInterface {

        private static readonly Dictionary<string, string> FileNameMapping = new Dictionary<string, string>();

        public static async Task<string> UploadFileAsync(string filePath) {
            using (HttpClient client = new HttpClient()) {
                try {
                    // Read the file content
                    byte[] fileContent = await File.ReadAllBytesAsync(filePath);

                    // Create a ByteArrayContent from the file content
                    ByteArrayContent content = new ByteArrayContent(fileContent);
                    string fileName = Path.GetFileName(filePath);
                    content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") {
                        FileName = fileName,
                    };

                    // Send a POST request to the server URL with the file content
                    HttpResponseMessage response = await client.PostAsync(WebSocketMetadata.CDSERVER_URL, content);
                    string randomizedFileName = await response.Content.ReadAsStringAsync();

                    // Check if the request was successful (status code 200 OK)
                    if (response.IsSuccessStatusCode) {
                        Console.WriteLine($"File uploaded successfully to: {WebSocketMetadata.CDSERVER_URL}");
                        return randomizedFileName;
                    } else {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                    return null;
                }
            }
        }

        public static string GetOriginalFileName(string uuid) {
            FileNameMapping.TryGetValue(uuid, out var originalFileName);
            return originalFileName;
        }

        public static async Task<string> DownloadFileAsync(string fileName, string saveDirectory) {
            string fileUrl = $"{WebSocketMetadata.CDSERVER_URL}{fileName}";
            using (HttpClient client = new HttpClient()) {
                try {
                    // Send a GET request to the file URL
                    HttpResponseMessage response = await client.GetAsync(fileUrl);
                    // Check if the request was successful (status code 200 OK)
                    if (response.IsSuccessStatusCode) {
                        string originalFileName = "";
                        if (response.Headers.TryGetValues("X-Original-File-Name", out var originalFileNames)) {
                            originalFileName = originalFileNames.FirstOrDefault();
                        }
                        if (originalFileName == "") {
                            originalFileName = "pfp.png";
                        }

                        byte[] fileContent = await response.Content.ReadAsByteArrayAsync();

                        string uuid = Guid.NewGuid().ToString();
                        string randomizedFileName = $"{uuid}{Path.GetExtension(originalFileName)}";
                        string savePath = Path.Combine(saveDirectory, randomizedFileName);

                        FileNameMapping.Add(uuid, originalFileName);

                        // Need to add cache system
                        await File.WriteAllBytesAsync(savePath, fileContent);
                        return savePath;
                    } else {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return "";
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                    return "";
                }
            }
        }
    }
}

