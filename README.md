<p align="center">
  <img src="docs/images/app-icon.png" width="82" height="82" alt="">
</p>

<h1 align="center">Recycle Bin Studio</h1>

<p align="center">Редактор иконок пустой и полной корзины Windows</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4" alt="Windows 10 и 11">
  <a href="https://github.com/Kaev-1n/recycle-bin-studio/releases/latest"><img src="https://img.shields.io/github/v/release/Kaev-1n/recycle-bin-studio?label=%D0%A1%D0%BA%D0%B0%D1%87%D0%B0%D1%82%D1%8C&amp;color=8EE0BD" alt="Скачать последний релиз"></a>
  <img src="https://img.shields.io/badge/%D0%98%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81-%D0%A0%D1%83%D1%81%D1%81%D0%BA%D0%B8%D0%B9-766CCA" alt="Русский интерфейс">
</p>

<p align="center">
  <img src="docs/images/main-window.png" width="900" alt="Главное окно Recycle Bin Studio с превью пустой и полной корзины Pop Cat">
  <br>
  <sub>Главный экран версии в разработке</sub>
</p>

## Как пользоваться

1. Скачайте [релиз для x64 или ARM64](https://github.com/Kaev-1n/recycle-bin-studio/releases/latest), распакуйте ZIP и запустите `RecycleBinStudio.exe`.
2. Перетащите ICO, PNG или JPG на нужную карточку. Кнопка «Загрузить Pop Cat» подставит готовую пару.
3. Проверьте превью и нажмите «Применить». Кнопка «Значки Windows» вернёт стандартные иконки.

Приложение обновляет счётчик корзины и показывает активное состояние. Импорт сохраняет пропорции и собирает ICO в размерах от 16 до 256 пикселей.

## Сборка

```powershell
dotnet run --project src/RecycleBinStudio
```

C# · WPF · .NET 10 · [MIT](LICENSE) · [Источник Pop Cat](https://github.com/sdushantha/recycle-bin-themes/tree/main/themes/pop-cat)
