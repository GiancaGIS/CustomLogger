using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace CustomLogger
{
    internal class FileLogger
    {
        private readonly string pathFile = string.Empty;
        private readonly object objLock = new Object();

        public FileLogger(string path)
        {
            this.pathFile = path;
        }

        public void LogSync(string messaggio, string intestazione)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(this.pathFile, true))
            {
                file.WriteLine($"{intestazione} {messaggio}");
            }
        }

        public void LogAsync(string messaggio, string intestazione)
        {
            new Task(() =>
            {
                try
                {
                    if (Monitor.TryEnter(objLock, TimeSpan.FromSeconds(15))) // Evito DeadLock dei Thread...
                    {
                        string messaggioErrore = string.Empty;
                        try
                        {
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(this.pathFile, true))
                            {
                                file.WriteLine($"{intestazione} {messaggio}");
                            }
                        }
                        catch (AggregateException ex)
                        {
                            messaggioErrore = ex.Message;
                        }
                        catch (Exception ex)
                        {
                            messaggioErrore = ex.Message;
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(objLock);
                }
            }, TaskCreationOptions.None).Wait();
        }
    }

    public class LogHelper
    {
        private readonly string pathFile = string.Empty;
        private readonly bool isAsync = false;

        public LogHelper(string path, bool async = false)
        {
            this.isAsync = async;

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                throw new ArgumentException("La path al file specificato non esiste!");

            if (string.IsNullOrEmpty(System.IO.Path.GetFileName(path)))
                throw new ArgumentException("Non è stato specificato il nome del file txt!");

            this.pathFile = path;
        }
        private FileLogger logger = null;

        /// <summary>
        /// Restituisce task, dove il result della string è l'eventuale messaggio di errore
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void LogInfo(string message)
        {
            logger = new FileLogger(this.pathFile);
            if (this.isAsync) logger.LogAsync(message, $"[{DateTime.Now}]|Info|:");
            else logger.LogSync(message, $"[{DateTime.Now}]|Info|:");
        }

        public void LogDebug(string message)
        {
            logger = new FileLogger(this.pathFile);
            if (this.isAsync) logger.LogAsync(message, $"[{DateTime.Now}]|Debug|:");
            else logger.LogSync(message, $"[{DateTime.Now}]|Debug|:");
        }

        public void LogWarning(string message)
        {
            logger = new FileLogger(this.pathFile);
            if (this.isAsync) logger.LogAsync(message, $"[{DateTime.Now}]|Warning|:");
            else logger.LogSync(message, $"[{DateTime.Now}]|Warning|:");
        }

        public void LogError(string message)
        {
            logger = new FileLogger(this.pathFile);
            if (this.isAsync) logger.LogAsync(message, $"[{DateTime.Now}]|Error|:");
            else logger.LogSync(message, $"[{DateTime.Now}]|Error|:");
        }
    }
}
