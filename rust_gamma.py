"""
Rust Gamma Switcher for NVIDIA GeForce RTX
Автоматическое переключение гаммы день/ночь по горячей клавише
(Настроено только для игрового монитора AOC)
"""

import ctypes
from ctypes import wintypes
import time
import winsound
import threading
import sys
import atexit
import os

# ==================== НАСТРОЙКИ ====================

# Какой монитор переключать:
# "AOC"     - Автоматически находит игровой монитор AOC Q27G2G
# "PRIMARY" - Основной монитор Windows
# "ALL"     - Все мониторы
TARGET_MODE = "AOC"

# Горячая клавиша:
# 0x7A = F11, 0x79 = F10, 0x77 = F8, 0x6A = Numpad * (Звёздочка)
HOTKEY_VK = 0x7A  # F11
HOTKEY_NAME = "F11"

# Значение гаммы для ночи (1.0 - стандарт, 1.5 - повышенная)
NIGHT_GAMMA = 1.5

# Звуковой сигнал при нажатии (True / False)
PLAY_SOUND = True

# ==================================================

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
kernel32 = ctypes.windll.kernel32

gdi32.CreateDCW.restype = wintypes.HDC
gdi32.CreateDCW.argtypes = [wintypes.LPCWSTR, wintypes.LPCWSTR, wintypes.LPCWSTR, ctypes.c_void_p]
gdi32.DeleteDC.argtypes = [wintypes.HDC]

gdi32.GetDeviceGammaRamp.restype = wintypes.BOOL
gdi32.GetDeviceGammaRamp.argtypes = [wintypes.HDC, ctypes.c_void_p]
gdi32.SetDeviceGammaRamp.restype = wintypes.BOOL
gdi32.SetDeviceGammaRamp.argtypes = [wintypes.HDC, ctypes.c_void_p]

user32.RegisterHotKey.argtypes = [wintypes.HWND, ctypes.c_int, wintypes.UINT, wintypes.UINT]
user32.RegisterHotKey.restype = wintypes.BOOL
user32.UnregisterHotKey.argtypes = [wintypes.HWND, ctypes.c_int]
user32.UnregisterHotKey.restype = wintypes.BOOL

class DISPLAY_DEVICE(ctypes.Structure):
    _fields_ = [
        ('cb', wintypes.DWORD),
        ('DeviceName', wintypes.WCHAR * 32),
        ('DeviceString', wintypes.WCHAR * 128),
        ('StateFlags', wintypes.DWORD),
        ('DeviceID', wintypes.WCHAR * 128),
        ('DeviceKey', wintypes.WCHAR * 128)
    ]

class MSG(ctypes.Structure):
    _fields_ = [
        ('hwnd', wintypes.HWND),
        ('message', wintypes.UINT),
        ('wParam', wintypes.WPARAM),
        ('lParam', wintypes.LPARAM),
        ('time', wintypes.DWORD),
        ('pt', wintypes.POINT),
        ('lPrivate', wintypes.DWORD)
    ]

def detect_monitors():
    """Сканирует систему и находит игровой монитор AOC и остальные экраны."""
    dev = DISPLAY_DEVICE()
    dev.cb = ctypes.sizeof(dev)
    i = 0
    aoc_device = None
    primary_device = None
    all_displays = []

    while user32.EnumDisplayDevicesW(None, i, ctypes.byref(dev), 0):
        if dev.StateFlags & 1:  # DISPLAY_DEVICE_ATTACHED_TO_DESKTOP
            is_primary = bool(dev.StateFlags & 4)
            dev_name = dev.DeviceName
            all_displays.append(dev_name)
            if is_primary:
                primary_device = dev_name

            # Проверяем модель монитора на этом видеовыходе
            mon = DISPLAY_DEVICE()
            mon.cb = ctypes.sizeof(mon)
            j = 0
            while user32.EnumDisplayDevicesW(dev_name, j, ctypes.byref(mon), 0):
                dev_id_upper = mon.DeviceID.upper()
                dev_str_upper = mon.DeviceString.upper()
                if "AOC" in dev_id_upper or "AOC" in dev_str_upper or "2702" in dev_id_upper:
                    aoc_device = dev_name
                j += 1
        i += 1

    return {
        "aoc": aoc_device,
        "primary": primary_device,
        "all": all_displays
    }

def build_gamma_ramp(gamma_val):
    ramp = (ctypes.c_ushort * 768)()
    for i in range(256):
        val = int(min(65535, max(0, ((i / 255.0) ** (1.0 / gamma_val)) * 65535 + 0.5)))
        ramp[i] = val        # R
        ramp[i + 256] = val  # G
        ramp[i + 512] = val  # B
    return ramp

