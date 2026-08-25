using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoHDR
{
    public static class Locale
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _strings = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pl"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Language"] = "Jezyk",
                ["Polish"] = "Polski",
                ["English"] = "Angielski",
                ["RestartRequired"] = "Zmiana jezyka wymaga restartu aplikacji. Zrestartowac teraz?",
                ["Restart"] = "Restart",
                ["HDRStatus"] = "HDR: sprawdzanie...",
                ["HDRSupportedEnabled"] = "Stan HDR: wspierane={0}, wlaczone={1}",
                ["HDROn"] = "HDR: WLACZONY",
                ["HDROff"] = "HDR: WYLACZONY",
                ["HDRNotSupported"] = "Brak monitora HDR",
                ["HDRReadError"] = "Blad odczytu stanu HDR",
                ["HDREnableError"] = "Blad wlaczania HDR",
                ["HDRDisableError"] = "Blad wylaczania HDR",
                ["HDRRestoreError"] = "Blad przywracania HDR",
                ["ProcessReadError"] = "Blad pobierania procesow",
                ["MonitorError"] = "Blad monitorowania",
                ["GameDetected"] = "Wykryto gre: {0}. Wlaczam HDR...",
                ["GameEnabledCount"] = "Wlaczono HDR na {0} monitorach.",
                ["NoHDRToEnable"] = "Brak monitora HDR do wlaczenia lub HDR juz aktywne.",
                ["LastGameClosed"] = "Ostatnia gra zamknieta. Przywracam poprzedni stan HDR...",
                ["HDRStaysOn"] = "HDR pozostaje wlaczone (bylo wlaczone przed gra).",
                ["HDRRestoredCount"] = "Przywrocono stan na {0} monitorach.",
                ["ScanningGames"] = "Skanowanie zainstalowanych gier...",
                ["GamesDetected"] = "Wykryto {0} gier.",
                ["GamesCount"] = "Gier: {0}",
                ["GamesCountStatus"] = "Gier: {0}",
                ["GameNone"] = "Gra: BRAK",
                ["GameYes"] = "Gra: TAK",
                ["RefreshGameList"] = "Odswiez liste gier",
                ["OpenConfigFolder"] = "Otworz folder konfiguracji",
                ["EditGameList"] = "Edytuj liste gier",
                ["RunWithWindows"] = "Uruchamiaj z Windowsem",
                ["Exit"] = "Wyjdz",
                ["AutoStartEnabled"] = "Autostart wlaczony.",
                ["AutoStartDisabled"] = "Autostart wylaczony.",
                ["AutoStartReadError"] = "Blad odczytu autostartu",
                ["AutoStartSetError"] = "Blad ustawiania autostartu",
                ["SettingsOpenError"] = "Blad otwierania ustawien",
                ["ConfigFolderOpenError"] = "Blad otwierania folderu",
                ["ApplicationError"] = "Blad aplikacji",
                ["StartupError"] = "Blad uruchamiania AutoHDR",
                ["RefreshError"] = "Blad odswiezania",
                ["UnhandledException"] = "Nieobslugiwany wyjatek",
                ["GameListTitle"] = "AutoHDR - lista gier",
                ["ProcessNameNoExe"] = "Nazwa procesu (bez .exe):",
                ["AddFromFile"] = "Dodaj z pliku",
                ["Add"] = "Dodaj",
                ["Delete"] = "Usun",
                ["Save"] = "Zapisz",
                ["Cancel"] = "Anuluj",
                ["InvalidProcessName"] = "Podaj poprawna nazwe pliku .exe (bez rozszerzenia).",
                ["GameAlreadyOnList"] = "Ta gra jest juz na liscie.",
                ["SaveError"] = "Blad zapisywania",
                ["GameFileFilter"] = "Pliki wykonywalne (*.exe)|*.exe",
                ["SelectGameExe"] = "Wybierz plik .exe gry",
                ["GamesFileHeader1"] = "# AutoHDR - lista gier (jedna nazwa .exe na linie)",
                ["GamesFileHeader2"] = "# Mozesz dodawac wlasne wpisy ponizej, np. cyberpunk2077",
                ["GamesFileHeader3"] = "#",
            },
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Language"] = "Language",
                ["Polish"] = "Polish",
                ["English"] = "English",
                ["RestartRequired"] = "Language change requires an application restart. Restart now?",
                ["Restart"] = "Restart",
                ["HDRStatus"] = "HDR: checking...",
                ["HDRSupportedEnabled"] = "HDR state: supported={0}, enabled={1}",
                ["HDROn"] = "HDR: ON",
                ["HDROff"] = "HDR: OFF",
                ["HDRNotSupported"] = "No HDR monitor",
                ["HDRReadError"] = "Error reading HDR state",
                ["HDREnableError"] = "Error enabling HDR",
                ["HDRDisableError"] = "Error disabling HDR",
                ["HDRRestoreError"] = "Error restoring HDR state",
                ["ProcessReadError"] = "Error reading process list",
                ["MonitorError"] = "Monitoring error",
                ["GameDetected"] = "Game detected: {0}. Enabling HDR...",
                ["GameEnabledCount"] = "HDR enabled on {0} monitor(s).",
                ["NoHDRToEnable"] = "No HDR monitor to enable or HDR already active.",
                ["LastGameClosed"] = "Last game closed. Restoring previous HDR state...",
                ["HDRStaysOn"] = "HDR stays on (was enabled before game).",
                ["HDRRestoredCount"] = "State restored on {0} monitor(s).",
                ["ScanningGames"] = "Scanning installed games...",
                ["GamesDetected"] = "Detected {0} games.",
                ["GamesCount"] = "Games: {0}",
                ["GamesCountStatus"] = "Games: {0}",
                ["GameNone"] = "Game: NONE",
                ["GameYes"] = "Game: YES",
                ["RefreshGameList"] = "Refresh game list",
                ["OpenConfigFolder"] = "Open configuration folder",
                ["EditGameList"] = "Edit game list",
                ["RunWithWindows"] = "Run with Windows",
                ["Exit"] = "Exit",
                ["AutoStartEnabled"] = "Autostart enabled.",
                ["AutoStartDisabled"] = "Autostart disabled.",
                ["AutoStartReadError"] = "Error reading autostart",
                ["AutoStartSetError"] = "Error setting autostart",
                ["SettingsOpenError"] = "Error opening settings",
                ["ConfigFolderOpenError"] = "Error opening folder",
                ["ApplicationError"] = "Application error",
                ["StartupError"] = "Error launching AutoHDR",
                ["RefreshError"] = "Error refreshing",
                ["UnhandledException"] = "Unhandled exception",
                ["GameListTitle"] = "AutoHDR - game list",
                ["ProcessNameNoExe"] = "Process name (without .exe):",
                ["AddFromFile"] = "Add from file",
                ["Add"] = "Add",
                ["Delete"] = "Delete",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["InvalidProcessName"] = "Please enter a valid .exe file name (without extension).",
                ["GameAlreadyOnList"] = "This game is already on the list.",
                ["SaveError"] = "Error saving",
                ["GameFileFilter"] = "Executable files (*.exe)|*.exe",
                ["SelectGameExe"] = "Select game .exe file",
                ["GamesFileHeader1"] = "# AutoHDR - game list (one .exe name per line)",
                ["GamesFileHeader2"] = "# You can add custom entries below, e.g. cyberpunk2077",
                ["GamesFileHeader3"] = "#",
            }
        };

        private static string _culture = "pl";

        public static string Culture
        {
            get => _culture;
            set
            {
                if (_strings.ContainsKey(value))
                    _culture = value;
            }
        }

        public static void LoadFromFile(string appDir)
        {
            try
            {
                string path = Path.Combine(appDir, "locale.txt");
                if (File.Exists(path))
                {
                    string line = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(line) && _strings.ContainsKey(line))
                        _culture = line;
                }
            }
            catch { }
        }

        public static void SaveToFile(string appDir)
        {
            try
            {
                string path = Path.Combine(appDir, "locale.txt");
                File.WriteAllText(path, _culture);
            }
            catch { }
        }

        public static string Get(string key)
        {
            if (_strings.TryGetValue(_culture, out var dict) && dict.TryGetValue(key, out string value))
                return value;

            if (_strings.TryGetValue("pl", out var fallback) && fallback.TryGetValue(key, out string fallbackValue))
                return fallbackValue;

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public static string[] SupportedCultures => _strings.Keys.ToArray();
    }
}
