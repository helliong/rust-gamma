# Rust Gamma Pro v1.1.0

Major update introducing RivaTuner-style display calibration controls, real-time LUT curve preview, and enhanced hardware gamma management for Rust.

## Downloads / Файлы для скачивания

| File / Файл | Type / Тип | Description / Описание |
| :--- | :---: | :--- |
| **`RustGammaPro_Setup_v1.1.0.exe`** | **Setup (Installer)** | Recommended for most users. Installs to system, creates Desktop/Start menu shortcuts and uninstaller. |
| **`RustGammaPro_v1.1.0_Portable.zip`** | **Portable (ZIP)** | No installation needed. Extract anywhere and launch immediately. |
| **`SHA256SUMS.txt`** | **Checksums** | SHA256 checksums for verifying download integrity. |

---

## SHA256 Checksums / Контрольные суммы

```text
c9018fb0fa0c61e0e3d225b2fd1a816aa4bf63de3e9cea1d7e31e88f4c57d445  RustGammaPro_Setup_v1.1.0.exe
32309acf0b8a0148aa9e5af7d29b081270aad746bcaee633a4c75724da9173f9  RustGammaPro_v1.1.0_Portable.zip
```

---

## What's New / Что нового в версии 1.1.0

### English
- **RivaTuner-Style Display Controls:** Full independent sliders for **Brightness** (-100 … +100), **Contrast** (-100 … +100), and **Gamma** (0.50 … 3.00x).
- **Real-Time Hardware LUT Graph:** Interactive curve preview box showing the mathematical display response curve live as sliders are moved.
- **Fail-Safe Day Gamma Reset:** Instant, unconditional restoration of mathematical 1.0 day gamma (R=G=B=1.0, 0 brightness, 0 contrast) on game exit, hotkey toggle, and profile reset.
- **Enhanced Profile Reset:** The reset button now immediately returns display hardware to day 1.0 in addition to resetting sliders.
- **Fixed Layout & Spacing:** Redesigned control blocks with dedicated rows for titles, values, and sliders, completely eliminating UI clipping at any DPI scale.
- **Universal Status Icons:** Replaced color emojis with cross-platform Unicode geometric indicators.
- **Automated Release Pipeline:** Added automated release compilation script (`build_release.ps1`).

### Русский
- **Управление цветом в стиле RivaTuner:** Независимые ползунки **Яркости** (-100 … +100), **Контраста** (-100 … +100) и **Гаммы** (0.50 … 3.00x).
- **График кривой Hardware LUT в реальном времени:** Интерактивное превью математической кривой отклика дисплея, обновляющееся на лету при движении ползунков.
- **Бескомпромиссный сброс в 1.0:** Гарантированное восстановление эталонной дневной гаммы 1.0 (R=G=B=1.0, яркость 0, контраст 0) при выходе из игры, нажатии хоткея и сбросе профиля.
- **Улучшенный сброс профиля:** Кнопка «↺ Сброс профиля» теперь не только возвращает ползунки в дефолт, но и сразу сбрасывает монитор на день (1.0).
- **Исправление интерфейса:** Полностью переработана компоновка блоков настроек — устранены любые наезды элементов друг на друга при любых масштабах Windows (DPI).
- **Универсальные индикаторы статуса:** Цветные эмодзи заменены на стандартные геометрические символы, которые корректно отображаются на всех версиях Windows.
- **Скрипт автоматической сборки:** Добавлен PowerShell-скрипт сборки релизных пакетов (`build_release.ps1`).

---

## Links / Ссылки
- Website: [helliong.space](https://helliong.space)
- GitHub: [github.com/helliong/rust-gamma](https://github.com/helliong/rust-gamma)
- Bug Reports: [github.com/helliong/rust-gamma/issues](https://github.com/helliong/rust-gamma/issues)