class GammaManager:
    def __init__(self, target_display, display_label, night_gamma=1.5):
        self.target_display = target_display
        self.display_label = display_label
        self.night_gamma = night_gamma
        self.is_boosted = False
        self.running = True
        self.original_ramp = (ctypes.c_ushort * 768)()
        self.has_original = False
        self.night_ramp = build_gamma_ramp(self.night_gamma)

        # Считываем и сохраняем исходный профиль только выбранного монитора
        hdc = gdi32.CreateDCW(None, self.target_display, None, None)
        if hdc:
            if gdi32.GetDeviceGammaRamp(hdc, ctypes.byref(self.original_ramp)):
                self.has_original = True
            gdi32.DeleteDC(hdc)

    def apply_ramp(self, boosted=False):
        hdc = gdi32.CreateDCW(None, self.target_display, None, None)
        if hdc:
            if boosted:
                gdi32.SetDeviceGammaRamp(hdc, ctypes.byref(self.night_ramp))
            else:
                if self.has_original:
                    gdi32.SetDeviceGammaRamp(hdc, ctypes.byref(self.original_ramp))
                else:
                    gdi32.SetDeviceGammaRamp(hdc, ctypes.byref(build_gamma_ramp(1.0)))
            gdi32.DeleteDC(hdc)

    def toggle(self):
        self.is_boosted = not self.is_boosted
        self.apply_ramp(self.is_boosted)
        if self.is_boosted:
            print(f"[{time.strftime('%H:%M:%S')}] >>> НОЧЬ ВКЛЮЧЕНА (Гамма {self.night_gamma}) на [{self.display_label}]")
            if PLAY_SOUND:
                threading.Thread(target=lambda: winsound.Beep(900, 100), daemon=True).start()
        else:
            print(f"[{time.strftime('%H:%M:%S')}] >>> ДЕНЬ (Гамма 1.0) на [{self.display_label}]")
            if PLAY_SOUND:
                threading.Thread(target=lambda: winsound.Beep(450, 140), daemon=True).start()

    def restore(self):
        self.apply_ramp(boosted=False)

    def enforce_background(self):
        """В полноэкранном режиме удерживает гамму включенной только на игровом мониторе."""
        while self.running:
            time.sleep(0.8)
            if self.is_boosted:
                self.apply_ramp(boosted=True)

def main():
    os.system("title Rust Gamma Switcher - AOC Edition")
    print("=" * 60)
    print("      RUST GAMMA SWITCHER (ТОЛЬКО ДЛЯ ИГРОВОГО МОНИТОРА)")
    print("=" * 60)

    devices = detect_monitors()
    
    # Определяем целевой дисплей
    target_display = None
    display_label = ""
    
    if TARGET_MODE == "AOC" and devices["aoc"]:
        target_display = devices["aoc"]
        display_label = "AOC Q27G2G (Игровой монитор)"
    elif TARGET_MODE == "PRIMARY" and devices["primary"]:
        target_display = devices["primary"]
        display_label = "Основной монитор"
    else:
        target_display = devices["primary"] or devices["all"][0]
        display_label = f"Монитор {target_display}"

    print(f"Целевой экран:       {display_label} [{target_display}]")
    print(f"Второй экран:        НЕ затрагивается (остаётся стандартным)")
    print(f"Горячая клавиша:     [{HOTKEY_NAME}]")
    print(f"Гамма ночью:         {NIGHT_GAMMA}")
    print("-" * 60)
    print("Высокий сигнал (пик) = НОЧЬ (Гамма 1.5)")
    print("Низкий сигнал  (бас) = ДЕНЬ (Гамма 1.0)")
    print("-" * 60)
    print("Скрипт активен. В игре Rust нажимай клавишу для переключения!")
    print("Для выхода нажми Ctrl + C или закрой это окно.\n")

    manager = GammaManager(target_display, display_label, night_gamma=NIGHT_GAMMA)
    atexit.register(manager.restore)

    HOTKEY_ID = 101
    MOD_NOREPEAT = 0x4000
    if not user32.RegisterHotKey(None, HOTKEY_ID, MOD_NOREPEAT, HOTKEY_VK):
        print(f"ОШИБКА: Не удалось зарегистрировать клавишу {HOTKEY_NAME}. Возможно, она уже занята другой программой.")
        input("Нажмите Enter для выхода...")
        return

    # Запускаем фоновый поток удержания только для выбранного монитора
    t = threading.Thread(target=manager.enforce_background, daemon=True)
    t.start()

    msg = MSG()
    try:
        while user32.GetMessageW(ctypes.byref(msg), None, 0, 0) != 0:
            if msg.message == 0x0312:  # WM_HOTKEY
                if msg.wParam == HOTKEY_ID:
                    manager.toggle()
            user32.TranslateMessage(ctypes.byref(msg))
            user32.DispatchMessageW(ctypes.byref(msg))
    except KeyboardInterrupt:
        pass
    finally:
        user32.UnregisterHotKey(None, HOTKEY_ID)
        manager.restore()
        print("\nГамма сброшена на стандартную. Завершение работы.")

if __name__ == '__main__':
    main()
