# AutoHDR

<p align="center">
  <img src="AutoHDR/Assets/logo.png" alt="AutoHDR logo" width="120">
</p>

<p align="center">
  Automatyczne włączanie HDR podczas gier w systemie Windows.
</p>

<p align="center">
  <a href="README.en.md">English version</a>
</p>

<p align="center">
  <img src="https://github.com/Zakwei/AutoHDR/actions/workflows/build.yml/badge.svg" alt="Build status">
  <img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License MIT">
</p>

![Okno ustawień AutoHDR (polski)](Screenshots/settings.png)

Interfejs jest dostępny po polsku i po angielsku. Możesz przełączyć język z menu w zasobniku.

![AutoHDR settings window (English)](Screenshots/settings-en.png)

AutoHDR działa w tle (ikona w zasobniku systemowym), wykrywa uruchamiane gry i automatycznie włącza HDR na kompatybilnych monitorach. Po zamknięciu ostatniej gry przywraca poprzedni stan HDR.

---

## Spis treści

- [Funkcje](#funkcje)
- [Wymagania](#wymagania)
- [Pobieranie](#pobieranie)
- [Instalacja](#instalacja)
- [Użycie](#użycie)
- [Argumenty wiersza poleceń](#argumenty-wiersza-poleceń)
- [Jak to działa?](#jak-to-działa)
- [Budowanie ze źródeł](#budowanie-ze-źródeł)
- [Znane ograniczenia](#znane-ograniczenia)
- [FAQ](#faq)
- [Wsparcie](#wsparcie)
- [Podziękowania](#podziękowania)
- [Licencja](#licencja)

---

## Funkcje

- **Automatyczne włączanie HDR** — gdy wykryje grę, włącza HDR na wszystkich wspierających monitorach.
- **Przywracanie stanu** — po zamknięciu gry wraca do ustawień sprzed rozgrywki.
- **Wykrywanie gier z launcherów** — skanuje biblioteki Steam, Epic Games, GOG, EA App, Ubisoft Connect, Battle.net, Microsoft Store oraz popularne katalogi.
- **Wbudowana lista 800+ gier** — `known_games.txt` zawiera nazwy procesów popularnych gier.
- **Ręczne dodawanie gier** — okno ustawień pozwala dodać własne `.exe` lub wpisać nazwę procesu.
- **Autostart z Windows** — opcjonalne uruchamianie aplikacji przy logowaniu.
- **Tryb przenośny** — wystarczy jeden plik `.exe` plus `known_games.txt` w tym samym folderze.
- **.NET Framework 4.8** — działa na Windows 10 i Windows 11 bez instalacji nowszego runtime'u.

---

## Wymagania

- Windows 10 lub Windows 11
- .NET Framework 4.8 (domyślnie zainstalowany w nowszych wersjach Windows 10/11)
- Monitor obsługujący HDR (do testowania przełączania)
- Uprawnienia użytkownika (aplikacja nie wymaga administratora)

---

## Pobieranie

Gotowe pliki znajdziesz w zakładce **[Releases](https://github.com/Zakwei/AutoHDR/releases/tag/v1.1.1)**.

| Plik | Opis |
|------|------|
| `AutoHDR-1.1.1.msi` | Instalator Windows (polecany) |
| `AutoHDR-1.1.1-portable.zip` | Wersja przenośna — rozpakuj i uruchom `AutoHDR.exe` |

---

## Instalacja

### Instalator MSI (polecany)

1. Pobierz `AutoHDR-1.1.1.msi`.
2. Kliknij dwukrotnie i postępuj zgodnie z kreatorem.
3. Po instalacji uruchom **AutoHDR** z Menu Start.

Instalacja odbywa się w profilu użytkownika (`%LocalAppData%\AutoHDR`), więc nie wymaga uprawnień administratora.

Deinstalacja: Menu Start → Ustawienia → Aplikacje → AutoHDR → Odinstaluj.

### Wersja przenośna

1. Pobierz `AutoHDR-1.0.0-portable.zip`.
2. Rozpakuj do dowolnego folderu.
3. Uruchom `AutoHDR.exe`.

---

## Użycie

Po uruchomieniu aplikacji pojawi się ikona w **zasobniku systemowym** (tray).

Kliknij ikonę prawym przyciskiem, aby:

- zobaczyć aktualny stan HDR,
- odświeżyć listę wykrytych gier,
- otworzyć folder konfiguracji,
- edytować listę gier,
- włączyć autostart z Windows,
- **przełączyć język (PL / EN)**,
- wyjść.

### Dodawanie własnej gry

1. Wybierz z menu **"Edytuj listę gier"**.
2. Kliknij **"Dodaj z pliku"** i wskaż plik `.exe` gry, lub wpisz nazwę procesu (bez `.exe`).
3. Kliknij **"Zapisz"**.

Możesz też ręcznie edytować plik `games.txt` w folderze aplikacji — jedna nazwa procesu na linię. Linie zaczynające się od `#` są ignorowane.

---

## Argumenty wiersza poleceń

| Argument | Działanie |
|----------|-----------|
| `AutoHDR.exe /install` lub `/autostart` | Dodaje aplikację do autostartu Windows |
| `AutoHDR.exe /uninstall` lub `/noautostart` | Usuwa aplikację z autostartu |
| `AutoHDR.exe /settings` | Otwiera okno edycji listy gier |

---

## Jak to działa?

1. Aplikacja skanuje zainstalowane gry z różnych źródeł i buduje zbiór nazw procesów do monitorowania.
2. Co 2 sekundy sprawdza uruchomione procesy.
3. Gdy wykryje proces pasujący do gry:
   - zapisuje aktualny stan HDR dla każdego monitora,
   - włącza HDR na monitorach, które go nie miały.
4. Gdy ostatni taki proces zostanie zamknięty:
   - przywraca każdy monitor do stanu sprzed gry.

Aplikacja używa Windows **DisplayConfig API** (`DisplayConfigSetDeviceInfo` / `DisplayConfigGetDeviceInfo`) do przełączania HDR. Na Windows 11 24H2+ (build 26100+) automatycznie wykorzystuje nowsze API `DISPLAYCONFIG_SET_HDR_STATE`.

---

## Budowanie ze źródeł

Wymagania:

- .NET SDK (do kompilacji, np. 6.0+)
- WiX Toolset v5 z rozszerzeniem `WixToolset.UI.wixext` (tylko do budowania instalatora)

```powershell
cd AutoHDR
dotnet build AutoHDR.csproj -c Release
```

Instalator:

```powershell
cd AutoHDR.Installer
.\build-installer.ps1
```

---

## Znane ograniczenia

- Gry z **Microsoft Store / Xbox Game Pass** są wykrywane na podstawie białej listy wydawców (`store_game_publishers.txt`). Jeśli Twojej gry brakuje, dopisz wydawcę do tego pliku i zrestartuj aplikację.
- Skanowanie katalogów może czasem znaleźć proces, który nie jest grą (fałszywy alarm). Można go usunąć w oknie ustawień.
- Włączenie/wyłączenie HDR może zająć 1–2 sekundy i może mignąć ekran.

---

## FAQ

### Czy AutoHDR działa z grami z Xbox Game Pass / Microsoft Store?

Tak, aplikacja wykrywa je automatycznie na podstawie białej listy wydawców w pliku `store_game_publishers.txt`. Jeśli danej gry nie ma na liście, dopisz wydawcę (pierwszy segment nazwy paczki, np. `king`) lub dodaj nazwę procesu ręcznie w oknie **"Edytuj listę gier"**.

### Czy potrzebuję monitora z HDR?

Tak, żeby zobaczyć efekt przełączania HDR. Aplikacja bez monitora HDR nadal będzie działać, ale w logu zobaczysz `wspierane=False`.

### Dlaczego aplikacja wykryła coś, co nie jest grą?

Skanowanie launcherów czasem znajduje pliki `.exe`, które nie są grami. Usuń je w ustawieniach lub w pliku `games.txt`.

### Czy AutoHDR jest bezpieczne?

Tak. Aplikacja nie wymaga uprawnień administratora, nie wysyła żadnych danych i zapisuje tylko pliki w swoim folderze (`AutoHDR.log`, `games.txt`).

### Czy działa na Windows 10?

Tak, wymagany jest .NET Framework 4.8, który jest obecny na współczesnych instalacjach Windows 10.

---

## Changelog

### v1.1.1 (2026-09-10)
- Poprawka: aplikacja Xbox (Microsoft.GamingApp / `XboxPcApp.exe`), Game Bar i Gaming Services nie są już traktowane jako gry — uruchomienie aplikacji Xbox nie włącza już HDR, a kierunek przełączania (gra startuje → HDR włącza się, gra kończy → HDR wraca) działa prawidłowo.

### v1.1.0 (2026-08-26)
- Automatyczne wykrywanie gier z **Microsoft Store / Xbox Game Pass** (`store_game_publishers.txt`).
- Poprawione obsługiwanie nazw procesów ze spacjami, nawiasami i innymi dozwolonymi znakami.
- Usunięto dymki powiadomień przy odświeżaniu listy gier.

### v1.0.0 (2026-08-25)
- Pierwsza publiczna wersja.

## Wsparcie

Masz problem? Sprawdź:

1. Plik logu `AutoHDR.log` w folderze aplikacji.
2. Sekcję [Znane ograniczenia](#znane-ograniczenia).
3. Jeśli to nie pomoże, otwórz **Issue** na GitHubie z opisem problemu i załącz log.

---

## Podziękowania

Inspiracją dla podejścia do sterowania HDR było narzędzie [AutoActions](https://github.com/Codectory/AutoActions) (GPL-3.0). Kod w tym repozytorium jest jednak napisany od podstaw i nie zawiera żadnych fragmentów AutoActions.

---

## Licencja

Zobacz plik [LICENSE](LICENSE).
