using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace sscs2023
{
    public class Cloudflare
    {
        public static void StartCloudflared()
        {
            try
            {
                string cf = "/home/container/cloudflared";

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    Process.Start("chmod", $"+x {cf}").WaitForExit();
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = cf,
                    Arguments = "tunnel --config /home/container/.cloudflared/config.yml run",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process process = new Process { StartInfo = startInfo };
                process.Start();

                Console.WriteLine("[Cloudflared] Tunnel started.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Cloudflared] Failed to start: {e.Message}");
            }
        }
    }
}