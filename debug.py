debug_log("Программа запущена")
debug_log(f"sys.frozen: {getattr(sys, 'frozen', False)}")
debug_log(f"sys._MEIPASS: {getattr(sys, '_MEIPASS', 'не задан')}")
debug_log(f"executable: {sys.executable}")

try:
    debug_log(f"ico.ico exists: {os.path.exists(resource_path('ico.ico'))}")
    debug_log(f"off.ico exists: {os.path.exists(resource_path('off.ico'))}")
    debug_log(f"sing-box.exe exists: {os.path.exists(resource_path('sing-box.exe'))}")
except Exception as e:
    debug_log(f"Ошибка проверки ресурсов: {e}")

check_and_clear_stale_proxy()

ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("blue")

APP_DATA_DIR = data_path()
DATA_DIR = data_path("data")
DB_FILE = data_path("data/servers.json")
SETTINGS_FILE = data_path("data/settings.json")
CONFIG_FILE = data_path("data/config.json")
ROUTES_DIR = data_path("list")
LOG_FILE = data_path("proxy.log")

BG_COLOR = "#09090B"
SIDEBAR_COLOR = "#18181B"
CARD_COLOR = "#18181B"
BORDER_COLOR = "#27272A"
ACCENT_COLOR = "#4F46E5"
ACCENT_HOVER = "#4338CA"
SUCCESS_COLOR = "#10B981"
CONNECTED_BORDER_COLOR = "#02402c"
DANGER_COLOR = "#EF4444"
DANGER_HOVER = "#DC2626"
TEXT_MAIN = "#F8FAFC"
TEXT_MUTED = "#A1A1AA"
ACTIVE_ITEM_COLOR = "#27272A"
DEFAULT_MARKER_COLOR = "#f5ae20"