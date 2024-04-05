namespace Server {
    public enum LoggingLevel {
        Low,
        Medium,
        High
    }

    public class Logger {
        private string filePath;
        private LoggingLevel loggingLevel;

        public Logger(LoggingLevel level) {
            // Set the logging level
            loggingLevel = level;

            // Generate the file name with current date and time
            string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            // Get the current directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // Combine the current directory with the file name to create a relative path
            filePath = Path.Combine(currentDirectory, fileName);
        }

        public void Log(string message, LoggingLevel level) {
            if (level <= loggingLevel) {
                try {
                    // Open the file for appending
                    using (StreamWriter writer = new StreamWriter(filePath, true)) {
                        // Write timestamped message to the file
                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                    }
                } catch (Exception ex) {
                    Console.WriteLine("An error occurred while writing to the log file: " + ex.Message);
                }
            }
        }
    }
}
