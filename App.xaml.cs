using rCubedToolkit.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;

namespace rCubedToolkit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static int Arch = Environment.Is64BitOperatingSystem ? 64 : 32;

        public static string InstallFolder;
        public static string InstallExe;
        public static string InstallEdition;

        public static string SWFMD5;
        public static JSONGameVersion swfVersion;

        public static string Arguments;
        public static MainWindow window;
        public static bool GUI = true;

        public static Dictionary<string, string> xmlDescriptor;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            //Settings.Default.Reset();
            Version = Version.Substring(0, Version.Length - 2); // Remove Revision
            
            // Check for Install Here
            Utils.GetInstallDir();

            // Previous Folder Set
            if(!string.IsNullOrEmpty(Settings.Default.InstallFolder) && Directory.Exists(Settings.Default.InstallFolder) && string.IsNullOrEmpty(Settings.Default.InstallExe))
            {
                Utils.SetInstallDir(Settings.Default.InstallFolder, "", "");
            }

            // Install wasn't located, ask user to find install.
            while (string.IsNullOrEmpty(App.InstallFolder))
            {
                var diagBox = System.Windows.Forms.MessageBox.Show("\"R3Air\" Install couldn't be found, please select the folder it is located in or Cancel to select the current folder.", "Select \"R3Air\" Folder", System.Windows.Forms.MessageBoxButtons.OKCancel);
                if (diagBox == System.Windows.Forms.DialogResult.OK)
                {
                    Utils.GetManualDir();
                }
                else
                {
                    Utils.SetInstallDir(Path.GetDirectoryName(Utils.ExePath), "", "");
                }
            }

            await GameVersions.GetVersions();

            ReloadXML();

            ArgumentHandler(e.Args);

            if (GUI)
            {
                window = new MainWindow();
                window.Show();
            }
            else
            {
                Environment.Exit(0);
            }
        }

        public static void ReloadXML()
        {
            xmlDescriptor = new Dictionary<string, string>();
            SWFMD5 = "";
            swfVersion = null;

            var xmlPath = Utils.GetApplicationXML(InstallFolder);

            // Load Application XML for selected install.
            if (!String.IsNullOrEmpty(App.InstallFolder) && File.Exists(xmlPath))
            {
                XElement xml = XElement.Load(xmlPath);
                Utils.LoadApplicationXML(xmlDescriptor, xml);
                /*
                foreach (KeyValuePair<string, string> kvp in xmlDescriptor)
                {
                    Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
                }
                */
            }

            // Get Accurate Version using swf hash.
            if (xmlDescriptor.TryGetValue("initialWindow_content", out string swfPath))
            {
                SWFMD5 = Utils.CalculateMD5(Path.Combine(InstallFolder, swfPath));
                Console.WriteLine("--------------\nSWF Hash: " + SWFMD5);

                bool isVerified = false;

                foreach (JSONGameVersion ver in GameVersions.VersionList)
                {
                    if (ver.swf_hash == SWFMD5)
                    {
                        swfVersion = ver;
                        isVerified = true;
                        break;
                    }
                }

                if (isVerified)
                {
                    if (swfVersion.edition != InstallEdition)
                        Utils.UpdateEdtion(swfVersion.edition);
                    xmlDescriptor["versionNumber"] = swfVersion.version;
                }
                else
                {
                    if (xmlDescriptor.TryGetValue("versionNumber", out string newVersion))
                        newVersion += " [Unknown]";
                    else
                        newVersion = "Unknown - " + SWFMD5;

                    xmlDescriptor["versionNumber"] = newVersion;
                }
            }
        }

        private void ArgumentHandler(string[] args)
        {
            Arguments = string.Join(" ", args);
            while (args.Length > 0)
            {
                switch (args[0])
                {
                    case "--register":
                        if(!string.IsNullOrEmpty(App.InstallExe))
                            Utils.Register(true);
                        GUI = false;
                        args = Shift(args, 1);
                        break;

                    case "--unregister":
                        if (!string.IsNullOrEmpty(App.InstallExe))
                            Utils.Unregister(true);
                        GUI = false;
                        args = Shift(args, 1);
                        break;

                    default:
                        Utils.SendNotify("Unrecognized Argument");
                        args = Shift(args);
                        break;
                }
            }
        }

        private static string[] Shift(string[] array, int places = 1)
        {
            if (places >= array.Length) return Array.Empty<string>();
            string[] newArray = new string[array.Length - places];
            for (int i = places; i < array.Length; i++)
            {
                newArray[i - places] = array[i];
            }

            return newArray;
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"UnhandledException: {e.Exception}", "Exception", MessageBoxButton.OK, MessageBoxImage.Warning);

            e.Handled = true;
            Application.Current.Shutdown();
        }
    }
}
