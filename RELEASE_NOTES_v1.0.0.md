# Rust Gamma Pro v1.0.0

First public release of Rust Gamma Pro — a lightweight hardware monitor gamma switcher for Rust.

## Downloads / Файлы для скачивания

| File / Файл | Type / Тип | Description / Описание |
| :--- | :---: | :--- |
| **`RustGammaPro_Setup_v1.0.0.exe`** | **Setup (Installer)** | Recommended for most users. Installs to system, creates Desktop/Start menu shortcuts and uninstaller. |
| **`RustGammaPro_v1.0.0_Portable.zip`** | **Portable (ZIP)** | No installation needed. Extract anywhere and launch immediately. |

---

## What's New / Что нового в версии 1.0.0

### English
- **Instant Hardware LUT Gamma:** Switch monitor gamma in 1 ms using standard Windows GDI API (`SetDeviceGammaRamp`).
- **100% Easy Anti-Cheat Safe:** No process injection, no DirectX hooks, no game file modifications.
- **Smart Rust Watcher:** Automatically detects `RustClient.exe` and targets the specific monitor where Rust is running.
- **Fail-Safe Restoration:** Resets monitor gamma back to 1.00 when the game closes or when Windows shuts down.
- **Universal Hardware Support:** Fully compatible with NVIDIA GeForce, AMD Radeon, and Intel GPUs.
- **Dark Theme GUI:** Configurable hotkeys (default: `F11`), 2-second test preview, and gamma slider (1.10 - 2.20).
- **Audio Feedback:** Distinct high/low frequency beeps for night and day modes.
- **Bilingual Interface:** Toggle between Russian and English on the fly.
- **Auto-Update System:** Built-in update checking for future releases.

### Русский
- **Мгновенное аппаратное переключение гаммы:** Работа через Windows Hardware LUT (`SetDeviceGammaRamp`) за 1 миллисекунду.
- **100% Безопасность от EAC:** Без инъекций в память, без перехвата DirectX и без изменения файлов игры.
- **Умное отслеживание Rust:** Находит процесс `RustClient.exe` и активирует гамму только на мониторе с игрой.
- **Защита от сбоев:** Автоматический сброс гаммы на 1.00 при закрытии игры или выключении ПК.
- **Совместимость с любыми видеокартами:** Одинаково работает на NVIDIA GeForce, AMD Radeon и Intel.
- **Тёмный интерфейс настроек:** Выбор горячих клавиш (по умолчанию `F11`), тест на 2 секунды, ползунок гаммы (1.10 - 2.20).
- **Звуковая индикация:** Высокий/низкий тон при включении и выключении.
- **Двуязычный интерфейс:** Моментальное переключение RU / EN в шапке окна.
- **Встроенная система автообновлений:** Проверка выхода новых версий.

---

## Links
- Website: [helliong.space](https://helliong.space)
- GitHub: [github.com/helliong/rust-gamma](https://github.com/helliong/rust-gamma)
- Bug Reports: [github.com/helliong/rust-gamma/issues](https://github.com/helliong/rust-gamma/issues)
