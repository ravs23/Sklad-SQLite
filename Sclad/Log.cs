using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sklad
{
    class Log
    {
        static string logFileName = "Sklad_log.txt";

        public static void LogWrite(object msg)
        {
            if (Settings.Logging)
            {
                using (StreamWriter writer = File.AppendText(logFileName))
                {
                    writer.WriteLine("[{0}] {1}", DateTime.Now, msg.ToString());
                }
            }
        }

        public static Task LogWriteAsync(string msg)
        {
            return Task.Factory.StartNew(LogWrite, msg);
        }

        public static void CheckSizeLogFile()
        {
            if (File.Exists(logFileName) && new FileInfo(logFileName).Length > 104857600)
            {
                if (DialogResult.Yes == MessageBox.Show("Лог-файл (Sklad_log.txt) достиг размера 100 Mb." + Environment.NewLine +
                                "Рекомендуется его удалить или заархивировать, если он необходим." + Environment.NewLine + Environment.NewLine +
                                "Открыть папку, содержащую этот файл?", "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                                )
                {
                    System.Diagnostics.Process.Start(".");
                }
            }
        }

    }
}
