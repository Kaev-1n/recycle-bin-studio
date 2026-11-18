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
  <img src="docs/images/main-window.png" width="900" alt="Предпросмотр темы Pop Cat перед применением">

</p>

## Возможности

- Превью пустой и полной корзины, счётчик объектов и подсветка текущего состояния.
- Импорт ICO, PNG и JPG с сохранением пропорций и конвертацией в ICO от 16 до 256 пикселей.
- Восемь готовых тем с предпросмотром перед применением. Портрет и Pop Cat встроены, ещё шесть пар скачиваются при первом запуске и дальше доступны офлайн.
- **Своя тема из фото.** Загрузите одну картинку с двумя значками или два отдельных файла. Подсказки для генерации в ChatGPT доступны в редакторе. Сохранённые пары находятся в разделе «Темы → Мои».
- Возврат стандартных значков и обновления через приложение. Автообновление при запуске включается по желанию.

## Установка

Скачайте установщик для [x64](https://github.com/Kaev-1n/recycle-bin-studio/releases/latest/download/RecycleBinStudio-Setup-win-x64.exe) или [ARM64](https://github.com/Kaev-1n/recycle-bin-studio/releases/latest/download/RecycleBinStudio-Setup-win-arm64.exe). Мастер добавит ярлык в «Пуск». Ярлык на рабочем столе можно выбрать при установке. Права администратора и отдельная установка .NET не нужны.

## Сборка

C# · WPF · .NET 10

```powershell
dotnet run --project src/RecycleBinStudio
./installer/build.ps1 -Compiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Код под лицензией [MIT](LICENSE). Источник готовых тем — [sdushantha/recycle-bin-themes](https://github.com/sdushantha/recycle-bin-themes/tree/main/themes).
