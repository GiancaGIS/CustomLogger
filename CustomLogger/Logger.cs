using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger
{
    internal class FileLogger
    {
        private readonly string pathFile = string.Empty;
        private readonly object objLock = new object();
        private List<Task> listaTask = null;

        public FileLogger(string path, ref List<Task> tasks)
        {
            this.pathFile = path;
            this.listaTask = tasks;
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
            this.listaTask.Add(
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
            }, TaskCreationOptions.None));
        }
    }

    public class LogHelper
    {
        private readonly string pathFile = string.Empty;
        private readonly bool isAsync = false;
        private readonly bool startTask = false;
        private FileLogger logger = null;

        /// <summary>
        /// Lista di tutti i task di scrittura dei log in modalità async, dovranno essere avviati
        /// e aspettati con apposito metodo EseguiTuttiTaskScritturaLog
        /// </summary>
        public List<Task> ListaTaskScritturaLog = new List<Task>();


        /// <summary>
        /// Costruttore della classe
        /// </summary>
        /// <param name="path">Path completa al file di Log</param>
        /// <param name="async">True per scrivere in modalità async</param>
        /// <param name="startTaskIfAsync"></param>
        public LogHelper(string path, bool async = false, bool startTaskIfAsync = false)
        {
            this.isAsync = async;
            this.startTask = startTaskIfAsync;

            if (!this.isAsync) this.startTask = false;

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                throw new ArgumentException("La path al file specificato non esiste!");

            if (string.IsNullOrEmpty(System.IO.Path.GetFileName(path)))
                throw new ArgumentException("Non è stato specificato il nome del file di log!");

            this.pathFile = path;
        }


        /// <summary>
        /// Esegue i task di scrittura di tutti i log in modalità async da scrivere
        /// </summary>
        public void EseguiTuttiTaskScritturaLog()
        {
            this.ListaTaskScritturaLog.ForEach(task =>
            {
                task.Start();
                task.Wait();
            });

        }

        /// <summary>
        /// Restituisce task, dove il result della string è l'eventuale messaggio di errore
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void LogInfo(string message)
        {
            logger = new FileLogger(this.pathFile, ref this.ListaTaskScritturaLog);
            if (this.isAsync)
            {
                logger.LogAsync(message, $"[{DateTime.Now}]|Info|:");

                if (this.startTask) this.EseguiTuttiTaskScritturaLog();
            }
            else logger.LogSync(message, $"[{DateTime.Now}]|Info|:");
        }

        public void LogDebug(string message)
        {
            logger = new FileLogger(this.pathFile, ref this.ListaTaskScritturaLog);
            if (this.isAsync)
            {
                logger.LogAsync(message, $"[{DateTime.Now}]|Debug|:");
                if (this.startTask) this.EseguiTuttiTaskScritturaLog();
            }
            else logger.LogSync(message, $"[{DateTime.Now}]|Debug|:");
        }

        public void LogWarning(string message)
        {
            logger = new FileLogger(this.pathFile, ref this.ListaTaskScritturaLog);
            if (this.isAsync)
            {
                logger.LogAsync(message, $"[{DateTime.Now}]|Warning|:");
                if (this.startTask) this.EseguiTuttiTaskScritturaLog();
            }
            else logger.LogSync(message, $"[{DateTime.Now}]|Warning|:");
        }

        public void LogError(string message)
        {
            logger = new FileLogger(this.pathFile, ref this.ListaTaskScritturaLog);
            if (this.isAsync)
            {
                logger.LogAsync(message, $"[{DateTime.Now}]|Error|:");
                if (this.startTask) this.EseguiTuttiTaskScritturaLog();
            }
            else logger.LogSync(message, $"[{DateTime.Now}]|Error|:");
        }
    }
}
