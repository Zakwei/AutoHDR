using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AutoHDR
{
    public static class GameDetector
    {
        private static readonly string[] ExcludeNameParts = new[]
        {
            "redist", "vcredist", "vcruntime", "msvcr", "msvcp", "unins", "uninstall",
            "setup", "install", "dxsetup", "dotnet", "directx", "commonredist", "_redist",
            "unitycrashhandler", "crashreport", "crashpad", "errorreport",
            "steam", "epicgameslauncher", "goggalaxy", "uplay", "origin", "eadesktop",
            "battle.net", "battlenet", "overwolf", "discord", "twitch",
            "launcher", "launch", "start", "config", "settings", "options",
            "update", "patcher", "patch", "repair", "support", "tool",
            "nvidia", "amd", "intel", "physx", "opengl", "vulkan",
            "sedpatcher", "sedlauncher", "ue4prereq", "ue3redist"
        };

        private static readonly string[] ExcludeExact = new[]
        {
            "steam.exe", "steamservice.exe", "steamwebhelper.exe", "gameoverlayui.exe",
            "epicgameslauncher.exe", "goggalaxy.exe", "uplay.exe", "uplaywebcore.exe",
            "origin.exe", "eadesktop.exe", "eac.exe", "eadesktop.exe",
            "battle.net.exe", "battlenet.exe", "overwolf.exe", "discord.exe",
            "twitch.exe", "nvidiashare.exe", "amdow.exe", "wallpaperengine.exe"
        };

        private static readonly string[] ExcludeDirs = new[]
        {
            "redist", "commonredist", "_commonredist", "directx", "dotnet",
            "vcredist", "unins", "install", "support", "tools", "sdk", "binaries" 
        };

        public static HashSet<string> DetectAll(string appDir)
        {
            var games = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Merge(games, DetectSteam());
            Merge(games, DetectEpic());
            Merge(games, DetectGog());
            Merge(games, DetectEa());
            Merge(games, DetectUbisoft());
            Merge(games, DetectBlizzard());
            Merge(games, DetectCommon());
            Merge(games, LoadKnownGames(appDir));
            Merge(games, LoadCustomGames(appDir));

            return games;
        }

        private static void Merge(HashSet<string> target, IEnumerable<string> source)
        {
            if (source == null) return;
            foreach (var item in source)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    target.Add(item);
            }
        }

        private static readonly Regex ValidGameName = new Regex(@"^[A-Za-z0-9_. -]+$", RegexOptions.Compiled);

        public static HashSet<string> LoadKnownGames(string appDir)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = Path.Combine(appDir, "known_games.txt");
                if (!File.Exists(path)) return set;
                foreach (var line in File.ReadAllLines(path))
                {
                    string name = SanitizeGameName(line);
                    if (!string.IsNullOrEmpty(name))
                        set.Add(name);
                }
            }
            catch { }
            return set;
        }

        public static HashSet<string> LoadCustomGames(string appDir)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = Path.Combine(appDir, "games.txt");
                if (!File.Exists(path)) return set;
                foreach (var line in File.ReadAllLines(path))
                {
                    string name = SanitizeGameName(line);
                    if (!string.IsNullOrEmpty(name))
                        set.Add(name);
                }
            }
            catch { }
            return set;
        }

        private static string SanitizeGameName(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            string trimmed = line.Trim();
            if (trimmed.StartsWith("#")) return null;
            string name = Path.GetFileNameWithoutExtension(trimmed).Trim();
            if (string.IsNullOrEmpty(name)) return null;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
            if (!ValidGameName.IsMatch(name)) return null;
            return name;
        }

        public static IEnumerable<string> DetectSteam()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string steamPath = null;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    steamPath = key?.GetValue("SteamPath") as string;
                }
            }
            catch { }

            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
                steamPath = @"C:\Program Files (x86)\Steam";

            if (!Directory.Exists(steamPath))
                return results;

            var libraryPaths = new List<string> { steamPath };

            string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                try
                {
                    string text = File.ReadAllText(vdf);
                    foreach (var p in ExtractVdfValues(text, "path"))
                    {
                        if (Directory.Exists(p))
                            libraryPaths.Add(p);
                    }
                }
                catch { }
            }

            foreach (var lib in libraryPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string appsDir = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(appsDir)) continue;

                try
                {
                    foreach (var manifest in Directory.EnumerateFiles(appsDir, "appmanifest_*.acf"))
                    {
                        try
                        {
                            string text = File.ReadAllText(manifest);
                            var installDirs = ExtractVdfValues(text, "installdir").ToList();
                            if (installDirs.Count == 0) continue;

                            string commonDir = Path.Combine(lib, "steamapps", "common");
                            if (!Directory.Exists(commonDir)) continue;

                            foreach (var dir in installDirs)
                            {
                                string gameDir = Path.Combine(commonDir, dir);
                                if (!Directory.Exists(gameDir)) continue;

                                foreach (var exe in FindExecutables(gameDir))
                                {
                                    if (IsGameExecutable(exe))
                                        results.Add(Path.GetFileNameWithoutExtension(exe));
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return results;
        }

        public static IEnumerable<string> DetectEpic()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string manifests = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
            if (!Directory.Exists(manifests)) return results;

            try
            {
                foreach (var item in Directory.EnumerateFiles(manifests, "*.item"))
                {
                    try
                    {
                        string text = File.ReadAllText(item);
                        string installLoc = ExtractJsonValue(text, "InstallLocation");
                        string launchExe = ExtractJsonValue(text, "LaunchExecutable");

                        if (!string.IsNullOrEmpty(installLoc) && Directory.Exists(installLoc))
                        {
                            if (!string.IsNullOrEmpty(launchExe))
                            {
                                string full = Path.Combine(installLoc, launchExe);
                                if (File.Exists(full) && IsGameExecutable(full))
                                    results.Add(Path.GetFileNameWithoutExtension(full));
                            }
                            else
                            {
                                foreach (var exe in FindExecutables(installLoc))
                                {
                                    if (IsGameExecutable(exe))
                                        results.Add(Path.GetFileNameWithoutExtension(exe));
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return results;
        }

        public static IEnumerable<string> DetectGog()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                {
                    if (baseKey != null)
                        ReadGogRegistry(baseKey, results);
                }
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games"))
                {
                    if (baseKey != null)
                        ReadGogRegistry(baseKey, results);
                }
            }
            catch { }

            string[] commonPaths = new[]
            {
                @"C:\GOG Games",
                @"C:\Program Files (x86)\GOG Galaxy\Games",
                @"C:\Program Files\GOG Galaxy\Games"
            };

            foreach (var path in commonPaths)
            {
                if (!Directory.Exists(path)) continue;
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        foreach (var exe in FindExecutables(dir))
                        {
                            if (IsGameExecutable(exe))
                                results.Add(Path.GetFileNameWithoutExtension(exe));
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        private static void ReadGogRegistry(RegistryKey baseKey, HashSet<string> results)
        {
            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                try
                {
                    using (var subKey = baseKey.OpenSubKey(subKeyName))
                    {
                        if (subKey == null) continue;
                        string path = subKey.GetValue("path") as string;
                        string exe = subKey.GetValue("exe") as string;

                        if (!string.IsNullOrEmpty(exe))
                        {
                            string full = Path.Combine(path ?? "", exe);
                            if (File.Exists(full) && IsGameExecutable(full))
                                results.Add(Path.GetFileNameWithoutExtension(full));
                        }
                        else if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        {
                            foreach (var f in FindExecutables(path))
                            {
                                if (IsGameExecutable(f))
                                    results.Add(Path.GetFileNameWithoutExtension(f));
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static IEnumerable<string> DetectEa()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EA Games"))
                {
                    if (baseKey != null)
                        ReadEaRegistry(baseKey, results);
                }
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\EA Games"))
                {
                    if (baseKey != null)
                        ReadEaRegistry(baseKey, results);
                }
            }
            catch { }

            string[] commonPaths = new[]
            {
                @"C:\Program Files\EA Games",
                @"C:\Program Files (x86)\EA Games",
                @"C:\Program Files\Electronic Arts",
                @"C:\Program Files (x86)\Electronic Arts"
            };

            foreach (var path in commonPaths)
            {
                if (!Directory.Exists(path)) continue;
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        foreach (var exe in FindExecutables(dir))
                        {
                            if (IsGameExecutable(exe))
                                results.Add(Path.GetFileNameWithoutExtension(exe));
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        private static void ReadEaRegistry(RegistryKey baseKey, HashSet<string> results)
        {
            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                try
                {
                    using (var subKey = baseKey.OpenSubKey(subKeyName))
                    {
                        if (subKey == null) continue;
                        string dir = subKey.GetValue("Install Dir") as string
                                  ?? subKey.GetValue("GDFBinary") as string
                                  ?? subKey.GetValue("InstallLocation") as string; // fallback

                        if (!string.IsNullOrEmpty(dir))
                        {
                            if (File.Exists(dir))
                                dir = Path.GetDirectoryName(dir);
                            if (Directory.Exists(dir))
                            {
                                foreach (var f in FindExecutables(dir))
                                {
                                    if (IsGameExecutable(f))
                                        results.Add(Path.GetFileNameWithoutExtension(f));
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static IEnumerable<string> DetectUbisoft()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ubisoft"))
                {
                    if (baseKey != null)
                        ReadUbisoftRegistry(baseKey, results);
                }
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft"))
                {
                    if (baseKey != null)
                        ReadUbisoftRegistry(baseKey, results);
                }
            }
            catch { }

            string[] commonPaths = new[]
            {
                @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
                @"C:\Program Files\Ubisoft\Ubisoft Game Launcher\games",
                @"C:\Ubisoft Game Launcher\games",
                @"C:\Program Files (x86)\Ubisoft",
                @"C:\Program Files\Ubisoft",
                @"C:\Ubisoft"
            };

            foreach (var path in commonPaths)
            {
                if (!Directory.Exists(path)) continue;
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        foreach (var exe in FindExecutables(dir))
                        {
                            if (IsGameExecutable(exe))
                                results.Add(Path.GetFileNameWithoutExtension(exe));
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        private static void ReadUbisoftRegistry(RegistryKey baseKey, HashSet<string> results)
        {
            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                try
                {
                    using (var subKey = baseKey.OpenSubKey(subKeyName))
                    {
                        if (subKey == null) continue;
                        foreach (var valName in subKey.GetValueNames())
                        {
                            string value = subKey.GetValue(valName) as string;
                            if (string.IsNullOrEmpty(value)) continue;
                            if (File.Exists(value))
                            {
                                string dir = Path.GetDirectoryName(value);
                                if (Directory.Exists(dir))
                                {
                                    foreach (var f in FindExecutables(dir))
                                    {
                                        if (IsGameExecutable(f))
                                            results.Add(Path.GetFileNameWithoutExtension(f));
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static IEnumerable<string> DetectBlizzard()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Blizzard Entertainment"))
                {
                    if (baseKey != null)
                        ReadBlizzardRegistry(baseKey, results);
                }
                using (var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Blizzard Entertainment"))
                {
                    if (baseKey != null)
                        ReadBlizzardRegistry(baseKey, results);
                }
            }
            catch { }

            string[] commonPaths = new[]
            {
                @"C:\Program Files (x86)\Battle.net",
                @"C:\Program Files\Battle.net",
                @"C:\Program Files (x86)\Overwatch",
                @"C:\Program Files\Overwatch",
                @"C:\Program Files (x86)\World of Warcraft",
                @"C:\Program Files\World of Warcraft",
                @"C:\Program Files (x86)\Diablo III",
                @"C:\Program Files\Diablo III",
                @"C:\Program Files (x86)\Diablo IV",
                @"C:\Program Files\Diablo IV",
                @"C:\Program Files (x86)\Hearthstone",
                @"C:\Program Files\Hearthstone",
                @"C:\Program Files (x86)\StarCraft II",
                @"C:\Program Files\StarCraft II",
                @"C:\Program Files (x86)\Heroes of the Storm",
                @"C:\Program Files\Heroes of the Storm",
                @"C:\Program Files (x86)\Call of Duty",
                @"C:\Program Files\Call of Duty"
            };

            foreach (var path in commonPaths)
            {
                if (!Directory.Exists(path)) continue;
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(path))
                    {
                        foreach (var exe in FindExecutables(dir))
                        {
                            if (IsGameExecutable(exe))
                                results.Add(Path.GetFileNameWithoutExtension(exe));
                        }
                    }
                    // also top-level exes
                    foreach (var exe in FindExecutables(path))
                    {
                        if (IsGameExecutable(exe))
                            results.Add(Path.GetFileNameWithoutExtension(exe));
                    }
                }
                catch { }
            }

            return results;
        }

        private static void ReadBlizzardRegistry(RegistryKey baseKey, HashSet<string> results)
        {
            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                try
                {
                    using (var subKey = baseKey.OpenSubKey(subKeyName))
                    {
                        if (subKey == null) continue;
                        foreach (var valName in subKey.GetValueNames())
                        {
                            string value = subKey.GetValue(valName) as string;
                            if (string.IsNullOrEmpty(value)) continue;
                            if (File.Exists(value))
                            {
                                string dir = Path.GetDirectoryName(value);
                                if (Directory.Exists(dir))
                                {
                                    foreach (var f in FindExecutables(dir))
                                    {
                                        if (IsGameExecutable(f))
                                            results.Add(Path.GetFileNameWithoutExtension(f));
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static IEnumerable<string> DetectCommon()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] roots = new[]
            {
                @"C:\Games", @"D:\Games", @"E:\Games", @"F:\Games", @"G:\Games",
                @"C:\Program Files\Epic Games", @"C:\Program Files (x86)\Epic Games",
                @"C:\XboxGames"
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(root))
                    {
                        foreach (var exe in FindExecutables(dir))
                        {
                            if (IsGameExecutable(exe))
                                results.Add(Path.GetFileNameWithoutExtension(exe));
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        private static List<string> FindExecutables(string root)
        {
            var result = new List<string>();
            if (!Directory.Exists(root)) return result;

            var queue = new Queue<(string dir, int depth)>();
            queue.Enqueue((root, 0));

            while (queue.Count > 0)
            {
                var (dir, depth) = queue.Dequeue();
                try
                {
                    result.AddRange(Directory.EnumerateFiles(dir, "*.exe"));
                }
                catch { }

                if (depth < 3)
                {
                    try
                    {
                        foreach (var sub in Directory.EnumerateDirectories(dir))
                        {
                            string name = Path.GetFileName(sub).ToLowerInvariant();
                            if (ExcludeDirs.Any(d => name == d || name.Contains(d))) continue;
                            queue.Enqueue((sub, depth + 1));
                        }
                    }
                    catch { }
                }
            }

            return result;
        }

        private static bool IsGameExecutable(string filePath)
        {
            string name = Path.GetFileName(filePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(name)) return false;

            if (ExcludeExact.Contains(name, StringComparer.OrdinalIgnoreCase))
                return false;

            if (ExcludeNameParts.Any(p => name.Contains(p)))
                return false;

            string dirName = Path.GetFileName(Path.GetDirectoryName(filePath)).ToLowerInvariant();
            if (ExcludeDirs.Any(d => dirName == d || dirName.Contains(d)))
                return false;

            return true;
        }

        private static IEnumerable<string> ExtractVdfValues(string text, string key)
        {
            string pattern = $"\"{Regex.Escape(key)}\"\\s+\"([^\"]+)\"";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            foreach (Match m in regex.Matches(text))
                yield return m.Groups[1].Value;
        }

        private static string ExtractJsonValue(string text, string key)
        {
            string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]+)\"";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var m = regex.Match(text);
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
