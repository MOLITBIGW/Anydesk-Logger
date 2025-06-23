using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AnydeskLogger
{
    class Program
    {
        static string logFile = Config.Log;
        static string targetProcessName = Config.Anydesk;
        static CancellationTokenSource cts = new CancellationTokenSource();

        static HashSet<string> loggedConnections = new HashSet<string>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("                        _           _      _                             ");
            Console.WriteLine("   __ _ _ __  _   _  __| | ___  ___| | __ | | ___   __ _  __ _  ___ _ __ ");
            Console.WriteLine("  / _` | '_ \\| | | |/ _` |/ _ \\/ __| |/ / | |/ _ \\ / _` |/ _` |/ _ \\ '__|");
            Console.WriteLine(" | (_| | | | | |_| | (_| |  __/\\__ \\   <  | | (_) | (_| | (_| |  __/ |   ");
            Console.WriteLine("  \\__,_|_| |_|\\__, |_|\\__,_|\\___||___/_|\\_\\ |_|\\___/ \\__, |\\__, |\\___|_|   ");
            Console.WriteLine("              |___/                                |___/ |___/            ");
            Console.WriteLine();

            await Task.Run(() => MonitorTargetedConnections(targetProcessName, cts.Token));
        }

        static void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(logFile, message + Environment.NewLine);
        }

        static void MonitorTargetedConnections(string targetName, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ParseNetstatOutput(targetName).GetAwaiter().GetResult();
                Thread.Sleep(1000);
            }
        }

        static async Task ParseNetstatOutput(string targetName)
        {
            try
            {
                var psi = new ProcessStartInfo("netstat", "-ano")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process proc = Process.Start(psi))
                {
                    string output = await proc.StandardOutput.ReadToEndAsync();
                    proc.WaitForExit();

                    var regex = new Regex(@"\s+TCP\s+([\d.]+):(\d+)\s+([\d.]+):(\d+)\s+ESTABLISHED\s+(\d+)");
                    var matches = regex.Matches(output);

                    foreach (Match match in matches)
                    {
                        var remoteIp = match.Groups[3].Value;
                        var remotePort = match.Groups[4].Value;
                        var pid = match.Groups[5].Value;

                        if (remoteIp == "0.0.0.0" || remotePort == "0" || remotePort == "80" || remotePort == "443")
                            continue;

                        string connectionKey = $"{remoteIp}:{pid}";
                        if (loggedConnections.Contains(connectionKey))
                        {
                            continue;
                        }

                        try
                        {
                            var tasklist = new ProcessStartInfo("tasklist", $"/FI \"PID eq {pid}\"")
                            {
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using (Process taskProc = Process.Start(tasklist))
                            {
                                string taskOutput = await taskProc.StandardOutput.ReadToEndAsync();
                                taskProc.WaitForExit();

                                if (taskOutput.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    LogMessage($"Process: {targetName}");
                                    LogMessage($"Remote IP: {remoteIp}");
                                    LogMessage($"PID: {pid}");
                                    LogMessage($"Join Our Server: https://discord.gg/WCsugqQupZ");

                                    loggedConnections.Add(connectionKey);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error processing PID {pid}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage($"Error executing netstat: {e.Message}");
            }
        }
    }
}
