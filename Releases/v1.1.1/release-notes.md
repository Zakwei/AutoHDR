# AutoHDR v1.1.1

## Poprawki

- Aplikacja Xbox (`Microsoft.GamingApp`, proces `XboxPcApp.exe`), Game Bar i Gaming Services nie są już błędnie wykrywane jako gry.
- Uruchomienie samej aplikacji Xbox nie włącza już HDR — HDR uruchamia się dopiero po starcie właściwej gry.
- Przywrócono poprawny kierunek przełączania: gra startuje → HDR włącza się, gra kończy → HDR wraca do stanu sprzed gry.
- Zamknięcie aplikacji Xbox lub jej przejście w tło nie wpływa już na stan HDR.

## Pliki

- `AutoHDR-1.1.1.msi` — instalator Windows
- `AutoHDR-1.1.1-portable.zip` — wersja przenośna

## Wymagania

- Windows 10 lub Windows 11
- .NET Framework 4.8
