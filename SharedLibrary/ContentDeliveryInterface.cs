using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace SharedLibrary {

    public static class ContentDeliveryInterface {
        public static async Task<string> UploadFileAsync(string serverUrl, string filePath) {
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
                    HttpResponseMessage response = await client.PostAsync(serverUrl, content);
                    string randomizedFileName = await response.Content.ReadAsStringAsync();

                    // Check if the request was successful (status code 200 OK)
                    if (response.IsSuccessStatusCode) {
                        Console.WriteLine($"File uploaded successfully to: {serverUrl}");
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

        public static async Task DownloadFileAsync(string serverUrl, string fileName, string saveDirectory) {
            string fileUrl = $"{serverUrl}/{fileName}";
            using (HttpClient client = new HttpClient()) {
                try {
                    // Send a GET request to the file URL
                    HttpResponseMessage response = await client.GetAsync(fileUrl);

                    // Check if the request was successful (status code 200 OK)
                    if (response.IsSuccessStatusCode) {
                        string originalFileName = "file.txt";
                        if (response.Headers.TryGetValues("X-Original-File-Name", out var originalFileNames)) {
                            originalFileName = originalFileNames.FirstOrDefault();
                        }

                        // Read and save the file content to the specified path
                        byte[] fileContent = await response.Content.ReadAsByteArrayAsync();
                        string savePath = Path.Combine(saveDirectory, originalFileName);
                        await File.WriteAllBytesAsync(savePath, fileContent);
                        Console.WriteLine($"File downloaded successfully to: {savePath}");
                    } else {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }

}

