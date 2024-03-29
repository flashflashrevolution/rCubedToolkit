﻿using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using rCubedToolkit.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using static rCubedToolkit.Http;

namespace rCubedToolkit
{
    public class Utils
    {
        public static bool IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public static string ExePath = Process.GetCurrentProcess().MainModule.FileName;
        public static string URLProtocol = "ffrgame";

        public static void StartAsAdmin(string Arguments, bool Close = false)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                process.StartInfo.Arguments = Arguments;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";

                try
                {
                    process.Start();

                    if (!Close)
                    {
                        process.WaitForExit();
                    }
                }
                catch
                {
                    MessageBox.Show("Unable to start as Admin");
                }

                if (Close) Application.Current.Shutdown();
            }
        }

        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static bool CheckInstallPath(string path)
        {
            if (File.Exists(Path.Combine(path, "R3.exe")) && File.Exists(GetApplicationXML(path)))
            {
                SetInstallDir(path, "R3.exe", "standard");
                return true;
            }
            if (File.Exists(Path.Combine(path, "R3Air.exe")) && File.Exists(GetApplicationXML(path)))
            {
                SetInstallDir(path, "R3Air.exe", "hybrid");
                return true;
            }
            return false;
        }

        public static bool GetInstallDir()
        {
            // Check Current Folder
            string path = Path.GetDirectoryName(ExePath);
            if (CheckInstallPath(path))
                return true;

            // Check Settings
            path = Settings.Default.InstallFolder;
            if (CheckInstallPath(path))
                return true;

            return false;
        }

        public static string SetInstallDir(string directory, string exe, string edition)
        {
            App.InstallFolder = directory;
            App.InstallExe = exe;
            App.InstallEdition = edition;
            Settings.Default.InstallFolder = directory;
            Settings.Default.InstallExe = exe;
            Settings.Default.InstallEditon = edition;
            Settings.Default.Save();
            return directory;
        }

        internal static void UpdateEdtion(string edition)
        {
            App.InstallEdition = edition;
            Settings.Default.InstallEditon = edition;
            Settings.Default.Save();
        }

        public static string GetApplicationXML(string directory)
        {
            return Path.Combine(directory, "META-INF", "AIR", "application.xml");
        }

        public static void LoadApplicationXML(Dictionary<string, string> dict, XElement node, string parentName = "")
        {
            foreach (XElement nodeElems in node.Elements())
            {
                if (nodeElems.HasElements)
                    LoadApplicationXML(dict, nodeElems, parentName + nodeElems.Name.LocalName + "_");
                else
                    dict[parentName + nodeElems.Name.LocalName] = nodeElems.Value;
            }
        }


        public static bool GetManualDir()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Please Select the Game",
                Filter = "rCubed|R3.exe;R3Air.exe",
                FileName = "R3Air.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                Console.WriteLine(dialog.FileName);
                string path = Path.GetDirectoryName(dialog.FileName);

                if (CheckInstallPath(path))
                    return true;
            }
            return false;
        }

        public static bool GetManualDirFolder()
        {
            return false;

            // CommonOpenFileDialog requires a seperate dll to be included along with the app, 
            // which is roughtly 2x the current size of the compiled app, just for a decent folder selection.
            // For now having the user select a pre-existing download serves this apps purpose fine as it
            // should be included in the games download already.
            /*
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                InitialDirectory = Path.GetDirectoryName(ExePath),
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (CheckInstallPath(dialog.FileName))
                    return true;
                else
                {
                    SetInstallDir(dialog.FileName, "", "");
                    return true;
                }
            }
            return false;
            */
        }

        public static void SendNotify(string message, string title = null)
        {
            string defaultTitle = "rCubed Toolkit";

            new ToastContentBuilder()
                .AddText(title ?? defaultTitle)
                .AddText(message)
                .Show();
        }

        public static async Task Download(string link, string output)
        {
            var resp = await HttpClient.GetAsync(link);
            using (var stream = await resp.Content.ReadAsStreamAsync())
            using (var fs = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await stream.CopyToAsync(fs);
            }
        }

        private delegate void ShowMessageBoxDelegate(string Message, string Caption);

        private static void ShowMessageBox(string Message, string Caption)
        {
            MessageBox.Show(Message, Caption);
        }

        public static void ShowMessageBoxAsync(string Message, string Caption)
        {
            ShowMessageBoxDelegate caller = new ShowMessageBoxDelegate(ShowMessageBox);
            caller.BeginInvoke(Message, Caption, null, null);
        }

        public static void ShowMessageBoxAsync(string Message)
        {
            ShowMessageBoxDelegate caller = new ShowMessageBoxDelegate(ShowMessageBox);
            caller.BeginInvoke(Message, null, null, null);
        }

        /// <summary>
        /// Attempts to write the specified string to the <see cref="System.Windows.Clipboard"/>.
        /// </summary>
        /// <param name="text">The string to be written</param>
        public static void SetClipboard(string text)
        {
            bool success = false;
            try
            {
                Clipboard.SetText(text);
                success = true;
            }
            catch (Exception)
            {
                // Swallow exceptions relating to writing data to clipboard.
            }

            // This could be placed in the try/catch block but we don't
            // want to suppress exceptions for non-clipboard operations
            if (success)
            {
                SendNotify($"Copied text to clipboard");
            }
        }
    }
}
