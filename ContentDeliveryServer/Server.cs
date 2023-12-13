using System.Net;
using Newtonsoft.Json;

class ContentDeliveryServer {

    private static string filenameMappingsFilePath = "filenameMappings.json";

    // Save filenameMappings to file
    private static void SaveFilenameMappings() {
        string mappingsJson = JsonConvert.SerializeObject(filenameMappings);
        File.WriteAllText(filenameMappingsFilePath, mappingsJson);
        Console.WriteLine("Filename mappings saved to file.");
    }

    // Load filenameMappings from file
    private static void LoadFilenameMappings() {
        if (File.Exists(filenameMappingsFilePath)) {
            string mappingsJson = File.ReadAllText(filenameMappingsFilePath);
            filenameMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(mappingsJson);
            Console.WriteLine("Filename mappings loaded from file.");
        }
    }

    static void Main() {

        LoadFilenameMappings();

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        // Get the current directory of the executable
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Create the 'static' folder if it doesn't exist
        string staticFolder = Path.Combine(currentDirectory, "static");
        if (!Directory.Exists(staticFolder)) {
            Directory.CreateDirectory(staticFolder);
        }

        // Use the 'static' folder as the root directory for serving and storing files
        string rootDirectory = staticFolder;

        string url = "http://127.0.0.1:7257/";

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

    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e) {
        SaveFilenameMappings();
    }

    private static void HandleStaticFileRequest(HttpListenerContext context, string rootDirectory) {
        string requestedUrl = context.Request.Url.LocalPath;
        string FileName = requestedUrl.TrimStart('/');
        string filePath = Path.Combine(rootDirectory, FileName);

        if (File.Exists(filePath)) {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            string extension = Path.GetExtension(filePath);
            string contentType = GetContentType(extension);
            context.Response.ContentType = contentType;
            string originalFileName = GetOriginalFileName(FileName);
            context.Response.Headers.Add("X-Original-File-Name", originalFileName);
            Console.WriteLine($"[DOWNLOAD] {FileName}->{originalFileName}");
            context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
        } else {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    private static Dictionary<string, string> filenameMappings = new Dictionary<string, string>();
    private static void HandleFileUpload(HttpListenerContext context, string rootDirectory) {
        if (context.Request.HasEntityBody) {
            using (Stream body = context.Request.InputStream) {

                // Get the filename from the Content-Disposition header
                string fileName = GetFileNameFromContentDisposition(context.Request.Headers["Content-Disposition"]);

                // Randomized filename created to avoid file overlap
                string randomizedFileName = GenerateRandomFileName(fileName);

                Console.WriteLine($"[UPLOAD] {fileName}->{randomizedFileName}");

                // Combine the filename with the root directory to get the full filepath
                string filePath = Path.Combine(rootDirectory, randomizedFileName);

                // Write the content to the file
                using (FileStream fileStream = File.Create(filePath)) {
                    body.CopyTo(fileStream);
                }

                filenameMappings[randomizedFileName] = fileName;

                // Respond with a success message
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes($"{randomizedFileName}");
                context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            }
        } else {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
    private static string GenerateRandomFileName(string originalFileName) {
        string extension = Path.GetExtension(originalFileName);
        string randomizedFileName = Guid.NewGuid().ToString("N") + extension;
        return randomizedFileName;
    }

    private static string GetOriginalFileName(string randomizedFileName) {
        if (filenameMappings.ContainsKey(randomizedFileName)) {
            return filenameMappings[randomizedFileName];
        }
        return null;
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
