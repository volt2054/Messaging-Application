using System;
using System.IO;
using System.Net;
using System.Threading;

class ContentDeliveryServer {
    static void Main() {
        // Get the current directory of the executable
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Create the 'static' folder if it doesn't exist
        string staticFolder = Path.Combine(currentDirectory, "static");
        if (!Directory.Exists(staticFolder)) {
            Directory.CreateDirectory(staticFolder);
        }

        // Use the 'static' folder as the root directory for serving and storing files
        string rootDirectory = staticFolder;
        string url = "http://localhost:7257/";

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);

        Console.WriteLine($"Listening for requests on {url}");

        listener.Start();

        while (true) {
            HttpListenerContext context = listener.GetContext();

            ThreadPool.QueueUserWorkItem((_) => {
                try {
                    if (context.Request.HttpMethod == "GET") {
                        // Handle GET requests for static files as before
                        HandleStaticFileRequest(context, rootDirectory);
                    } else if (context.Request.HttpMethod == "POST") {
                        // Handle POST requests for file uploads
                        HandleFileUpload(context, rootDirectory);
                    } else {
                        // Unsupported HTTP method
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error handling request: {ex.Message}");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                } finally {
                    context.Response.Close();
                }
            }, null);
        }
    }

    private static void HandleStaticFileRequest(HttpListenerContext context, string rootDirectory) {
        string requestedUrl = context.Request.Url.LocalPath;
        string filePath = Path.Combine(rootDirectory, requestedUrl.TrimStart('/'));

        if (File.Exists(filePath)) {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string extension = Path.GetExtension(filePath);
            string contentType = GetContentType(extension);
            context.Response.ContentType = contentType;
            context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
        } else {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    private static void HandleFileUpload(HttpListenerContext context, string rootDirectory) {
        if (context.Request.HasEntityBody) {
            using (Stream body = context.Request.InputStream) {
                using (StreamReader reader = new StreamReader(body)) {
                    // Read the entire request body (uploaded file content)
                    string content = reader.ReadToEnd();

                    // Get the filename from the Content-Disposition header
                    string fileName = GetFileNameFromContentDisposition(context.Request.Headers["Content-Disposition"]);

                    // Combine the filename with the root directory to get the full filepath
                    string filePath = Path.Combine(rootDirectory, fileName);

                    // Write the content to the file
                    File.WriteAllText(filePath, content);

                    // Respond with a success message
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes("File uploaded successfully.");
                    context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
        } else {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }

    private static string GetFileNameFromContentDisposition(string contentDisposition) {
        string[] elements = contentDisposition.Split(';');
        foreach (var element in elements) {
            if (element.Trim().StartsWith("filename=")) {
                return element.Trim().Substring("filename=".Length).Trim('"');
            }
        }
        return null;
    }

    private static string GetContentType(string extension) {
        switch (extension.ToLower()) {
            case ".png":
                return "image/png";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".gif":
                return "image/gif";
            case ".txt":
                return "text/plain";
            default:
                return "application/octet-stream";
        }
    }
}
