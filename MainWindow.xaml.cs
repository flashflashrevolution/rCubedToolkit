using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static rCubedToolkit.Http;

namespace rCubedToolkit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JSONGameVersion selectedVersionCombo;
        private ObservableCollection<ComboBoxItem> versionListCombo = new ObservableCollection<ComboBoxItem>();
        private Dictionary<string, int> version_counts = new Dictionary<string, int>();

        public MainWindow()
        {
            // Version Counts
            foreach (JSONGameVersion ver in GameVersions.VersionList)
            {
                string key = ver.edition + "_" + ver.version;

                if (version_counts.ContainsKey(key))
                    version_counts[key] += 1;
                else
                    version_counts.Add(key, 1);
            }

            // Init GUI
            InitializeComponent();

            UpdateUIComponent();
        }

        public void UpdateUIComponent()
        {
            // Edition
            if (App.InstallEdition != "")
            {
                lbl_xmlEdition.Content = char.ToUpper(App.InstallEdition[0]) + App.InstallEdition.Substring(1);
                downloadUpdateBtn.Content = "Change Version";
                toggleURLBtn.IsEnabled = true;
            }
            else
            {
                lbl_xmlEdition.Content = "---";
                downloadUpdateBtn.Content = "Download";
                toggleURLBtn.IsEnabled = false;
            }

            // Version
            if (App.xmlDescriptor.TryGetValue("versionNumber", out string xmlVersion))
                lbl_xmlVersion.Content = xmlVersion;
            else
                lbl_xmlVersion.Content = "---";

            // Path
            lbl_xmlPath.Content = App.InstallFolder + Path.DirectorySeparatorChar + App.InstallExe;
            lbl_xmlPath.ToolTip = App.InstallFolder + Path.DirectorySeparatorChar + App.InstallExe;

            // Edition Radios
            if (App.InstallEdition == "hybrid")
                editionHybridRadio.IsChecked = true;
            else
                editionStandardRadio.IsChecked = true;

            // URI Handler
            toggleURLBtn.Content = UriRegistration.IsRegistered() ? "Disable URI Handler" : "Enable URI Handler";

            // Version Downdown
            versionDropdown.ItemsSource = versionListCombo;
        }

        public void RebuildVersionDropdown()
        {
            string key;
            string edition = "standard";
            if (editionHybridRadio.IsChecked == true)
                edition = "hybrid";

            if(versionListCombo.Count > 0)
                versionListCombo.Clear();

            ComboBoxItem selectedItem = null;
            foreach (JSONGameVersion ver in GameVersions.VersionList)
            {
                key = ver.edition + "_" + ver.version;
                if (ver.edition == edition)
                {
                    if (App.Arch == 32 && ver.arch == 64)
                        continue;

                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = ver.version,
                        Tag = ver,
                        Name = "Name"
                    };

                    if(version_counts[key] > 1)
                    {
                        item.Content += " [x" + ver.arch + "]";
                    }

                    if (ver.swf_hash == App.SWFMD5)
                    {
                        selectedItem = item;
                        item.Content += " (Current)";
                    }

                    versionListCombo.Add(item);
                }
            }

            if (selectedItem != null)
                versionDropdown.SelectedItem = selectedItem;
            else
                versionDropdown.SelectedIndex = Math.Max(0, versionListCombo.Count - 1);
        }

        private void URLButton_CLick(object sender, RoutedEventArgs e)
        {
            if (UriRegistration.IsRegistered())
            {
                if (UriRegistration.Unregister())
                {
                    Utils.SendNotify("URL Handler Removed");
                    toggleURLBtn.Content = "Enable URI Handler";
                }
            }
            else
            {
                if (UriRegistration.Register())
                {
                    Utils.SendNotify("URL Handler Registered");
                    toggleURLBtn.Content = "Disable URI Handler";
                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if(selectedVersionCombo != null)
            {
                var zipPath = Path.Combine(App.InstallFolder, Path.GetFileName(selectedVersionCombo.url));

                downloadUpdateBtn.IsEnabled = false;

                // Get New Zip
                if (!File.Exists(zipPath))
                {
                    statusLabel.Content = "Downloading " + Path.GetFileName(selectedVersionCombo.url) + "...";
                    await Utils.Download(selectedVersionCombo.url, zipPath);
                }

                // Get Current Version ID
                int MANIFEST_ID = -1;
                foreach (JSONGameVersion ver in GameVersions.VersionList)
                {
                    if (ver.swf_hash == App.SWFMD5)
                    {
                        MANIFEST_ID = ver.id;
                    }
                }

                // Get Manifest Details
                if (MANIFEST_ID >= 0)
                {
                    statusLabel.Content = "Removing Old Version...";
                    var resp = await HttpClient.GetAsync(GameVersions.manifest_url + MANIFEST_ID);
                    var body = await resp.Content.ReadAsStringAsync();
                    var oldManifest = JsonSerializer.Deserialize<List<JSONManifestList>>(body);

                    // Remove Old Files
                    foreach (JSONManifestList file in oldManifest)
                    {
                        var fullPath = Path.Combine(App.InstallFolder, file.path.Replace(@"\", Path.DirectorySeparatorChar.ToString()));
                        var doesExist = File.Exists(fullPath);
                        var fileHash = doesExist ? Utils.CalculateMD5(fullPath) : "0";
                        Console.WriteLine("--------------");
                        Console.WriteLine(file.path + " => " + file.hash);
                        Console.WriteLine(fullPath);
                        Console.WriteLine("Exists: " + (doesExist ? ("true | MD5: " + fileHash + " | Match: " + (fileHash == file.hash ? "true" : "false")) : "false"));

                        if(doesExist && fileHash == file.hash)
                        {
                            File.Delete(fullPath);
                        }
                    }
                }

                // Extract Zip
                statusLabel.Content = "Extracting ZIP...";
                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Remove first folder name.
                        string filePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        int index = filePath.IndexOf(Path.DirectorySeparatorChar);

                        if(index >= 0)
                            filePath = filePath.Substring(index);

                        if (filePath.Length > 0)
                        {
                            filePath = App.InstallFolder + filePath;

                            if (!ZipEntryIsIsDirectory(entry))
                            {
                                var folderPath = Path.GetDirectoryName(filePath);

                                if (!Directory.Exists(folderPath))
                                   Directory.CreateDirectory(folderPath);

                                if (File.Exists(filePath))
                                    File.Delete(filePath);

                                entry.ExtractToFile(filePath);
                            }
                        }
                    }
                }

                statusLabel.Content = "Removing ZIP...";
                File.Delete(zipPath);

                // Reload Version Data
                App.ReloadXML();
                UpdateUIComponent();

                statusLabel.Content = "Complete";
                downloadUpdateBtn.IsEnabled = true;
            }
        }

        public bool ZipEntryIsIsDirectory(ZipArchiveEntry entry)
        {
            string name = entry.Name;
            int nameLength = name.Length;
            return nameLength <= 0 || (nameLength > 0 && ((name[nameLength - 1] == '/') || (name[nameLength - 1] == '\\')));
        }

        private void EditionRadio_Checked(object sender, RoutedEventArgs e)
        {
            RebuildVersionDropdown();
        }

        private void VersionDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem sel = ((sender as ComboBox).SelectedItem as ComboBoxItem);
            if (sel != null)
            {
                selectedVersionCombo = (sel.Tag as JSONGameVersion);
                Console.WriteLine(selectedVersionCombo.edition + ":" + selectedVersionCombo.version + "=" + Path.GetFileName(selectedVersionCombo.url));
            }
        }

        private void ChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            Utils.GetManualDir();
            App.ReloadXML();
            UpdateUIComponent();
            //RebuildVersionDropdown();
        }
    }
}
