using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BrowserAddOnView
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Tarayıcı Eklenti Çıkarma Aracı");
                Console.WriteLine("1. Chrome Eklentileri");
                Console.WriteLine("2. Edge Eklentileri");
                Console.WriteLine("3. Firefox Eklentileri");
                Console.WriteLine("4. Internet Explorer Eklentileri");
                Console.WriteLine("5. Opera Eklentileri");
                Console.WriteLine("6. Tüm Tarayıcı Eklentileri");
                Console.WriteLine("0. Çıkış");
                Console.Write("Seçiminiz (0-6): ");

                var choice = Console.ReadLine();

                if (choice == "0")
                {
                    Console.WriteLine("\nProgram sonlandırılıyor...");
                    break;
                }

                switch (choice)
                {
                    case "1":
                        ExtractChromeExtensions();
                        break;
                    case "2":
                        ExtractEdgeExtensions();
                        break;
                    case "3":
                        ExtractFirefoxExtensions();
                        break;
                    case "4":
                        ExtractIEExtensions();
                        break;
                    case "5":
                        ExtractOperaExtensions();
                        break;
                    case "6":
                        ExtractAllBrowserExtensions();
                        break;
                    default:
                        Console.WriteLine("\nGeçersiz seçim! Lütfen 0-6 arasında bir sayı girin.");
                        break;
                }

                Console.WriteLine("\nDevam etmek için bir tuşa basın...");
                Console.ReadKey();
            }
        }

        static void ExtractChromeExtensions()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string chromeUserDataPath = Path.Combine(localAppData, @"Google\Chrome\User Data");

                if (!Directory.Exists(chromeUserDataPath))
                {
                    Console.WriteLine("Chrome kullanıcı veri dizini bulunamadı!");
                    return;
                }

                Console.WriteLine("\nChrome Eklentileri Taraması Başladı...\n");
                var extensions = new List<BrowserExtension>();

                var profileDirs = Directory.GetDirectories(chromeUserDataPath)
                    .Where(dir => dir.EndsWith("Default") || dir.Contains("Profile"));

                foreach (var profileDir in profileDirs)
                {
                    string extensionsPath = Path.Combine(profileDir, "Extensions");
                    if (Directory.Exists(extensionsPath))
                    {
                        extensions.AddRange(ProcessChromiumBasedExtensions(extensionsPath, "Chrome"));
                    }
                }

                PrintExtensionsTable(extensions);
                SaveExtensionsToJson(extensions, "chrome_extensions.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        static void ExtractEdgeExtensions()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string edgeUserDataPath = Path.Combine(localAppData, @"Microsoft\Edge\User Data");

                if (!Directory.Exists(edgeUserDataPath))
                {
                    Console.WriteLine("Edge kullanıcı veri dizini bulunamadı!");
                    return;
                }

                Console.WriteLine("\nEdge Eklentileri Taraması Başladı...\n");
                var extensions = new List<BrowserExtension>();

                var profileDirs = Directory.GetDirectories(edgeUserDataPath)
                    .Where(dir => dir.EndsWith("Default") || dir.Contains("Profile"));

                foreach (var profileDir in profileDirs)
                {
                    string extensionsPath = Path.Combine(profileDir, "Extensions");
                    if (Directory.Exists(extensionsPath))
                    {
                        extensions.AddRange(ProcessChromiumBasedExtensions(extensionsPath, "Edge"));
                    }
                }

                PrintExtensionsTable(extensions);
                SaveExtensionsToJson(extensions, "edge_extensions.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        static void ExtractFirefoxExtensions()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string firefoxPath = Path.Combine(appData, @"Mozilla\Firefox\Profiles");

                if (!Directory.Exists(firefoxPath))
                {
                    Console.WriteLine("Firefox profili bulunamadı!");
                    return;
                }

                Console.WriteLine("\nFirefox Eklentileri Taraması Başladı...\n");
                var extensions = new List<BrowserExtension>();

                var profileDirs = Directory.GetDirectories(firefoxPath);
                foreach (var profileDir in profileDirs)
                {
                    var jsonFile = Path.Combine(profileDir, "addons.json");
                    if (File.Exists(jsonFile))
                    {
                        try
                        {
                            var addonsContent = File.ReadAllText(jsonFile);
                            var addonsData = JObject.Parse(addonsContent);

                            foreach (var addon in addonsData["addons"])
                            {
                                extensions.Add(new BrowserExtension
                                {
                                    Browser = "Firefox",
                                    Id = addon["id"]?.ToString(),
                                    Name = addon["name"]?.ToString(),
                                    Version = addon["version"]?.ToString(),
                                    Description = addon["description"]?.ToString(),
                                    Author = addon["creator"]?.ToString(),
                                    HomepageUrl = addon["homepageURL"]?.ToString(),
                                    Type = "WebExtension"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Firefox addons.json okuma hatası: {ex.Message}");
                        }
                    }
                }

                PrintExtensionsTable(extensions);
                SaveExtensionsToJson(extensions, "firefox_extensions.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        static void ExtractIEExtensions()
        {
            try
            {
                Console.WriteLine("\nInternet Explorer Eklentileri Taraması Başladı...\n");
                var extensions = new List<BrowserExtension>();

                using (var bhoKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects"))
                {
                    if (bhoKey != null)
                    {
                        foreach (string subKeyName in bhoKey.GetSubKeyNames())
                        {
                            var extension = GetIEExtensionInfo(subKeyName);
                            if (extension != null)
                            {
                                extensions.Add(extension);
                            }
                        }
                    }
                }

                PrintExtensionsTable(extensions);
                SaveExtensionsToJson(extensions, "ie_extensions.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        static void ExtractOperaExtensions()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string operaExtensionsPath = Path.Combine(localAppData, @"Opera Software\Opera Stable\Extensions");

                if (!Directory.Exists(operaExtensionsPath))
                {
                    Console.WriteLine("Opera eklenti dizini bulunamadı!");
                    return;
                }

                Console.WriteLine("\nOpera Eklentileri Taraması Başladı...\n");
                var extensions = new List<BrowserExtension>();
                var extensionDirs = Directory.GetDirectories(operaExtensionsPath);

                foreach (var extDir in extensionDirs)
                {
                    var manifestPath = Path.Combine(extDir, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        try
                        {
                            var manifestContent = File.ReadAllText(manifestPath);
                            var manifest = JObject.Parse(manifestContent);

                            extensions.Add(new BrowserExtension
                            {
                                Browser = "Opera",
                                Id = Path.GetFileName(extDir),
                                Name = manifest["name"]?.ToString(),
                                Version = manifest["version"]?.ToString(),
                                Description = manifest["description"]?.ToString(),
                                Author = manifest["author"]?.ToString(),
                                HomepageUrl = manifest["homepage_url"]?.ToString(),
                                Type = "Opera Extension"
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Manifest okuma hatası: {Path.GetFileName(extDir)} - {ex.Message}");
                        }
                    }
                }

                PrintExtensionsTable(extensions);
                SaveExtensionsToJson(extensions, "opera_extensions.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        static void ExtractAllBrowserExtensions()
        {
            try
            {
                Console.WriteLine("\nTüm Tarayıcı Eklentileri Taraması Başladı...\n");
                var allExtensions = new List<BrowserExtension>();

                // Chrome eklentilerini ekle
                string chromeUserDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data");
                if (Directory.Exists(chromeUserDataPath))
                {
                    var profileDirs = Directory.GetDirectories(chromeUserDataPath)
                        .Where(dir => dir.EndsWith("Default") || dir.Contains("Profile"));
                    foreach (var profileDir in profileDirs)
                    {
                        string extensionsPath = Path.Combine(profileDir, "Extensions");
                        if (Directory.Exists(extensionsPath))
                        {
                            allExtensions.AddRange(ProcessChromiumBasedExtensions(extensionsPath, "Chrome"));
                        }
                    }
                }

                // Edge eklentilerini ekle
                string edgeUserDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data");
                if (Directory.Exists(edgeUserDataPath))
                {
                    var profileDirs = Directory.GetDirectories(edgeUserDataPath)
                        .Where(dir => dir.EndsWith("Default") || dir.Contains("Profile"));
                    foreach (var profileDir in profileDirs)
                    {
                        string extensionsPath = Path.Combine(profileDir, "Extensions");
                        if (Directory.Exists(extensionsPath))
                        {
                            allExtensions.AddRange(ProcessChromiumBasedExtensions(extensionsPath, "Edge"));
                        }
                    }
                }

                // Firefox eklentilerini ekle
                string firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(firefoxPath))
                {
                    var profileDirs = Directory.GetDirectories(firefoxPath);
                    foreach (var profileDir in profileDirs)
                    {
                        var jsonFile = Path.Combine(profileDir, "addons.json");
                        if (File.Exists(jsonFile))
                        {
                            try
                            {
                                var addonsContent = File.ReadAllText(jsonFile);
                                var addonsData = JObject.Parse(addonsContent);

                                foreach (var addon in addonsData["addons"])
                                {
                                    allExtensions.Add(new BrowserExtension
                                    {
                                        Browser = "Firefox",
                                        Id = addon["id"]?.ToString(),
                                        Name = addon["name"]?.ToString(),
                                        Version = addon["version"]?.ToString(),
                                        Description = addon["description"]?.ToString(),
                                        Author = addon["creator"]?.ToString(),
                                        HomepageUrl = addon["homepageURL"]?.ToString(),
                                        Type = "WebExtension"
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Firefox addons.json okuma hatası: {ex.Message}");
                            }
                        }
                    }
                }

                // IE eklentilerini ekle
                using (var bhoKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects"))
                {
                    if (bhoKey != null)
                    {
                        foreach (string subKeyName in bhoKey.GetSubKeyNames())
                        {
                            var extension = GetIEExtensionInfo(subKeyName);
                            if (extension != null)
                            {
                                extension.Browser = "Internet Explorer";
                                extension.Type = "Browser Helper Objects";
                                allExtensions.Add(extension);
                            }
                        }
                    }
                }

                // Opera eklentilerini ekle
                string operaExtensionsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Opera Software\Opera Stable\Extensions");
                if (Directory.Exists(operaExtensionsPath))
                {
                    var extensionDirs = Directory.GetDirectories(operaExtensionsPath);
                    foreach (var extDir in extensionDirs)
                    {
                        var manifestPath = Path.Combine(extDir, "manifest.json");
                        if (File.Exists(manifestPath))
                        {
                            try
                            {
                                var manifestContent = File.ReadAllText(manifestPath);
                                var manifest = JObject.Parse(manifestContent);

                                allExtensions.Add(new BrowserExtension
                                {
                                    Browser = "Opera",
                                    Id = Path.GetFileName(extDir),
                                    Name = manifest["name"]?.ToString(),
                                    Version = manifest["version"]?.ToString(),
                                    Description = manifest["description"]?.ToString(),
                                    Author = manifest["author"]?.ToString(),
                                    HomepageUrl = manifest["homepage_url"]?.ToString(),
                                    Type = "Opera Extension"
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Manifest okuma hatası: {Path.GetFileName(extDir)} - {ex.Message}");
                            }
                        }
                    }
                }

                // Daha detaylı bilgileri ekle
                foreach (var ext in allExtensions)
                {
                    ext.Status = "Enabled";
                    ext.Title = ext.Name;
                    ext.Creator = ext.Author;
                    ext.InstallTime = null;
                    ext.UpdateTime = null;
                    ext.UpdateURL = null;
                    ext.SourceURL = null;
                    ext.ProfileFolder = null;

                    // IE eklentileri için ekstra bilgiler
                    if (ext.Browser == "Internet Explorer")
                    {
                        try
                        {
                            using (var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + ext.Id))
                            {
                                if (clsidKey != null)
                                {
                                    using (var inprocServerKey = clsidKey.OpenSubKey("InprocServer32"))
                                    {
                                        if (inprocServerKey != null)
                                        {
                                            ext.AddonFilename = inprocServerKey.GetValue(null)?.ToString();
                                            if (File.Exists(ext.AddonFilename))
                                            {
                                                var fileInfo = new FileInfo(ext.AddonFilename);
                                                ext.Size = $"{fileInfo.Length / 1024} KB";
                                                ext.AddonFileCreatedTime = fileInfo.CreationTime.ToString("dd.MM.yyyy HH:mm:ss");
                                                ext.AddonFileModifiedTime = fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                PrintAllExtensionsTable(allExtensions);
                SaveAllExtensionsToJson(allExtensions, "browser_extensions.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        static List<BrowserExtension> ProcessChromiumBasedExtensions(string extensionsPath, string browserName)
        {
            var extensions = new List<BrowserExtension>();
            var extensionDirs = Directory.GetDirectories(extensionsPath);

            foreach (var extDir in extensionDirs)
            {
                var versionDirs = Directory.GetDirectories(extDir);
                foreach (var versionDir in versionDirs)
                {
                    var manifestPath = Path.Combine(versionDir, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        try
                        {
                            var manifestContent = File.ReadAllText(manifestPath);
                            var manifest = JObject.Parse(manifestContent);

                            var extension = new BrowserExtension
                            {
                                Browser = browserName,
                                Id = Path.GetFileName(extDir),
                                Name = manifest["name"]?.ToString(),
                                Version = manifest["version"]?.ToString(),
                                Description = manifest["description"]?.ToString(),
                                Author = manifest["author"]?.ToString(),
                                HomepageUrl = manifest["homepage_url"]?.ToString(),
                                Type = "Extension"
                            };

                            extensions.Add(extension);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Manifest okuma hatası: {Path.GetFileName(extDir)} - {ex.Message}");
                        }
                    }
                }
            }

            return extensions;
        }

        static BrowserExtension GetIEExtensionInfo(string clsid)
        {
            try
            {
                using (var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + clsid))
                {
                    if (clsidKey != null)
                    {
                        return new BrowserExtension
                        {
                            Browser = "Internet Explorer",
                            Id = clsid,
                            Name = clsidKey.GetValue(null)?.ToString(),
                            Type = "Browser Helper Objects"
                        };
                    }
                }
            }
            catch { }
            return null;
        }

        static void PrintExtensionsTable(List<BrowserExtension> extensions)
        {
            Console.WriteLine("\n{0,-25} {1,-15} {2,-15} {3,-20} {4,-25} {5,-50} {6,-25}",
                "Item ID", "Status", "Web Browser", "Addon Type", "Name", "Description", "Version");

            Console.WriteLine(new string('-', 170));

            foreach (var ext in extensions)
            {
                Console.WriteLine("{0,-25} {1,-15} {2,-15} {3,-20} {4,-25} {5,-50} {6,-25}",
                    Truncate(ext.Id, 23),
                    "Enabled",
                    ext.Browser,
                    ext.Type,
                    Truncate(ext.Name ?? "-", 23),
                    Truncate(ext.Description ?? "-", 48),
                    ext.Version ?? "-");
            }

            Console.WriteLine($"\nToplam {extensions.Count} eklenti bulundu.");
        }

        static void PrintAllExtensionsTable(List<BrowserExtension> extensions)
        {
            Console.WriteLine("\n{0,-40} {1,-15} {2,-25} {3,-25} {4,-20} {5,-25} {6,-15}",
                "Item ID", "Status", "Web Browser", "Addon Type", "Name", "Version", "Size");

            Console.WriteLine(new string('-', 170));

            foreach (var ext in extensions)
            {
                Console.WriteLine("{0,-40} {1,-15} {2,-25} {3,-25} {4,-20} {5,-25} {6,-15}",
                    Truncate(ext.Id, 38),
                    ext.Status ?? "Enabled",
                    Truncate(ext.Browser, 23),
                    Truncate(ext.Type, 23),
                    Truncate(ext.Name ?? "-", 18),
                    Truncate(ext.Version ?? "-", 23),
                    Truncate(ext.Size ?? "-", 13));
            }

            Console.WriteLine($"\nToplam {extensions.Count} eklenti bulundu.");
        }

        static void SaveExtensionsToJson(List<BrowserExtension> extensions, string fileName)
        {
            string json = JsonConvert.SerializeObject(extensions, Formatting.Indented);
            File.WriteAllText(fileName, json);
            Console.WriteLine($"\nEklenti bilgileri {fileName} dosyasına kaydedildi.");
        }

        static void SaveAllExtensionsToJson(List<BrowserExtension> extensions, string fileName)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            var outputList = new List<object>();
            foreach (var ext in extensions)
            {
                outputList.Add(new
                {
                    ItemID = ext.Id,
                    Status = ext.Status ?? "Enabled",
                    WebBrowser = ext.Browser + (ext.Browser == "Internet Explorer" ? " (64-bit) - HKLM" : ""),
                    AddonType = ext.Type,
                    Name = ext.Name,
                    Version = ext.Version,
                    Description = ext.Description,
                    Title = ext.Title ?? ext.Name,
                    Creator = ext.Creator ?? ext.Author,
                    InstallTime = ext.InstallTime,
                    UpdateTime = ext.UpdateTime,
                    HomepageURL = ext.HomepageUrl,
                    UpdateURL = ext.UpdateURL,
                    SourceURL = ext.SourceURL,
                    AddonFilename = ext.AddonFilename,
                    AddonFileCreatedTime = ext.AddonFileCreatedTime,
                    AddonFileModifiedTime = ext.AddonFileModifiedTime,
                    Size = ext.Size,
                    ProfileFolder = ext.ProfileFolder
                });
            }

            string json = JsonConvert.SerializeObject(outputList, jsonSettings);
            File.WriteAllText(fileName, json);
            Console.WriteLine($"\nTüm eklenti bilgileri {fileName} dosyasına kaydedildi.");
        }

        static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }

    public class BrowserExtension
    {
        public string Browser { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string HomepageUrl { get; set; }
        public string[] Permissions { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Title { get; set; }
        public string Creator { get; set; }
        public string InstallTime { get; set; }
        public string UpdateTime { get; set; }
        public string UpdateURL { get; set; }
        public string SourceURL { get; set; }
        public string AddonFilename { get; set; }
        public string AddonFileCreatedTime { get; set; }
        public string AddonFileModifiedTime { get; set; }
        public string Size { get; set; }
        public string ProfileFolder { get; set; }
    }
}