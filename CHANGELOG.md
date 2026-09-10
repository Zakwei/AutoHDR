# Changelog

## [1.1.1] - 2026-09-10

### Fixed
- Aplikacja Xbox (`Microsoft.GamingApp`, proces `XboxPcApp.exe`) oraz usługi towarzyszące (Game Bar, Gaming Services) nie są już błędnie wykrywane jako gry. Wcześniej samo uruchomienie aplikacji Xbox włączało HDR, a uruchomienie właściwej gry je wyłączało, bo monitorowany był launcher, a nie gra.
- Dodano `Microsoft.Gaming*` do listy pakietów niebędących grami oraz "Xbox", "Game Bar", "Gaming Services" do filtra nazw wyświetlanych pakietów Microsoft.
- Dodano `xboxpcapp.exe`, `xboxpcappft.exe`, `xboxgamebarwidgets.exe`, `gamebar.exe`, `gamebarpresencewriter.exe`, `gamingservices.exe`, `gamingservicesnet.exe`, `gamingtcui.exe` do listy wykluczonych plików `.exe`.

## [1.1.0] - 2026-08-26

### Added
- Automatyczne wykrywanie gier z Microsoft Store / Xbox Game Pass.
- Nowy plik `store_game_publishers.txt` z białą listą wydawców gier ze Store.

### Fixed
- Nazwy procesów ze spacjami, nawiasami i innymi dozwolonymi znakami są teraz poprawnie obsługiwane podczas wprowadzania.
- Usunięto dymki powiadomień przy odświeżaniu listy gier.

## [1.0.0] - 2026-08-25

### Added
- Automatyczne włączanie HDR po uruchomieniu gry.
- Przywracanie stanu HDR po zamknięciu ostatniej gry.
- Wykrywanie gier z launcherów: Steam, Epic Games, GOG, EA App, Ubisoft Connect, Battle.net.
- Wbudowana lista 800+ znanych gier (`known_games.txt`).
- Okno edycji listy gier (dodawanie `.exe`, usuwanie pozycji).
- Autostart z Windows.
- Instalator MSI i wersja przenośna.
- Obsługa nowego API `DISPLAYCONFIG_SET_HDR_STATE` na Windows 11 24H2+.
