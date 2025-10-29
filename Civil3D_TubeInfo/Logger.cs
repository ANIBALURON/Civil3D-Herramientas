using System;
using System.IO;
using System.Text;

namespace Civil3D_TubeInfo
{
    /// <summary>
    /// Servicio centralizado de logging para la aplicación Civil3D_TubeInfo.
    /// Maneja registro de eventos, advertencias y errores en archivo y consola.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath;
        private static readonly object LockObject = new object();

        static Logger()
        {
            // Crear carpeta de logs en AppData
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logFolder = Path.Combine(appDataFolder, "Civil3D_TubeInfo", "Logs");

            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            LogFilePath = Path.Combine(logFolder, $"TubeInfo_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// Registra un mensaje informativo.
        /// </summary>
        public static void Info(string message)
        {
            LogMessage("INFO", message);
        }

        /// <summary>
        /// Registra una advertencia.
        /// </summary>
        public static void Warning(string message)
        {
            LogMessage("WARNING", message);
        }

        /// <summary>
        /// Registra un error con stack trace.
        /// </summary>
        public static void Error(string message, Exception ex = null)
        {
            StringBuilder sb = new StringBuilder(message);
            if (ex != null)
            {
                sb.AppendLine($"\nExcepción: {ex.GetType().Name}");
                sb.AppendLine($"Detalle: {ex.Message}");
                sb.AppendLine($"Stack Trace: {ex.StackTrace}");
            }
            LogMessage("ERROR", sb.ToString());
        }

        /// <summary>
        /// Registra un mensaje de depuración.
        /// </summary>
        public static void Debug(string message)
        {
            #if DEBUG
            LogMessage("DEBUG", message);
            #endif
        }

        /// <summary>
        /// Obtiene la ruta del archivo de log actual.
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        private static void LogMessage(string level, string message)
        {
            lock (LockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{level}] {message}";

                    // Escribir en archivo
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Si falla el logging, no lanzar excepción
                }
            }
        }

        /// <summary>
        /// Abre el archivo de log en el explorador.
        /// </summary>
        public static void OpenLogFile()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", LogFilePath);
                }
            }
            catch (Exception ex)
            {
                Error("No se pudo abrir el archivo de log", ex);
            }
        }
    }
}
