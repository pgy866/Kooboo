﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Threading;
using Kooboo.Mail.Utility;

namespace Kooboo.Mail
{
    public class SocketMonitor : IDisposable
    {
        public static SocketMonitor Instance = new SocketMonitor();

        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
        public static readonly int WarningLine = 4096;
        public static readonly int MaxWarningTimes = 3;
        public static readonly string AlertAddress = "alert@kooboo.com";
        public static string[] AdminEmails = new string[]
        {
            "suning@kooboo.com"
        };

        static SocketMonitor()
        {
            var adminEmails = ConfigurationManager.AppSettings.Get("AdminEmails");
            if (!String.IsNullOrEmpty(adminEmails))
            {
                AdminEmails = adminEmails.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            }

            var warningLine = ConfigurationManager.AppSettings.Get("MaxLinuxFiles");
            if (!String.IsNullOrEmpty(warningLine))
            {
                try
                {
                    WarningLine = Convert.ToInt32(warningLine);
                }
                catch
                {
                }
            }
        }

        private Timer _timer;
        private bool _started;
        private int _executing;
        private int _warningTimes;

        public void Start()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return;

            if (_started)
                throw new InvalidOperationException("Heartbeat can only be started one time");

            _started = true;
            _timer = new Timer(TimerCallback, state: this, dueTime: Interval, period: Interval);
        }

        private void TimerCallback(object state)
        {
            ((SocketMonitor)state).OnTimerCallback();
        }

        internal void OnTimerCallback()
        {
            if (Interlocked.Exchange(ref _executing, 1) != 0)
                return;

            try
            {
                CheckOpenedFiles();
            }
            catch (Exception ex)
            {
                Kooboo.Data.Log.Instance.Exception.Write(DateTime.UtcNow.ToString() + " " + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ex.Source);
            }
            finally
            {
                Interlocked.Exchange(ref _executing, 0);
            }
        }

        private void CheckOpenedFiles()
        {
            var pid = Process.GetCurrentProcess().Id;

            var countStr = RunCommand($"lsof -p {pid} | wc -l");
            var count = Convert.ToInt32(countStr);

            Console.WriteLine($"SocketMonitor: {count} opened files");

            if (count >= WarningLine)
            {
                SendWarning();
            }
        }

        private void SendWarning()
        {
            if (_warningTimes >= MaxWarningTimes)
                return;

            Console.WriteLine($"SocketMonitor: send warning {_warningTimes + 1}");

            DoSendWaring();

            _warningTimes++;
        }

        private void DoSendWaring()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var msg = $"Server {"TBD"} opened files Overflow at {timestamp}(UTC)";
            foreach (var each in AdminEmails)
            {
                var content = ComposeUtility.ComposeTextEmailBody(
                    AlertAddress, 
                    each,
                    $"[KoobooAlert] {msg}",
                    msg
                );
                _ = Transport.Delivery.Send(AlertAddress, each, content);
                Console.WriteLine($"SocketMonitor: send warning to {each}");
            }
        }

        private string RunCommand(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return result;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
