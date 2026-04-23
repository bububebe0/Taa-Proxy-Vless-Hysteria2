internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main()
    {
        _mutex = new Mutex(true, "TaaClient_Singleton_Mutex_CSharp", out bool isNew);
        if (!isNew) { BringToFront(); return; }

        SystemProxy.ClearStale();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => { SystemProxy.Set(false); KillSwitch.Set(false); };

        var app = new TaaApp();
        app.Run(new MainWindow());
    }

    public static void ReleaseMutex()
    {
        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
        _mutex = null;
    }

    private static void BringToFront()
    {
        NativeInterop.EnumWindows((hWnd, _) =>
        {
            int len = NativeInterop.GetWindowTextLength(hWnd);
            if (len > 0)
            {
                var sb = new StringBuilder(len + 1);
                NativeInterop.GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().Contains("Taa", StringComparison.OrdinalIgnoreCase))
                {
                    NativeInterop.ShowWindow(hWnd, 9);
                    NativeInterop.SetForegroundWindow(hWnd);
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);
    }
}

internal class TaaApp : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyDarkTheme();
        DispatcherUnhandledException += (_, ex) =>
        {
            try { Paths.AppendLog(Paths.LogPath("error.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {ex.Exception}\n"); }
            catch { }
            ex.Handled = true;
        };
    }

    private static void ApplyDarkTheme()
    {
        const string xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <Style TargetType='CheckBox'>
    <Setter Property='Foreground' Value='#F8FAFC'/>
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='CheckBox'>
          <StackPanel Orientation='Horizontal' VerticalAlignment='Center'>
            <Border x:Name='box' Width='20' Height='20' Background='#09090B'
                    BorderBrush='#4F46E5' BorderThickness='2' CornerRadius='5' VerticalAlignment='Center'>
              <Path x:Name='chk' Visibility='Collapsed' Data='M 3,10 L 7.5,14.5 L 17,5'
                    Stroke='#F8FAFC' StrokeThickness='2.5' HorizontalAlignment='Center' VerticalAlignment='Center'
                    StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/>
            </Border>
            <ContentPresenter Margin='10,0,0,0' VerticalAlignment='Center'/>
          </StackPanel>
          <ControlTemplate.Triggers>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='chk' Property='Visibility' Value='Visible'/>
              <Setter TargetName='box' Property='Background' Value='#4F46E5'/>
              <Setter TargetName='box' Property='BorderBrush' Value='#4F46E5'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter TargetName='box' Property='Opacity' Value='0.4'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='ComboBoxItem'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Foreground' Value='#F8FAFC'/>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ComboBoxItem'>
          <Border x:Name='bd' Background='{TemplateBinding Background}' Padding='10,8,10,8'>
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsHighlighted' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='#27272A'/>
            </Trigger>
            <Trigger Property='IsSelected' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='#312E81'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='ComboBox'>
    <Setter Property='Background' Value='#18181B'/>
    <Setter Property='Foreground' Value='#F8FAFC'/>
    <Setter Property='BorderBrush' Value='#27272A'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='MaxDropDownHeight' Value='260'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ComboBox'>
          <Grid>
            <Border x:Name='bg' Background='{TemplateBinding Background}'
                    BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='{TemplateBinding BorderThickness}'
                    CornerRadius='6'/>
            <ContentPresenter x:Name='cp'
                              Content='{TemplateBinding SelectionBoxItem}'
                              ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                              Margin='10,0,28,0'
                              VerticalAlignment='Center'
                              HorizontalAlignment='Left'
                              IsHitTestVisible='False'/>
            <ToggleButton Background='Transparent' BorderThickness='0' Cursor='Hand'
                          IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
              <ToggleButton.Template>
                <ControlTemplate TargetType='ToggleButton'>
                  <Grid>
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition/>
                      <ColumnDefinition Width='28'/>
                    </Grid.ColumnDefinitions>
                    <Rectangle Grid.ColumnSpan='2' Fill='Transparent'/>
                    <Path Grid.Column='1' Data='M 0,0 L 4.5,5 L 9,0'
                          Stroke='#71717A' StrokeThickness='1.5'
                          VerticalAlignment='Center' HorizontalAlignment='Center'
                          StrokeStartLineCap='Round' StrokeEndLineCap='Round'/>
                  </Grid>
                </ControlTemplate>
              </ToggleButton.Template>
            </ToggleButton>
            <Popup x:Name='PART_Popup'
                   IsOpen='{TemplateBinding IsDropDownOpen}'
                   Placement='Bottom' AllowsTransparency='True' Focusable='False'>
              <Border Background='#1C1C1F' BorderBrush='#3F3F46' BorderThickness='1' CornerRadius='6'
                      MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}'
                      MaxHeight='{TemplateBinding MaxDropDownHeight}'>
                <ScrollViewer CanContentScroll='True' VerticalScrollBarVisibility='Auto'>
                  <ItemsPresenter/>
                </ScrollViewer>
              </Border>
            </Popup>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter TargetName='bg' Property='Opacity' Value='0.5'/>
            </Trigger>
            <Trigger Property='IsDropDownOpen' Value='True'>
              <Setter TargetName='bg' Property='BorderBrush' Value='#4F46E5'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='TabControl'>
    <Setter Property='Background' Value='#18181B'/>
    <Setter Property='BorderBrush' Value='#27272A'/>
    <Setter Property='Padding' Value='0'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TabControl'>
          <Grid>
            <Grid.RowDefinitions>
              <RowDefinition Height='Auto'/>
              <RowDefinition Height='*'/>
            </Grid.RowDefinitions>
            <Border Grid.Row='0' Background='#111113' BorderBrush='#27272A'
                    BorderThickness='1,1,1,0' CornerRadius='10,10,0,0'>
              <TabPanel x:Name='HeaderPanel' IsItemsHost='True' Background='Transparent'/>
            </Border>
            <Border Grid.Row='1' Background='#18181B' BorderBrush='#27272A'
                    BorderThickness='1,0,1,1' CornerRadius='0,0,10,10'>
              <ContentPresenter ContentSource='SelectedContent' Margin='0'/>
            </Border>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

<Style TargetType='TabItem'>
  <Setter Property='Foreground' Value='#71717A'/>
  <Setter Property='Background' Value='Transparent'/>
  <Setter Property='Padding' Value='20,12,20,12'/>
  <Setter Property='FontSize' Value='13'/>
  <Setter Property='Template'>
    <Setter.Value>
      <ControlTemplate TargetType='TabItem'>
        <Border x:Name='bd' Background='{TemplateBinding Background}'
                Padding='{TemplateBinding Padding}' Cursor='Hand'
                CornerRadius='10,10,0,0'> 
          <Grid>
            <TextBlock x:Name='txt' Text='{TemplateBinding Header}'
                       Foreground='{TemplateBinding Foreground}'
                       FontSize='{TemplateBinding FontSize}'
                       VerticalAlignment='Center' FontWeight='Normal'/>
            <Rectangle x:Name='indicator' Height='2' VerticalAlignment='Bottom'
                       Fill='Transparent' Margin='-4,0,-4,-1'/>
          </Grid>
        </Border>
        <ControlTemplate.Triggers>
          <Trigger Property='IsSelected' Value='True'>
            <Setter Property='Foreground' Value='#F8FAFC'/>
            <Setter TargetName='txt' Property='FontWeight' Value='SemiBold'/>
            <Setter TargetName='indicator' Property='Fill' Value='#4F46E5'/>
          </Trigger>
          <MultiTrigger>
            <MultiTrigger.Conditions>
              <Condition Property='IsMouseOver' Value='True'/>
              <Condition Property='IsSelected' Value='False'/>
            </MultiTrigger.Conditions>
            <Setter TargetName='bd' Property='Background' Value='#1A1A1E'/>
            <Setter Property='Foreground' Value='#A1A1AA'/>
          </MultiTrigger>
        </ControlTemplate.Triggers>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>

  <Style TargetType='TextBox'>
    <Setter Property='SelectionBrush' Value='#4F46E5'/>
    <Setter Property='SelectionOpacity' Value='0.55'/>
    <Setter Property='CaretBrush' Value='#F8FAFC'/>
  </Style>

  <Style TargetType='ScrollBar'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ScrollBar'>
          <Grid Background='Transparent'>
            <Track x:Name='PART_Track' IsDirectionReversed='True'>
              <Track.DecreaseRepeatButton>
                <RepeatButton Command='ScrollBar.PageUpCommand' Background='Transparent' BorderThickness='0' Opacity='0'/>
              </Track.DecreaseRepeatButton>
              <Track.IncreaseRepeatButton>
                <RepeatButton Command='ScrollBar.PageDownCommand' Background='Transparent' BorderThickness='0' Opacity='0'/>
              </Track.IncreaseRepeatButton>
              <Track.Thumb>
                <Thumb>
                  <Thumb.Template>
                    <ControlTemplate TargetType='Thumb'>
                      <Border x:Name='th' Background='#3F3F46' CornerRadius='3' Margin='3,2,3,2'/>
                      <ControlTemplate.Triggers>
                        <Trigger Property='IsMouseOver' Value='True'>
                          <Setter TargetName='th' Property='Background' Value='#52525B'/>
                        </Trigger>
                        <Trigger Property='IsDragging' Value='True'>
                          <Setter TargetName='th' Property='Background' Value='#6366F1'/>
                        </Trigger>
                      </ControlTemplate.Triggers>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
            </Track>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
    <Style.Triggers>
      <Trigger Property='Orientation' Value='Horizontal'>
        <Setter Property='Template'>
          <Setter.Value>
            <ControlTemplate TargetType='ScrollBar'>
              <Grid Background='Transparent'>
                <Track x:Name='PART_Track'>
                  <Track.DecreaseRepeatButton>
                    <RepeatButton Command='ScrollBar.PageLeftCommand' Background='Transparent' BorderThickness='0' Opacity='0'/>
                  </Track.DecreaseRepeatButton>
                  <Track.IncreaseRepeatButton>
                    <RepeatButton Command='ScrollBar.PageRightCommand' Background='Transparent' BorderThickness='0' Opacity='0'/>
                  </Track.IncreaseRepeatButton>
                  <Track.Thumb>
                    <Thumb>
                      <Thumb.Template>
                        <ControlTemplate TargetType='Thumb'>
                          <Border x:Name='th' Background='#3F3F46' CornerRadius='3' Margin='2,3,2,3'/>
                          <ControlTemplate.Triggers>
                            <Trigger Property='IsMouseOver' Value='True'>
                              <Setter TargetName='th' Property='Background' Value='#52525B'/>
                            </Trigger>
                          </ControlTemplate.Triggers>
                        </ControlTemplate>
                      </Thumb.Template>
                    </Thumb>
                  </Track.Thumb>
                </Track>
              </Grid>
            </ControlTemplate>
          </Setter.Value>
        </Setter>
      </Trigger>
    </Style.Triggers>
  </Style>

</ResourceDictionary>";
        try
        {
            var rd = (ResourceDictionary)XamlReader.Parse(xaml);
            Application.Current.Resources.MergedDictionaries.Add(rd);
        }
        catch (Exception ex)
        {
            try { Paths.AppendLog(Paths.LogPath("theme_error.log"), $"{DateTime.Now} - Theme error: {ex}\n"); } catch { }
        }
    }
}

internal static class NativeInterop
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc fn, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("wininet.dll")] public static extern bool InternetSetOptionW(IntPtr h, int opt, IntPtr buf, int len);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    public static void RefreshIE()
    {
        InternetSetOptionW(IntPtr.Zero, 37, IntPtr.Zero, 0);
        InternetSetOptionW(IntPtr.Zero, 39, IntPtr.Zero, 0);
    }
}

internal static class Paths
{
    public static string Base => AppDomain.CurrentDomain.BaseDirectory;
    public static string DataDir => Path.Combine(Base, "data");
    public static string ListDir => Path.Combine(Base, "list");
    public static string DbFile => Path.Combine(DataDir, "servers.json");
    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string ConfigFile => Path.Combine(DataDir, "config.json");
    public static string LogFile => Path.Combine(Base, "proxy.log");
    public static string Resource(string n) => Path.Combine(Base, n);
    public static string LogPath(string n) => Path.Combine(Base, n);

    private const long MaxLogBytes = 8 * 1024 * 1024;

    public static void AppendLog(string path, string text)
    {
        try
        {
            if (File.Exists(path) && new FileInfo(path).Length >= MaxLogBytes)
            {
                var old1 = path + ".old";
                var old2 = path + ".old2";
                if (File.Exists(old2)) File.Delete(old2);
                if (File.Exists(old1)) File.Move(old1, old2);
                File.Move(path, old1);
            }
            File.AppendAllText(path, text);
        }
        catch { }
    }
}

internal static class SystemProxy
{
    private const string Reg = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    public static void Set(bool enable, int port = 1080)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Reg, true)!;
            k.SetValue("ProxyEnable", enable ? 1 : 0, RegistryValueKind.DWord);
            if (enable) k.SetValue("ProxyServer", $"127.0.0.1:{port}", RegistryValueKind.String);
            NativeInterop.RefreshIE();
        }
        catch { }
    }

    public static void ClearStale()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Reg)!;
            if ((int)(k.GetValue("ProxyEnable") ?? 0) == 1)
                if (((k.GetValue("ProxyServer") as string) ?? "").StartsWith("127.0.0.1:"))
                    Set(false);
        }
        catch { }
    }

}

internal static class KillSwitch
{
    private const string Rule = "TaaProxy_KillSwitch";
    public static void Set(bool enable)
    {
        try
        {
            if (enable)
                foreach (var p in new[] { "TCP", "UDP" })
                    Run($"advfirewall firewall add rule name={Rule} dir=out action=block protocol={p} remoteaddress=any");
            else
                Run($"advfirewall firewall delete rule name={Rule}");
        }
        catch { }
    }
    private static void Run(string args)
    {
        using var p = new Process();
        p.StartInfo = new ProcessStartInfo("netsh", args) { CreateNoWindow = true, UseShellExecute = false };
        p.Start(); p.WaitForExit();
    }
}

internal static class Dpapi
{
    public static byte[] Encrypt(byte[] d) => ProtectedData.Protect(d, null, DataProtectionScope.CurrentUser);
    public static byte[] Decrypt(byte[] d) => ProtectedData.Unprotect(d, null, DataProtectionScope.CurrentUser);
}

internal static class FileAcl
{
    public static void SecureFile(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var domain = Environment.UserDomainName;
            var user   = Environment.UserName;
            var account = string.IsNullOrEmpty(domain) || domain == Environment.MachineName
                ? user
                : $"{domain}\\{user}";

            RunIcacls($"\"{path}\" /inheritance:r /grant:r \"{account}:F\"");
        }
        catch { }
    }

    private static void RunIcacls(string args)
    {
        using var p = new Process();
        p.StartInfo = new ProcessStartInfo("icacls", args)
        {
            CreateNoWindow  = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        p.Start();
        p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit(5000);
    }
}

internal static class Autostart
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool IsEnabled()
    {
        try { using var k = Registry.CurrentUser.OpenSubKey(Key)!; return (k.GetValue("TaaClient") as string) == ExePath(); }
        catch { return false; }
    }
    public static void Set(bool on)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Key, true)!;
            if (on) k.SetValue("TaaClient", ExePath(), RegistryValueKind.String);
            else k.DeleteValue("TaaClient", false);
        }
        catch { }
    }
    private static string ExePath() => Process.GetCurrentProcess().MainModule?.FileName ?? "";
}

internal class ServerModel
{
    public string Type { get; set; } = "vless";
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 443;
    public Dictionary<string, string> Params { get; set; } = new();
    public string Uuid { get; set; } = "";
    public string Password { get; set; } = "";
    public string Method { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}

internal class AppException
{
    public string ExType { get; set; } = "path";
    public string Value { get; set; } = "";
    public string Name { get; set; } = "";
}

internal class AppSettings
{
    public bool SplitTunneling { get; set; } = false;
    public string Language { get; set; } = "ru";
    public string DefaultServer { get; set; } = "";
    public string DnsType { get; set; } = "system";
    public string DnsServer { get; set; } = "https://1.1.1.1/dns-query";
    public bool DnsThroughProxy { get; set; } = true;
    public List<AppException> AppExceptions { get; set; } = new();
    public List<string> DomainExceptions { get; set; } = new();
    public bool MinimizeOnClose { get; set; } = true;
    public bool DebugMode { get; set; } = false;
    public bool KillSwitch { get; set; } = false;
    public bool AutoReconnect { get; set; } = true;
    public string CurrentRoutesFile { get; set; } = "routes.txt";
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 700;
    public bool UseTunMode { get; set; } = false;
    public string HotkeyToggle { get; set; } = "";
    public string HotkeyRouting { get; set; } = "";
    public string HotkeyTun     { get; set; } = "";
    public string HotkeyExit    { get; set; } = "";
    public bool EnableNotifications { get; set; } = false;
}

internal static class Tr
{
    private static string _lang = "ru";
    public static void SetLang(string l) => _lang = l is "ru" or "en" ? l : "ru";
    public static string Get(string k) => (_all.TryGetValue(_lang, out var d) && d.TryGetValue(k, out var v)) ? v : k;

    private static readonly Dictionary<string, Dictionary<string, string>> _all = new()
    {

internal static class HotkeyManager
{
    public const uint MOD_ALT      = 0x0001;
    public const uint MOD_CTRL     = 0x0002;
    public const uint MOD_SHIFT    = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;

    public static string Build(bool ctrl, bool alt, bool shift, Key key)
    {
        if (IsModOnly(key)) return "";
        if (!ctrl && !alt && !shift)
        {
            return KeyToStr(key);
        }
        var p = new List<string>();
        if (ctrl)  p.Add("Ctrl");
        if (alt)   p.Add("Alt");
        if (shift) p.Add("Shift");
        p.Add(KeyToStr(key));
        return string.Join("+", p);
    }

    public static bool TryParse(string s, out uint mods, out uint vk)
    {
        mods = 0; vk = 0;
        if (string.IsNullOrEmpty(s)) return false;
        var parts = s.Split('+');
        if (parts.Length == 1)
        {
            var ks = parts[0];
            if (ks.Length == 1 && char.IsDigit(ks[0])) ks = "D" + ks;
            try
            {
                var key = (Key)Enum.Parse(typeof(Key), ks, ignoreCase: true);
                vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                mods = MOD_NOREPEAT;
                return vk != 0;
            }
            catch { return false; }
        }
        else if (parts.Length >= 2)
        {
            foreach (var t in parts[..^1])
            {
                if (t == "Ctrl")       mods |= MOD_CTRL;
                else if (t == "Alt")   mods |= MOD_ALT;
                else if (t == "Shift") mods |= MOD_SHIFT;
            }
            mods |= MOD_NOREPEAT;
            var ks = parts[^1];
            if (ks.Length == 1 && char.IsDigit(ks[0])) ks = "D" + ks;
            try
            {
                var key = (Key)Enum.Parse(typeof(Key), ks, ignoreCase: true);
                vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                return vk != 0;
            }
            catch { return false; }
        }
        return false;
    }

    private static bool IsModOnly(Key k) => k is
        Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or
        Key.System or Key.None or Key.Tab or Key.CapsLock;

    private static string KeyToStr(Key k) => k switch
    {
        Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
        Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
        _ => k.ToString()
    };
}

internal static class LinkParser
{
    private static readonly string[] Schemes = { "vless://", "hysteria2://", "ss://", "trojan://" };

    public static ServerModel? Parse(string link)
    {
        if (!Schemes.Any(s => link.StartsWith(s, StringComparison.OrdinalIgnoreCase))) return null;
        try
        {
            var uri = new Uri(link);
            var q = ParseQuery(uri.Query);
            var frag = uri.Fragment.TrimStart('#');
            var name = string.IsNullOrEmpty(frag) ? uri.Host : Uri.UnescapeDataString(frag);
            var sv = new ServerModel
            {
                Type = uri.Scheme,
                Name = name,
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 443,
                Params = q
            };
            switch (uri.Scheme)
            {
                case "vless":
                    sv.Uuid = uri.UserInfo;
                    break;
                case "hysteria2":
                    sv.Password = Uri.UnescapeDataString(uri.UserInfo ?? "");
                    if (q.TryGetValue("security", out var sec) && sec.Equals("reality", StringComparison.OrdinalIgnoreCase))
                    {
                        sv.Type = "vless";
                        sv.Uuid = sv.Password;
                        sv.Password = "";
                    }
                    break;
                case "ss":
                    var ui = uri.UserInfo ?? "";
                    if (ui.Contains(':'))
                    {
                        var idx = ui.IndexOf(':');
                        sv.Method = Uri.UnescapeDataString(ui[..idx]);
                        sv.Password = Uri.UnescapeDataString(ui[(idx + 1)..]);
                    }
                    else
                    {
                        try
                        {
                            var dec = Encoding.UTF8.GetString(Convert.FromBase64String(
                                ui.PadRight(ui.Length + (4 - ui.Length % 4) % 4, '=')));
                            if (dec.Contains(':'))
                            { sv.Method = dec[..dec.IndexOf(':')]; sv.Password = dec[(dec.IndexOf(':') + 1)..]; }
                        }
                        catch { sv.Method = ui; }
                    }
                    if (sv.Method.StartsWith("2022-", StringComparison.OrdinalIgnoreCase)) return null;
                    break;
                case "trojan":
                    sv.Password = Uri.UnescapeDataString(uri.UserInfo ?? "");
                    if (string.IsNullOrEmpty(frag))
                        sv.Name = $"Trojan {uri.Host}:{sv.Port}";
                    break;
            }
            return sv;
        }
        catch { return null; }
    }

    public static List<ServerModel> ExtractAll(string text)
    {
        var pat = @"(vless://[^\s]+|hysteria2://[^\s]+|ss://[^\s]+|trojan://[^\s]+)";
        return Regex.Matches(text, pat).Select(m => Parse(m.Value)).OfType<ServerModel>().ToList();
    }

    private static Dictionary<string, string> ParseQuery(string q)
    {
        var d = new Dictionary<string, string>();
        foreach (var part in q.TrimStart('?').Split('&'))
        {
            var i = part.IndexOf('=');
            if (i < 0) continue;
            d[Uri.UnescapeDataString(part[..i])] = Uri.UnescapeDataString(part[(i + 1)..]);
        }
        return d;
    }
}

internal static class SingBoxConfig
{
    public static void Generate(ServerModel sv, AppSettings settings,
        string routesContent, int port, string logFile, string configPath, out string? tunInterfaceName)
    {
        tunInterfaceName = null;
        var p = sv.Params;
        var rules = new List<object>();

        foreach (var app in settings.AppExceptions)
        {
            var r = new Dictionary<string, object> { ["outbound"] = "direct" };
            if (app.ExType == "path") r["process_path"] = app.Value;
            else r["process_name"] = app.Value;
            rules.Add(r);
        }

        if (settings.DomainExceptions.Count > 0)
        {
            var suf = settings.DomainExceptions.ToList();
            rules.Add(new Dictionary<string, object> { ["outbound"] = "direct", ["domain_suffix"] = suf });
        }

        var isSplit = settings.SplitTunneling;
        var finalOut = isSplit ? "direct" : "proxy";
        if (isSplit && !string.IsNullOrWhiteSpace(routesContent))
        {
            var domains = new List<string>();
            var ips = new List<string>();
            foreach (var raw in routesContent.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                if (IsIpOrCidr(raw)) ips.Add(raw.Contains('/') ? raw : raw + "/32");
                else domains.Add(raw);
            }
            var rule = new Dictionary<string, object> { ["outbound"] = "proxy" };
            if (domains.Count > 0) rule["domain_suffix"] = domains;
            if (ips.Count > 0) rule["ip_cidr"] = ips;
            if (rule.Count > 1) rules.Add(rule);
        }

        object[] inbounds;
        if (settings.UseTunMode)
        {
            tunInterfaceName = "taa-tun0";
            inbounds = new object[]
            {
                new
                {
                    type = "tun",
                    interface_name = tunInterfaceName,
                    address = new[] { "172.19.0.1/30" },
                    route_address = new[] { "0.0.0.0/1", "128.0.0.0/1" },
                    auto_route = true,
                    strict_route = false,
                    exclude_interface = new[] { "Loopback Pseudo-Interface 1" }
                }
            };
        }
        else
        {
            inbounds = new object[]
            {
                new
                {
                    type = "mixed",
                    listen = "127.0.0.1",
                    listen_port = port,
                    sniff = true,
                    sniff_override_destination = true,
                    domain_strategy = "prefer_ipv4"
                }
            };
        }

        var config = new Dictionary<string, object>
        {
            ["log"]      = new { level = settings.DebugMode ? "debug" : "info", output = logFile },
            ["inbounds"] = inbounds,
            ["route"]    = new { rules, final = finalOut, auto_detect_interface = true }
        };

        config["outbounds"] = new object[] { BuildOutbound(sv, p), new { type = "direct", tag = "direct" } };

        var dns = BuildDns(settings);
        if (dns != null) config["dns"] = dns;

        Directory.CreateDirectory(Paths.DataDir);
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        FileAcl.SecureFile(configPath);
    }

    private static string ResolveSni(Dictionary<string, string> p, string fallback)
    {
        var sni = p.GetValueOrDefault("sni", "");
        if (string.IsNullOrEmpty(sni) || sni == "undefined" || sni == "null")
            return fallback;
        return sni;
    }

    private static object BuildOutbound(ServerModel sv, Dictionary<string, string> p)
    {
        return sv.Type switch
        {
            "hysteria2" => (object)new
            {
                type = "hysteria2",
                tag = "proxy",
                server = sv.Host,
                server_port = sv.Port,
                password = sv.Password,
                tls = new
                {
                    enabled = true,
                    server_name = ResolveSni(p, sv.Host),
                    insecure = p.GetValueOrDefault("insecure", "0") == "1"
                }
            },
            "ss" => (object)new
            {
                type = "shadowsocks",
                tag = "proxy",
                server = sv.Host,
                server_port = sv.Port,
                method = sv.Method,
                password = sv.Password
            },
            "trojan" => BuildTrojan(sv, p),
            _ => BuildVless(sv, p)
        };
    }

    private static object BuildVless(ServerModel sv, Dictionary<string, string> p)
    {
        string serverName = ResolveSni(p, sv.Host);
        bool isReality = p.GetValueOrDefault("security", "") == "reality";
        if (isReality && System.Net.IPAddress.TryParse(serverName, out _))
        {
            serverName = "www.microsoft.com";
        }

        var tls = new Dictionary<string, object>
        {
            ["enabled"] = true,
            ["server_name"] = serverName,
            ["utls"] = new { enabled = true, fingerprint = p.GetValueOrDefault("fp", "chrome") }
        };

        if (isReality)
            tls["reality"] = new
            {
                enabled = true,
                public_key = p.GetValueOrDefault("pbk", ""),
                short_id = p.GetValueOrDefault("sid", "")
            };

        var outbound = new Dictionary<string, object>
        {
            ["type"] = "vless",
            ["tag"] = "proxy",
            ["server"] = sv.Host,
            ["server_port"] = sv.Port,
            ["uuid"] = sv.Uuid,
            ["packet_encoding"] = "xudp",
            ["tls"] = tls
        };

        var flow = p.GetValueOrDefault("flow", "");
        if (string.IsNullOrEmpty(flow) && isReality)
        {
            flow = "xtls-rprx-vision";
        }
        if (!string.IsNullOrEmpty(flow)) outbound["flow"] = flow;

        var transport = BuildTransport(p);
        if (transport != null) outbound["transport"] = transport;

        return outbound;
    }

    private static object? BuildTransport(Dictionary<string, string> p)
    {
        var netType = p.GetValueOrDefault("type", "tcp");
        return netType switch
        {
            "ws" => (object)new Dictionary<string, object>
            {
                ["type"] = "ws",
                ["path"] = p.GetValueOrDefault("path", "/"),
                ["headers"] = new Dictionary<string, string>
                {
                    ["Host"] = p.GetValueOrDefault("host", p.GetValueOrDefault("sni", ""))
                }
            },
            "grpc" => (object)new Dictionary<string, object>
            {
                ["type"] = "grpc",
                ["service_name"] = p.GetValueOrDefault("serviceName", p.GetValueOrDefault("authority", ""))
            },
            "h2" => (object)new Dictionary<string, object>
            {
                ["type"] = "http",
                ["host"] = new[] { p.GetValueOrDefault("host", p.GetValueOrDefault("sni", "")) },
                ["path"] = p.GetValueOrDefault("path", "/")
            },
            "httpupgrade" => (object)new Dictionary<string, object>
            {
                ["type"] = "httpupgrade",
                ["path"] = p.GetValueOrDefault("path", "/"),
                ["host"] = p.GetValueOrDefault("host", p.GetValueOrDefault("sni", ""))
            },
            _ => null
        };
    }

    private static object BuildTrojan(ServerModel sv, Dictionary<string, string> p)
    {
        string serverName = ResolveSni(p, sv.Host);
        bool isReality = p.GetValueOrDefault("security", "") == "reality";
        bool sniIsIp = System.Net.IPAddress.TryParse(serverName, out _);

        var tls = new Dictionary<string, object>
        {
            ["enabled"] = true,
            ["server_name"] = serverName,
            ["utls"] = new { enabled = true, fingerprint = p.GetValueOrDefault("fp", "chrome") }
        };

        if (isReality && sniIsIp)
            tls["insecure"] = true;

        if (isReality)
            tls["reality"] = new
            {
                enabled = true,
                public_key = p.GetValueOrDefault("pbk", ""),
                short_id = p.GetValueOrDefault("sid", "")
            };

        var outbound = new Dictionary<string, object>
        {
            ["type"] = "trojan",
            ["tag"] = "proxy",
            ["server"] = sv.Host,
            ["server_port"] = sv.Port,
            ["password"] = sv.Password,
            ["tls"] = tls
        };

        var transport = BuildTransport(p);
        if (transport != null) outbound["transport"] = transport;

        return outbound;
    }

    private static object? BuildDns(AppSettings s)
    {
        if (s.DnsType == "system") return null;
        var addr = s.DnsServer;
        if (s.DnsType == "doh" && !addr.StartsWith("https://")) addr = "https://" + addr;
        if (s.DnsType == "dot" && !addr.StartsWith("tls://")) addr = "tls://" + addr;
        var srv = s.DnsThroughProxy
            ? (object)new { tag = "custom_dns", address = addr, detour = "proxy" }
            : (object)new { tag = "custom_dns", address = addr };
        return new { servers = new[] { srv }, rules = new[] { new { outbound = "any", server = "custom_dns" } } };
    }

    private static bool IsIpOrCidr(string s)
    {
        if (s.Contains('/')) s = s[..s.IndexOf('/')];
        return System.Net.IPAddress.TryParse(s, out _);
    }
}

internal class MainWindow : Window
{
    private static readonly Color C_BG = ParseColor("#0C0C0D");
    private static readonly Color C_SIDEBAR = ParseColor("#131315");
    private static readonly Color C_CARD = ParseColor("#1A1A1D");
    private static readonly Color C_BORDER = ParseColor("#2A2A30");
    private static readonly Color C_ACCENT = ParseColor("#6366F1");
    private static readonly Color C_HOVER = ParseColor("#4F46E5");
    private static readonly Color C_SUCCESS = ParseColor("#10B981");
    private static readonly Color C_CON_BRD = ParseColor("#065F46");
    private static readonly Color C_DANGER = ParseColor("#EF4444");
    private static readonly Color C_DANG_H = ParseColor("#DC2626");
    private static readonly Color C_TXT = ParseColor("#FFFFFF");
    private static readonly Color C_MUTED = ParseColor("#9CA3AF");
    private static readonly Color C_ACTIVE = ParseColor("#2A2A30");

    private static SolidColorBrush Br(Color c) => new(c);
    private static Color ParseColor(string h) => (Color)ColorConverter.ConvertFromString(h);

    private List<ServerModel> _servers = new();
    private AppSettings _settings = new();
    private Process? _proxyProcess;
    private int _selectedIdx = -1;
    private int _connectedIdx = -1;
    private int _proxyPort;
    private bool _hideIp = true;
    private bool _noNetwork = false;
    private int _autoReconnectAttempts = 0;
    private CancellationTokenSource _monitorCts = new();
    private CancellationTokenSource _reconnectCts = new();

    private StackPanel _serverListPanel = null!;
    private TextBlock _nameLabel = null!;
    private TextBlock _hostLabel = null!;
    private TextBlock _statusLabel = null!;
    private Button _connectBtn = null!;
    private Button _pingBtn = null!;
    private Button _defaultBtn = null!;
    private Button _deleteBtn = null!;
    private TextBlock _pingLabel = null!;
    private Border _serverInfoCard = null!;
    private ComboBox _routesCombo = null!;
    private ToggleButton _splitSwitch = null!;
    private StackPanel _serversPanel = null!;
    private ScrollViewer _serversScrollViewer = null!;
    private Grid _settingsPanel = null!;
    private Grid _importPanel = null!;
    private Grid _proxyListPanel = null!;
    private string _currentPanel = "servers";
    private string _currentRoutesFile = "routes.txt";

    private ToggleButton? _autostartChk, _minimizeChk, _debugChk, _killSwitchChk, _autoReconnectChk, _tunModeChk, _notificationsChk;
    private ComboBox? _langCombo;

    private ComboBox? _dnsTypeCombo;
    private TextBox? _dnsAddrBox;
    private ToggleButton? _dnsProxyChk;
    private TextBlock? _dnsTestLabel;

    private StackPanel? _appExcList, _domExcList;
    private int _selAppExcIdx = -1;
    private Button? _removeAppBtn, _removeDomBtn;
    private string? _selectedDomainException = null;

    private ComboBox? _proxyRoutesCombo;
    private TextBox? _proxyRoutingText;
    private Button? _proxySaveBtn;
    private Button? _proxyNewBtn;
    private Button? _proxyDeleteBtn;
    private Button? _proxyRenameBtn;

    private DispatcherTimer? _dotTimer;
    private int _dotPhase = 0;
    private Color _statusColor = ParseColor("#9CA3AF");

    private DispatcherTimer? _boundsTimer;

    private Forms.NotifyIcon? _tray;

    private Button _titleMaxBtn = null!;
    private Ellipse? _titleStatusDot;

    private static readonly IdnMapping _idn = new IdnMapping();

    private bool _editingName = false;
    private TextBox? _editNameBox = null;

    private HwndSource?  _hwndSource;
    private const int    WM_HOTKEY      = 0x0312;
    private const int    HK_TOGGLE      = 1001;
    private const int    HK_ROUTING     = 1002;
    private const int    HK_TUN         = 1003;
    private const int    HK_EXIT        = 1004;
    private Button?      _hkListeningBtn;
    private string?      _hkListeningProp;
    private readonly Dictionary<string, Button> _hkButtons = new();
    private bool _dataLoaded = false;

    private ToggleButton? _exportServersChk;
    private ToggleButton? _exportRoutesChk;

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void RestartAsAdmin()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exe)) return;

        SaveSettings();
        StopProxy();
        UnregisterAllHotkeys();
        _hwndSource?.RemoveHook(HwndHook);
        Program.ReleaseMutex();

        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            Verb = "runas"
        };
        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _hwndSource?.AddHook(HwndHook);
            RegisterAllHotkeys();
            MessageBox.Show($"Не удалось перезапустить с правами администратора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            Application.Current.Shutdown();
        }), DispatcherPriority.Background);
    }

    public MainWindow()
    {
        _proxyPort = new Random().Next(10000, 60000);

        LoadSettings();
        Tr.SetLang(_settings.Language);

        Title = Tr.Get("title");
        Width = _settings.WindowWidth > 0 ? _settings.WindowWidth : 1200;
        Height = _settings.WindowHeight > 0 ? _settings.WindowHeight : 700;
        if (_settings.WindowLeft >= 0) Left = _settings.WindowLeft;
        if (_settings.WindowTop >= 0) Top = _settings.WindowTop;
        MinWidth = 860; MinHeight = 620;
        Background = Br(C_BG);
        WindowStyle = WindowStyle.None;
        FontFamily = new FontFamily("Segoe UI");

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 44,
            ResizeBorderThickness = new Thickness(5),
            UseAeroCaptionButtons = false,
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0)
        });

        StateChanged += (_, _) =>
        {
            if (_titleMaxBtn != null)
                _titleMaxBtn.Content = WindowState == WindowState.Maximized ? "▢" : "▢";
        };

        try { Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(Paths.Resource("pic\\ico.ico"))); }
        catch { }

        BuildUI();
        Loaded += (_, _) =>
        {
            LoadData();
            _dataLoaded = true;
            InitTray();
            CenterIfDefault();
            RegisterAllHotkeys();
            SetWindowIcon(false);

            if (_settings.UseTunMode && !IsAdministrator())
            {
                var result = MessageBox.Show(
                    "TUN-режим требует прав администратора.\nПерезапустить приложение с правами администратора?",
                    "TUN Mode",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    RestartAsAdmin();
                }
                else
                {
                    _settings.UseTunMode = false;
                    SaveSettings();
                    if (_tunModeChk != null) _tunModeChk.IsChecked = false;
                }
            }
        };
        Closing += OnClosing;
        SizeChanged    += (_, _) => DebounceSaveWindowBounds();
        LocationChanged += (_, _) => DebounceSaveWindowBounds();

        this.AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnWindowPreviewKeyDown), true);

        Directory.CreateDirectory(Paths.DataDir);
        Directory.CreateDirectory(Paths.ListDir);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        _hwndSource?.AddHook(HwndHook);
        if (_dataLoaded) RegisterAllHotkeys();
    }

    private void SetWindowIcon(bool connected)
    {
        try
        {
            var icoName = connected ? "icoon.ico" : "ico.ico";
            var icoPath = Paths.Resource($"pic\\{icoName}");
            if (File.Exists(icoPath))
            {
                var icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(icoPath));
                Icon = icon;
                InvalidateVisual();
                UpdateLayout();
            }
        }
        catch { }
    }

    private void BuildUI()
    {
        var dock = new DockPanel();

        var titleBar = BuildTitleBar();
        DockPanel.SetDock(titleBar, Dock.Top);
        dock.Children.Add(titleBar);

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = BuildSidebar();
        Grid.SetColumn(sidebar, 0);
        root.Children.Add(sidebar);

        var mainArea = BuildMainArea();
        Grid.SetColumn(mainArea, 1);
        root.Children.Add(mainArea);

        dock.Children.Add(root);
        Content = dock;
    }

    private Border BuildTitleBar()
    {
        var bar = new Border
        {
            Background = Br(ParseColor("#0A0A0B")),
            Height = 44,
            BorderBrush = Br(C_BORDER),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0)
        };
        _titleStatusDot = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = Br(C_MUTED),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        WindowChrome.SetIsHitTestVisibleInChrome(_titleStatusDot, false);
        var titleTxt = new TextBlock
        {
            Text = "Taa Proxy",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Br(C_TXT),
            VerticalAlignment = VerticalAlignment.Center
        };
        left.Children.Add(_titleStatusDot);
        left.Children.Add(titleTxt);
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var minBtn = MakeTitleBarBtn("━", false, () => WindowState = WindowState.Minimized);
        _titleMaxBtn = MakeTitleBarBtn("▢", false,
            () => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        var closeBtn = MakeTitleBarBtn("⛌", true,
            () => { if (_settings.MinimizeOnClose) Hide(); else CleanupAndExit(); });

        foreach (var b in new[] { minBtn, _titleMaxBtn, closeBtn })
            WindowChrome.SetIsHitTestVisibleInChrome(b, true);

        right.Children.Add(minBtn);
        right.Children.Add(_titleMaxBtn);
        right.Children.Add(closeBtn);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        bar.Child = grid;
        return bar;
    }

    private static readonly string _titleBtnXaml = @"
<ControlTemplate TargetType='Button' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Border x:Name='bd' Background='{TemplateBinding Background}'>
    <TextBlock Text='{TemplateBinding Content}' HorizontalAlignment='Center' VerticalAlignment='Center'
               Foreground='{TemplateBinding Foreground}' FontSize='14' FontWeight='SemiBold'/>
  </Border>
</ControlTemplate>";

    private static ControlTemplate? _titleBtnTemplate;
    private static ControlTemplate GetTitleBtnTemplate()
    {
        _titleBtnTemplate ??= (ControlTemplate)XamlReader.Parse(_titleBtnXaml);
        return _titleBtnTemplate;
    }

    private Button MakeTitleBarBtn(string symbol, bool isClose, Action click)
    {
        var hoverBg = isClose ? C_DANGER : ParseColor("#2D2D32");
        var btn = new Button
        {
            Content = symbol,
            Width = 44,
            Height = 44,
            Background = Brushes.Transparent,
            Foreground = Br(C_MUTED),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Template = GetTitleBtnTemplate(),
            FontSize = 16
        };
        btn.MouseEnter += (_, _) =>
        {
            btn.Background = Br(hoverBg);
            btn.Foreground = Brushes.White;
        };
        btn.MouseLeave += (_, _) =>
        {
            btn.Background = Brushes.Transparent;
            btn.Foreground = Br(C_MUTED);
        };
        btn.Click += (_, _) => click();
        return btn;
    }

    private Border BuildSidebar()
    {
        var bg = new Border
        {
            Background = Br(C_SIDEBAR),
            BorderBrush = Br(C_BORDER),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Width = 314
        };

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0)
        };
        _serverListPanel = new StackPanel { Margin = new Thickness(12) };
        scroll.Content = _serverListPanel;

        var dockPanel = new DockPanel { LastChildFill = true };
        bg.Child = dockPanel;

        var bottomSp = new StackPanel { Margin = new Thickness(24, 0, 24, 32) };
        DockPanel.SetDock(bottomSp, Dock.Bottom);
        dockPanel.Children.Add(bottomSp);

        var topSp = new StackPanel();
        topSp.Children.Add(MakeText(Tr.Get("app_name"), 24, true, C_TXT, margin: new Thickness(24, 48, 24, 32)));
        topSp.Children.Add(MakeBtn(Tr.Get("add_from_clipboard"), C_ACCENT, C_TXT, h: 46, margin: new Thickness(24, 0, 24, 16), click: AddFromClipboard, radius: 12, bold: false, fontSize: 16, hPad: 13));
        topSp.Children.Add(MakeOutlineBtn(Tr.Get("import_configs"),
            margin: new Thickness(24, 0, 24, 8), click: () => ShowPanel("import"), fontSize: 15, h: 42, hPad: 13));
        topSp.Children.Add(MakeOutlineBtn(Tr.Get("proxy_list"),
            margin: new Thickness(24, 0, 24, 16), click: () => ShowPanel("proxylist"), fontSize: 15, h: 42, hPad: 13));
        topSp.Children.Add(MakeOutlineBtn(Tr.Get("settings"),
            margin: new Thickness(24, 0, 24, 16), click: () => ShowPanel("settings"), fontSize: 15, h: 42, hPad: 13));
        DockPanel.SetDock(topSp, Dock.Top);
        dockPanel.Children.Add(topSp);

        bottomSp.Children.Add(MakeHyperText("Taaproxy.ru", C_ACCENT,
            click: () => Process.Start(new ProcessStartInfo("https://taaproxy.ru/allversion") { UseShellExecute = true }),
            margin: new Thickness(0, 0, 0, 8), align: HorizontalAlignment.Center, underline: false, fontSize: 15));
        bottomSp.Children.Add(MakeOutlineBtn(Tr.Get("btn_exit"), fg: C_DANGER,
            margin: new Thickness(0), click: () => CleanupAndExit(), fontSize: 15, h: 42, hPad: 13));

        dockPanel.Children.Add(scroll);
        return bg;
    }

    private Grid BuildMainArea()
    {
        var g = new Grid { Margin = new Thickness(32) };

        _serversPanel = BuildServersPanel();
        _serversScrollViewer = new ScrollViewer
        {
            Content = _serversPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _settingsPanel = BuildSettingsPanel();
        _settingsPanel.Visibility = Visibility.Collapsed;
        _importPanel = BuildImportPanel();
        _importPanel.Visibility = Visibility.Collapsed;
        _proxyListPanel = BuildProxyListPanel();
        _proxyListPanel.Visibility = Visibility.Collapsed;

        g.Children.Add(_serversScrollViewer);
        g.Children.Add(_settingsPanel);
        g.Children.Add(_importPanel);
        g.Children.Add(_proxyListPanel);

        return g;
    }

    private ToggleButton MakeToggleSwitch(string text, bool isChecked, Action<bool> onChanged)
    {
        const string templateXaml = @"
<ControlTemplate TargetType='ToggleButton' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width='Auto'/>
      <ColumnDefinition Width='*'/>
    </Grid.ColumnDefinitions>
    <Border x:Name='Border' Width='44' Height='24' CornerRadius='12' Background='#3F3F46' Grid.Column='0'/>
    <Border x:Name='Thumb' Width='20' Height='20' Margin='2' CornerRadius='10' Background='#FFFFFF' HorizontalAlignment='Left' Grid.Column='0'/>
    <ContentPresenter Grid.Column='1' Content='{TemplateBinding Content}' Margin='12,0,0,0' HorizontalAlignment='Left' VerticalAlignment='Center'/>
  </Grid>
  <ControlTemplate.Triggers>
    <Trigger Property='IsChecked' Value='True'>
      <Setter TargetName='Border' Property='Background' Value='#6366F1'/>
      <Setter TargetName='Thumb' Property='HorizontalAlignment' Value='Right'/>
    </Trigger>
    <Trigger Property='IsEnabled' Value='False'>
      <Setter TargetName='Border' Property='Opacity' Value='0.5'/>
    </Trigger>
  </ControlTemplate.Triggers>
</ControlTemplate>";

        var toggle = new ToggleButton
        {
            IsChecked = isChecked,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand,
            Margin = new Thickness(0),
            Content = text,
            Foreground = Br(C_TXT),
            Template = (ControlTemplate)XamlReader.Parse(templateXaml)
        };
        toggle.Checked += (_, _) => onChanged(true);
        toggle.Unchecked += (_, _) => onChanged(false);
        return toggle;
    }

    private StackPanel BuildServersPanel()
    {
        var sp = new StackPanel();

        _nameLabel = MakeText(Tr.Get("server_not_selected"), 16, false, C_TXT);
        _hostLabel = MakeText("—", 15, false, C_TXT);
        _pingLabel = MakeText("", 15, true, C_TXT);

        var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        nameRow.Children.Add(MakeText(Tr.Get("name"), 14, false, C_MUTED, margin: new Thickness(0, 0, 8, 0)));

        var nameContainer = new Border { Child = _nameLabel };
        _nameLabel.MouseLeftButtonDown += (s, e) => StartEditName();
        _nameLabel.Cursor = Cursors.Hand;
        nameRow.Children.Add(nameContainer);

        var editNameBtn = MakeSmallTextBtn("ред.", C_MUTED, click: StartEditName);
        editNameBtn.Margin = new Thickness(8, 0, 0, 0);
        nameRow.Children.Add(editNameBtn);

        var hostRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        hostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        hostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hostRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var addressLabel = MakeText(Tr.Get("address"), 14, false, C_MUTED, margin: new Thickness(0, 0, 8, 0));
        Grid.SetColumn(addressLabel, 0);
        hostRow.Children.Add(addressLabel);

        Grid.SetColumn(_hostLabel, 1);
        hostRow.Children.Add(_hostLabel);

        var hideToggle = MakeToggleSwitch(Tr.Get("hide_ip"), _hideIp, (v) => { _hideIp = v; UpdateHostDisplay(); });
        hideToggle.Margin = new Thickness(20, 0, 0, 0);
        Grid.SetColumn(hideToggle, 2);
        hostRow.Children.Add(hideToggle);

        var pingRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        _pingBtn = MakeOutlineBtn(Tr.Get("check_ping"), margin: new Thickness(0, 0, 12, 0),
            click: () => _ = CheckPingAsync(), h: 32, enabled: false, w: 100);
        _defaultBtn = MakeOutlineBtn(Tr.Get("set_default"), fg: C_DANGER, margin: new Thickness(0, 0, 12, 0),
            click: SetCurrentDefault, h: 32, enabled: false, w: 100);
        pingRow.Children.Add(_pingBtn);
        pingRow.Children.Add(_defaultBtn);
        pingRow.Children.Add(_pingLabel);

        var infoHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
        _deleteBtn = MakeSmallTextBtn(Tr.Get("btn_delete"), C_DANGER, click: DeleteCurrentServer, enabled: false);
        DockPanel.SetDock(_deleteBtn, Dock.Right);
        infoHeader.Children.Add(_deleteBtn);
        infoHeader.Children.Add(MakeText(Tr.Get("connection_info"), 18, true, C_TXT));

        var infoCnt = new StackPanel();
        infoCnt.Children.Add(infoHeader);
        infoCnt.Children.Add(nameRow);
        infoCnt.Children.Add(hostRow);
        infoCnt.Children.Add(pingRow);

        _serverInfoCard = MakeCard(infoCnt);
        _serverInfoCard.Margin = new Thickness(0, 0, 0, 24);
        sp.Children.Add(_serverInfoCard);

        _statusLabel = MakeText(Tr.Get("status_disconnected"), 24, true, C_MUTED);
        _statusLabel.VerticalAlignment = VerticalAlignment.Center;
        _connectBtn = MakeBtn(Tr.Get("btn_connect"), C_ACCENT, C_TXT, h: 48, w: 220,
            radius: 12, bold: true, enabled: false, click: ToggleConnection);
        var statusRow = new DockPanel();
        DockPanel.SetDock(_connectBtn, Dock.Right);
        statusRow.Children.Add(_connectBtn);
        statusRow.Children.Add(_statusLabel);

        var statusCard = MakeCard(statusRow);
        statusCard.Margin = new Thickness(0, 0, 0, 24);
        sp.Children.Add(statusCard);

        _splitSwitch = MakeToggleSwitch(Tr.Get("split_tunneling"), _settings.SplitTunneling,
            (v) => { _settings.SplitTunneling = v; OnSplitToggle(); });

        _routesCombo = new ComboBox
        {
            Height = 40,
            MinWidth = 220,
            Background = Br(C_SIDEBAR),
            Foreground = Br(C_TXT),
            BorderBrush = Br(C_BORDER),
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0)
        };
        _routesCombo.SelectionChanged += (_, _) =>
        {
            if (_routesCombo.SelectedItem is string s && s != _currentRoutesFile)
            {
                SaveCurrentRoutes();
                _currentRoutesFile = s;
                LoadRoutesFile(s);
                if (_proxyRoutesCombo != null && _proxyRoutesCombo.SelectedItem as string != s)
                    _proxyRoutesCombo.SelectedItem = s;
                RestartIfNeeded();
                UpdateTrayMenu();
            }
        };

        var routeHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(_splitSwitch, Dock.Right);
        routeHeader.Children.Add(_splitSwitch);
        routeHeader.Children.Add(MakeText(Tr.Get("routing"), 18, true, C_TXT));

        var routeCardContent = new StackPanel();
        routeCardContent.Children.Add(routeHeader);
        routeCardContent.Children.Add(_routesCombo);

        var routeCard = MakeCard(routeCardContent);
        sp.Children.Add(routeCard);

        return sp;
    }

    private Grid BuildProxyListPanel()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
        header.Children.Add(MakeOutlineBtn(Tr.Get("back"), h: 32, click: () => ShowPanel("servers"), hPad: 14, w: 80));
        header.Children.Add(MakeText(Tr.Get("proxy_list_title"), 14, true, C_TXT, margin: new Thickness(20, 0, 0, 0)));
        Grid.SetRow(header, 0);
        g.Children.Add(header);

        var content = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };

        var fileRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        fileRow.Children.Add(MakeText(Tr.Get("current_file"), 14, false, C_MUTED, margin: new Thickness(0, 0, 10, 0)));
        _proxyRoutesCombo = new ComboBox
        {
            Height = 36,
            MinWidth = 200,
            Background = Br(C_SIDEBAR),
            Foreground = Br(C_TXT),
            BorderBrush = Br(C_BORDER),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        _proxyRoutesCombo.SelectionChanged += (_, _) =>
        {
            if (_proxyRoutesCombo.SelectedItem is string s && s != _currentRoutesFile)
            {
                SaveCurrentRoutes();
                _currentRoutesFile = s;
                LoadRoutesFile(s);
                if (_routesCombo.SelectedItem as string != s)
                    _routesCombo.SelectedItem = s;
                RestartIfNeeded();
                UpdateTrayMenu();
            }
        };
        fileRow.Children.Add(_proxyRoutesCombo);
        content.Children.Add(fileRow);

        var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        _proxyNewBtn = MakeOutlineBtn(Tr.Get("new_routes_file"), margin: new Thickness(0, 0, 8, 0), h: 36, click: CreateRoutesFile, w: 120);
        _proxyDeleteBtn = MakeOutlineBtn(Tr.Get("delete_routes_file"), fg: C_DANGER, margin: new Thickness(0, 0, 8, 0), h: 36, click: DeleteRoutesFile, w: 120);
        _proxyRenameBtn = MakeOutlineBtn(Tr.Get("rename_routes_file"), margin: new Thickness(0, 0, 8, 0), h: 36, click: RenameRoutesFile, w: 120);
        buttonsRow.Children.Add(_proxyNewBtn);
        buttonsRow.Children.Add(_proxyDeleteBtn);
        buttonsRow.Children.Add(_proxyRenameBtn);
        content.Children.Add(buttonsRow);

        _proxyRoutingText = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            Background = Br(C_SIDEBAR),
            Foreground = Br(C_TXT),
            BorderBrush = Br(C_BORDER),
            BorderThickness = new Thickness(1),
            CaretBrush = Br(C_TXT),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Padding = new Thickness(12),
            Height = 360,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 16),
            IsEnabled = true
        };
        var textBoxStyle = new Style(typeof(TextBox));
        textBoxStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Br(C_BORDER)));
        textBoxStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        var focusTrigger = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
        focusTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, Br(C_BORDER)));
        var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        mouseOverTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, Br(C_BORDER)));
        textBoxStyle.Triggers.Add(focusTrigger);
        textBoxStyle.Triggers.Add(mouseOverTrigger);
        _proxyRoutingText.Style = textBoxStyle;

        content.Children.Add(_proxyRoutingText);

        _proxySaveBtn = MakeBtn(Tr.Get("save_routes"), C_ACCENT, C_TXT, h: 40, w: 120, radius: 8, click: SaveRoutesFromEditor);
        _proxySaveBtn.IsEnabled = true;
        content.Children.Add(_proxySaveBtn);

        var scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(scroll, 1);
        g.Children.Add(scroll);

        return g;
    }

    private Grid BuildSettingsPanel()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
        header.Children.Add(MakeOutlineBtn(Tr.Get("back"), h: 32, click: () => ShowPanel("servers"), hPad: 14, w: 80));
        header.Children.Add(MakeText(Tr.Get("settings_title"), 14, true, C_TXT, margin: new Thickness(20, 0, 0, 0)));
        Grid.SetRow(header, 0); g.Children.Add(header);

        var tabs = new TabControl { Background = Br(C_CARD), BorderBrush = Br(C_BORDER) };
        ApplyTabStyle(tabs);

        tabs.Items.Add(BuildTabGeneral());
        tabs.Items.Add(BuildTabDns());
        tabs.Items.Add(BuildTabExceptions());
        tabs.Items.Add(BuildTabHotkeys());
        tabs.Items.Add(BuildTabConfig());

        Grid.SetRow(tabs, 1); g.Children.Add(tabs);
        return g;
    }

    private TabItem BuildTabGeneral()
    {
        var sp = new StackPanel { Margin = new Thickness(20) };

        _autostartChk = MakeToggleSwitch(Tr.Get("autostart"), Autostart.IsEnabled(), v => Autostart.Set(v));
        _minimizeChk = MakeToggleSwitch(Tr.Get("minimize_to_tray"), _settings.MinimizeOnClose, v => { _settings.MinimizeOnClose = v; SaveSettings(); });
        _killSwitchChk = MakeToggleSwitch(Tr.Get("kill_switch"), _settings.KillSwitch, v => { _settings.KillSwitch = v; SaveSettings(); });
        _autoReconnectChk = MakeToggleSwitch(Tr.Get("auto_reconnect"), _settings.AutoReconnect, v => { _settings.AutoReconnect = v; SaveSettings(); });
        _debugChk = MakeToggleSwitch(Tr.Get("debug_mode"), _settings.DebugMode, v => { _settings.DebugMode = v; SaveSettings(); });
        _notificationsChk = MakeToggleSwitch(Tr.Get("notifications"), _settings.EnableNotifications, v => { _settings.EnableNotifications = v; SaveSettings(); });

        _tunModeChk = MakeToggleSwitch(Tr.Get("tun_mode"), _settings.UseTunMode, v =>
        {
            if (v && !IsAdministrator())
            {
                var result = MessageBox.Show(
                    "TUN-режим требует прав администратора.\nПерезапустить приложение с правами администратора?",
                    "TUN Mode",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _settings.UseTunMode = true;
                    SaveSettings();
                    RestartAsAdmin();
                }
                else
                {
                    _tunModeChk!.IsChecked = false;
                }
                return;
            }

            _settings.UseTunMode = v;
            SaveSettings();
            if (_proxyProcess != null)
                RestartIfNeeded();
        });
        _tunModeChk.Margin = new Thickness(0, 0, 0, 8);

        var langRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        langRow.Children.Add(MakeText(Tr.Get("language_label"), 14, false, C_MUTED));
        _langCombo = new ComboBox { Margin = new Thickness(10, 0, 0, 0), MinWidth = 120, Height = 36, FontSize = 14 };
        _langCombo.Items.Add("Русский"); _langCombo.Items.Add("English");
        _langCombo.SelectedIndex = _settings.Language == "ru" ? 0 : 1;
        _langCombo.SelectionChanged += (_, _) =>
        {
            _settings.Language = _langCombo.SelectedIndex == 0 ? "ru" : "en";
            SaveSettings();
            MessageBox.Show("Перезапустите приложение для смены языка.\nRestart app to change language.",
                "Language", MessageBoxButton.OK, MessageBoxImage.Information);
        };
        langRow.Children.Add(_langCombo);

        sp.Children.Add(MakeText(Tr.Get("settings_title"), 18, true, C_TXT, margin: new Thickness(0, 0, 0, 16)));
        foreach (var chk in new[] { _autostartChk, _minimizeChk, _killSwitchChk, _autoReconnectChk, _debugChk, _notificationsChk, _tunModeChk })
        { chk.Margin = new Thickness(0, 0, 0, 8); sp.Children.Add(chk); }
        sp.Children.Add(langRow);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        btnRow.Children.Add(MakeOutlineBtn(Tr.Get("view_logs"), h: 36, margin: new Thickness(0, 0, 8, 0), click: ViewLogs, w: 140));
        btnRow.Children.Add(MakeOutlineBtn(Tr.Get("restart_app"), h: 36, click: RestartApp, w: 140));
        sp.Children.Add(btnRow);

        var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return new TabItem { Header = Tr.Get("tab_general"), Content = scroll, Foreground = Br(C_TXT) };
    }

    private TabItem BuildTabDns()
    {
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(MakeText(Tr.Get("dns_settings"), 18, true, C_TXT, margin: new Thickness(0, 0, 0, 16)));

        var typeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        typeRow.Children.Add(MakeText(Tr.Get("dns_type"), 14, false, C_MUTED));
        _dnsTypeCombo = new ComboBox { Margin = new Thickness(10, 0, 0, 0), MinWidth = 200, Height = 36 };
        _dnsTypeCombo.Items.Add(Tr.Get("dns_system"));
        _dnsTypeCombo.Items.Add(Tr.Get("dns_doh"));
        _dnsTypeCombo.Items.Add(Tr.Get("dns_dot"));
        _dnsTypeCombo.SelectedIndex = _settings.DnsType switch { "doh" => 1, "dot" => 2, _ => 0 };
        typeRow.Children.Add(_dnsTypeCombo);

        var addrRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        addrRow.Children.Add(MakeText(Tr.Get("dns_server_address"), 14, false, C_MUTED));
        _dnsAddrBox = new TextBox
        {
            Margin = new Thickness(10, 0, 0, 0),
            MinWidth = 250,
            Text = _settings.DnsServer,
            Background = Br(C_SIDEBAR),
            Foreground = Br(C_TXT),
            BorderBrush = Br(C_BORDER),
            CaretBrush = Br(C_TXT),
            Padding = new Thickness(6, 4, 6, 4)
        };
        addrRow.Children.Add(_dnsAddrBox);

        _dnsProxyChk = MakeToggleSwitch(Tr.Get("dns_through_proxy"), _settings.DnsThroughProxy, _ => { });
        _dnsProxyChk.Margin = new Thickness(0, 0, 0, 12);

        _dnsTestLabel = MakeText("", 13, false, C_MUTED);
        var testRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        testRow.Children.Add(MakeOutlineBtn(Tr.Get("dns_test"), h: 36, margin: new Thickness(0, 0, 10, 0), click: TestDns, w: 120));
        testRow.Children.Add(_dnsTestLabel);

        sp.Children.Add(typeRow);
        sp.Children.Add(addrRow);
        sp.Children.Add(_dnsProxyChk);

        var saveRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        saveRow.Children.Add(MakeBtn(Tr.Get("save"), C_ACCENT, C_TXT, h: 36, margin: new Thickness(0, 0, 8, 0), click: SaveDns, w: 120));
        sp.Children.Add(saveRow);
        sp.Children.Add(testRow);

        return new TabItem { Header = Tr.Get("tab_dns"), Content = sp, Foreground = Br(C_TXT) };
    }

    private TabItem BuildTabExceptions()
    {
        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(MakeText(Tr.Get("app_exceptions"), 16, true, C_TXT, margin: new Thickness(0, 0, 0, 4)));
        sp.Children.Add(MakeText(Tr.Get("app_exceptions_desc"), 12, false, C_MUTED, margin: new Thickness(0, 0, 0, 10)));
        var appBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        var addAppBtn = MakeOutlineBtn(Tr.Get("add_app"), h: 32, margin: new Thickness(0, 0, 6, 0), click: ShowAddAppMenu, w: 170);
        _removeAppBtn = MakeOutlineBtn(Tr.Get("remove_app"), fg: C_DANGER, h: 32, enabled: false, click: RemoveAppException, w: 100);
        appBtns.Children.Add(addAppBtn); appBtns.Children.Add(_removeAppBtn);
        sp.Children.Add(appBtns);
        _appExcList = new StackPanel();
        var appExcScroll = new ScrollViewer { Content = _appExcList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 172 };
        sp.Children.Add(MakeCard(appExcScroll, margin: new Thickness(0, 0, 0, 16), maxHeight: 220));

        sp.Children.Add(MakeText(Tr.Get("domain_exceptions"), 16, true, C_TXT, margin: new Thickness(0, 0, 0, 4)));
        sp.Children.Add(MakeText(Tr.Get("domain_exceptions_desc"), 12, false, C_MUTED, margin: new Thickness(0, 0, 0, 10)));
        var domBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        var addDomBtn = MakeOutlineBtn(Tr.Get("add_domain"), h: 32, margin: new Thickness(0, 0, 6, 0), click: AddDomainException, w: 140);
        _removeDomBtn = MakeOutlineBtn(Tr.Get("remove_domain"), fg: C_DANGER, h: 32, enabled: false, click: RemoveDomainException, w: 100);
        domBtns.Children.Add(addDomBtn); domBtns.Children.Add(_removeDomBtn);
        sp.Children.Add(domBtns);
        _domExcList = new StackPanel();
        var domExcScroll = new ScrollViewer { Content = _domExcList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 172 };
        sp.Children.Add(MakeCard(domExcScroll, margin: new Thickness(0, 0, 0, 0), maxHeight: 220));

        var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return new TabItem { Header = Tr.Get("tab_exceptions"), Content = scroll, Foreground = Br(C_TXT) };
    }

    private Grid BuildImportPanel()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
        header.Children.Add(MakeOutlineBtn(Tr.Get("back"), h: 32, click: () => ShowPanel("servers"), hPad: 14, w: 80));
        header.Children.Add(MakeText(Tr.Get("import_title"), 14, true, C_TXT, margin: new Thickness(20, 0, 0, 0)));
        Grid.SetRow(header, 0); g.Children.Add(header);

        var sp = new StackPanel { Margin = new Thickness(0, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 400 };
        sp.Children.Add(MakeBtn(Tr.Get("import_file"), C_SIDEBAR, C_TXT, h: 48, radius: 8,
            bold: true, margin: new Thickness(0, 0, 0, 12), click: ImportSitesFromFile));
        sp.Children.Add(MakeBtn(Tr.Get("import_clipboard"), C_ACCENT, C_TXT, h: 48, radius: 8,
            bold: true, click: ImportServersFromFile));
        Grid.SetRow(sp, 1); g.Children.Add(sp);

        return g;
    }

    private TabItem BuildTabConfig()
    {
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(MakeText(Tr.Get("tab_config"), 18, true, C_TXT, margin: new Thickness(0, 0, 0, 16)));

        var exportGroup = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
        exportGroup.Children.Add(MakeText("Экспорт", 14, true, C_TXT, margin: new Thickness(0, 0, 0, 8)));

        var exportBtn = MakeOutlineBtn(Tr.Get("export_config"), h: 36, click: ExportConfiguration, w: 200);
        exportBtn.Margin = new Thickness(0, 0, 0, 8);
        exportGroup.Children.Add(exportBtn);

        _exportServersChk = MakeToggleSwitch(Tr.Get("export_servers"), false, _ => { });
        _exportServersChk.Margin = new Thickness(0, 0, 0, 4);
        exportGroup.Children.Add(_exportServersChk);

        _exportRoutesChk = MakeToggleSwitch(Tr.Get("export_routes"), false, _ => { });
        _exportRoutesChk.Margin = new Thickness(0, 0, 0, 0);
        exportGroup.Children.Add(_exportRoutesChk);

        sp.Children.Add(exportGroup);

        var importGroup = new StackPanel();
        importGroup.Children.Add(MakeText("Импорт", 14, true, C_TXT, margin: new Thickness(0, 0, 0, 8)));

        var importBtn = MakeOutlineBtn(Tr.Get("import_config"), h: 36, click: ImportConfiguration, w: 200);
        importGroup.Children.Add(importBtn);

        sp.Children.Add(importGroup);

        var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return new TabItem { Header = Tr.Get("tab_config"), Content = scroll, Foreground = Br(C_TXT) };
    }

    private void ExportConfiguration()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Taa Configuration (*.taa)|*.taa|All files (*.*)|*.*",
            Title = "Export configuration",
            FileName = $"TaaConfig_{DateTime.Now:yyyyMMdd_HHmmss}.taa"
        };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            var export = new Dictionary<string, object>
            {
                ["version"] = "1.0",
                ["settings"] = _settings
            };

            if (_exportServersChk?.IsChecked == true)
            {
                export["servers"] = _servers;
            }

            if (_exportRoutesChk?.IsChecked == true)
            {
                var routes = new Dictionary<string, string>();
                if (Directory.Exists(Paths.ListDir))
                {
                    foreach (var file in Directory.GetFiles(Paths.ListDir, "*.txt"))
                    {
                        var name = Path.GetFileName(file);
                        var content = File.ReadAllText(file);
                        routes[name] = content;
                    }
                }
                export["routes"] = routes;
            }

            var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);

            MessageBox.Show(Tr.Get("export_success"), "Taa Proxy", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Tr.Get("export_error")} {ex.Message}", Tr.Get("error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportConfiguration()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Taa Configuration (*.taa)|*.taa|JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import configuration"
        };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("settings", out var settingsElem))
            {
                var importedSettings = JsonSerializer.Deserialize<AppSettings>(settingsElem.GetRawText());
                if (importedSettings != null)
                {
                    importedSettings.WindowLeft = _settings.WindowLeft;
                    importedSettings.WindowTop = _settings.WindowTop;
                    importedSettings.WindowWidth = _settings.WindowWidth;
                    importedSettings.WindowHeight = _settings.WindowHeight;
                    _settings = importedSettings;
                    SaveSettings();
                    Tr.SetLang(_settings.Language);
                }
            }

            if (root.TryGetProperty("servers", out var serversElem))
            {
                var importedServers = JsonSerializer.Deserialize<List<ServerModel>>(serversElem.GetRawText());
                if (importedServers != null)
                {
                    _servers = importedServers;
                    SaveServers();
                }
            }

            if (root.TryGetProperty("routes", out var routesElem))
            {
                var routes = JsonSerializer.Deserialize<Dictionary<string, string>>(routesElem.GetRawText());
                if (routes != null)
                {
                    Directory.CreateDirectory(Paths.ListDir);
                    foreach (var kv in routes)
                    {
                        var path = Path.Combine(Paths.ListDir, kv.Key);
                        File.WriteAllText(path, kv.Value);
                    }
                    RefreshRoutesListForCombos();
                    LoadRoutesFile(_currentRoutesFile);
                }
            }

            RefreshServerList();
            SelectDefaultServer();
            RefreshSettingsUI();
            UpdateSplitState();
            UpdateTrayMenu();

            MessageBox.Show(Tr.Get("import_success"), "Taa Proxy", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Tr.Get("import_error")} {ex.Message}", Tr.Get("error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static readonly string _btnXaml = @"
<ControlTemplate TargetType='Button' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Border x:Name='bd' Background='{TemplateBinding Background}'
          BorderBrush='{TemplateBinding BorderBrush}'
          BorderThickness='{TemplateBinding BorderThickness}'
          CornerRadius='8'>
    <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
  </Border>
  <ControlTemplate.Triggers>
    <Trigger Property='IsEnabled' Value='False'>
      <Setter TargetName='bd' Property='Opacity' Value='0.4'/>
    </Trigger>
  </ControlTemplate.Triggers>
</ControlTemplate>";

    private static ControlTemplate? _btnTemplate;
    private static ControlTemplate GetBtnTemplate()
    {
        _btnTemplate ??= (ControlTemplate)XamlReader.Parse(_btnXaml);
        return _btnTemplate;
    }

    private Button MakeBtn(string text, Color bg, Color fg, double h = 44, double w = double.NaN,
        double radius = 12, bool bold = false, bool enabled = true, Thickness margin = default,
        Action? click = null, double fontSize = 14, double hPad = 16)
    {
        var btn = new Button
        {
            Content = text,
            Height = h,
            IsEnabled = enabled,
            Background = Br(bg),
            Foreground = Br(fg),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Cursor = Cursors.Hand,
            Margin = margin,
            Template = GetBtnTemplate(),
            Tag = new CornerRadius(radius),
            Padding = new Thickness(hPad, 0, hPad, 0)
        };
        if (!double.IsNaN(w)) btn.Width = w;
        if (click != null) btn.Click += (_, _) => click();
        return btn;
    }

    private Button MakeOutlineBtn(string text, Color? fg = null, Thickness margin = default,
        double h = 44, bool enabled = true, Action? click = null, double hPad = 16, double fontSize = 13, double? w = null)
    {
        var btn = new Button
        {
            Content = text,
            Height = h,
            IsEnabled = enabled,
            Background = Brushes.Transparent,
            Foreground = Br(fg ?? C_TXT),
            BorderBrush = Br(C_BORDER),
            BorderThickness = new Thickness(1),
            FontSize = fontSize,
            Cursor = Cursors.Hand,
            Margin = margin,
            Template = GetBtnTemplate(),
            Tag = new CornerRadius(10),
            Padding = new Thickness(hPad, 0, hPad, 0)
        };
        if (w.HasValue) btn.Width = w.Value;
        if (click != null) btn.Click += (_, _) => click();
        return btn;
    }

    private Button MakeSmallTextBtn(string text, Color fg, Action? click = null, bool enabled = true)
    {
        var btn = new Button
        {
            Content = text,
            Height = 26,
            Padding = new Thickness(6, 0, 6, 0),
            Background = Brushes.Transparent,
            Foreground = Br(fg),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Cursor = Cursors.Hand,
            IsEnabled = enabled,
            Template = GetBtnTemplate(),
            Tag = new CornerRadius(4)
        };
        if (click != null) btn.Click += (_, _) => click();
        return btn;
    }

    private TextBlock MakeText(string text, double fs = 14, bool bold = false, Color? fg = null,
        Thickness margin = default)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fs,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Br(fg ?? C_TXT),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = margin,
            TextWrapping = TextWrapping.Wrap
        };
    }

    private TextBlock MakeHyperText(string text, Color fg, Action? click = null,
        Thickness margin = default, HorizontalAlignment align = HorizontalAlignment.Left, bool underline = true, double fontSize = 13)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = Br(fg),
            Cursor = Cursors.Hand,
            Margin = margin,
            HorizontalAlignment = align
        };
        if (underline)
            tb.TextDecorations = TextDecorations.Underline;
        if (click != null) tb.MouseLeftButtonUp += (_, _) => click();
        return tb;
    }

    private Border MakeCard(UIElement content, Thickness margin = default, double maxHeight = double.NaN)
    {
        var b = new Border
        {
            Background = Br(C_CARD),
            BorderBrush = Br(C_BORDER),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(24),
            Child = content,
            Margin = margin
        };
        if (!double.IsNaN(maxHeight)) b.MaxHeight = maxHeight;
        return b;
    }

    private void ApplyTabStyle(TabControl tc)
    {
        tc.Background = Br(C_CARD);
        tc.BorderBrush = Br(C_BORDER);
        tc.Foreground = Br(C_TXT);
    }

    private void ShowPanel(string name)
    {
        _serversScrollViewer.Visibility = name == "servers" ? Visibility.Visible : Visibility.Collapsed;
        _settingsPanel.Visibility = name == "settings" ? Visibility.Visible : Visibility.Collapsed;
        _importPanel.Visibility = name == "import" ? Visibility.Visible : Visibility.Collapsed;
        _proxyListPanel.Visibility = name == "proxylist" ? Visibility.Visible : Visibility.Collapsed;
        _currentPanel = name;
        if (name == "settings") RefreshSettingsUI();
        if (name == "exceptions") { RefreshAppExcListUI(); RefreshDomExcListUI(); }
        if (name == "proxylist") RefreshProxyListPanel();
    }

    private void RefreshProxyListPanel()
    {
        if (_proxyRoutesCombo != null)
        {
            RefreshRoutesListForCombos();
            _proxyRoutesCombo.SelectedItem = _currentRoutesFile;
        }
        if (_proxyRoutingText != null)
            _proxyRoutingText.Text = _routingTextBacking;
        if (_proxyRoutingText != null) _proxyRoutingText.IsEnabled = true;
        if (_proxySaveBtn != null) _proxySaveBtn.IsEnabled = true;
    }

    private string _routingTextBacking = "";
    private void LoadRoutesFile(string filename)
    {
        var path = Path.Combine(Paths.ListDir, filename);
        _routingTextBacking = File.Exists(path) ? File.ReadAllText(path) : "";
        if (_proxyRoutingText != null && _proxyRoutingText.IsLoaded)
            _proxyRoutingText.Text = _routingTextBacking;
    }

    private void SaveCurrentRoutes()
    {
        if (string.IsNullOrEmpty(_currentRoutesFile)) return;
        Directory.CreateDirectory(Paths.ListDir);
        string content = _routingTextBacking;
        if (_proxyRoutingText != null && _proxyListPanel.Visibility == Visibility.Visible)
            content = _proxyRoutingText.Text;
        File.WriteAllText(Path.Combine(Paths.ListDir, _currentRoutesFile), content);
        _routingTextBacking = content;
    }

    private void SaveRoutesFromEditor()
    {
        if (_proxyRoutingText != null)
        {
            _routingTextBacking = _proxyRoutingText.Text;
            SaveCurrentRoutes();
            RestartIfNeeded();
        }
    }

    private void RefreshRoutesListForCombos()
    {
        var files = Directory.Exists(Paths.ListDir)
            ? Directory.GetFiles(Paths.ListDir, "*.txt").Select(Path.GetFileName).OfType<string>().OrderBy(x => x).ToList()
            : new List<string>();
        if (!files.Any())
        {
            var def = Path.Combine(Paths.ListDir, "routes.txt");
            Directory.CreateDirectory(Paths.ListDir);
            if (!File.Exists(def)) File.WriteAllText(def, "instagram.com\ntwitter.com\n2ip.ru");
            files = new List<string> { "routes.txt" };
        }
        _routesCombo.ItemsSource = files;
        if (_proxyRoutesCombo != null)
            _proxyRoutesCombo.ItemsSource = files;
        if (!files.Contains(_currentRoutesFile)) _currentRoutesFile = files[0];
        if (_routesCombo.SelectedItem as string != _currentRoutesFile)
            _routesCombo.SelectedItem = _currentRoutesFile;
        if (_proxyRoutesCombo != null && _proxyRoutesCombo.SelectedItem as string != _currentRoutesFile)
            _proxyRoutesCombo.SelectedItem = _currentRoutesFile;
    }

    private void LoadData()
    {
        LoadServers();
        RefreshRoutesListForCombos();
        LoadRoutesFile(_currentRoutesFile);
        UpdateSplitState();
        SelectDefaultServer();
    }

    private void LoadSettings()
    {
        if (!File.Exists(Paths.SettingsFile)) return;
        try
        {
            var json = File.ReadAllText(Paths.SettingsFile);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { _settings = new AppSettings(); }
        _currentRoutesFile = _settings.CurrentRoutesFile;

        for (int i = 0; i < _settings.DomainExceptions.Count; i++)
        {
            var d = _settings.DomainExceptions[i];
            if (string.IsNullOrEmpty(d)) continue;
            if (d.Contains("xn--", StringComparison.OrdinalIgnoreCase)) continue;
            if (d.Any(c => c > 127))
            {
                try
                {
                    bool startsWithDot = d.StartsWith('.');
                    string part = startsWithDot ? d.Substring(1) : d;
                    string puny = _idn.GetAscii(part);
                    string newVal = startsWithDot ? "." + puny : puny;
                    _settings.DomainExceptions[i] = newVal;
                }
                catch { }
            }
        }
    }

    private void SaveSettings()
    {
        _settings.CurrentRoutesFile = _currentRoutesFile;
        Directory.CreateDirectory(Paths.DataDir);
        File.WriteAllText(Paths.SettingsFile, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
        FileAcl.SecureFile(Paths.SettingsFile);
    }

    private void DebounceSaveWindowBounds()
    {
        if (_boundsTimer == null)
        {
            _boundsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            _boundsTimer.Tick += (_, _) => { _boundsTimer.Stop(); SaveWindowBounds(); };
        }
        _boundsTimer.Stop();
        _boundsTimer.Start();
    }

    private void SaveWindowBounds()
    {
        _settings.WindowLeft = Left; _settings.WindowTop = Top;
        _settings.WindowWidth = Width; _settings.WindowHeight = Height;
        SaveSettings();
    }

    private void LoadServers()
    {
        if (!File.Exists(Paths.DbFile)) return;
        try
        {
            var raw = File.ReadAllBytes(Paths.DbFile);
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("enc", out var encProp) && encProp.GetString() == "dpapi")
            {
                var b64 = doc.RootElement.GetProperty("data").GetString()!;
                var decrypted = Dpapi.Decrypt(Convert.FromBase64String(b64));
                _servers = JsonSerializer.Deserialize<List<ServerModel>>(decrypted) ?? new();
            }
            else
            {
                _servers = JsonSerializer.Deserialize<List<ServerModel>>(raw) ?? new();
                SaveServers();
            }
        }
        catch { _servers = new(); }
        MigrateServers();
        RefreshServerList();
    }

    private void MigrateServers()
    {
        bool changed = false;
        int before = _servers.Count;
        _servers.RemoveAll(sv => sv.Type == "ss" &&
            sv.Method.StartsWith("2022-", StringComparison.OrdinalIgnoreCase));
        if (_servers.Count != before) changed = true;
        if (changed)
            SaveServers();
    }

    private void SaveServers()
    {
        Directory.CreateDirectory(Paths.DataDir);
        var plain = JsonSerializer.SerializeToUtf8Bytes(_servers, new JsonSerializerOptions { WriteIndented = true });
        var enc = Dpapi.Encrypt(plain);
        var wrap = new { enc = "dpapi", data = Convert.ToBase64String(enc) };
        File.WriteAllText(Paths.DbFile, JsonSerializer.Serialize(wrap));
        FileAcl.SecureFile(Paths.DbFile);
    }

    private void SelectDefaultServer()
    {
        var def = _settings.DefaultServer;
        if (!string.IsNullOrEmpty(def))
        {
            var idx = _servers.FindIndex(s => s.Name == def);
            if (idx >= 0) SelectServer(idx);
        }
    }

    private List<Border> _serverFrames = new();

    private void RefreshServerList()
    {
        _serverListPanel.Children.Clear();
        _serverFrames.Clear();
        var def = _settings.DefaultServer;

        for (int i = 0; i < _servers.Count; i++)
        {
            var sv = _servers[i];
            var idx = i;
            bool isDefault = sv.Name == def;
            bool isConnected = idx == _connectedIdx;
            bool isSelected = idx == _selectedIdx;

            var frame = new Border
            {
                Background = isSelected ? Br(C_ACTIVE) : Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                Height = 48
            };

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = isConnected ? Br(C_SUCCESS) : Br(C_MUTED),
                Margin = new Thickness(12, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dot, 0);
            inner.Children.Add(dot);

            var lbl = new TextBlock
            {
                Text = sv.Name,
                FontSize = 14,
                Margin = new Thickness(0, 0, 12, 0),
                Foreground = isConnected ? Br(C_SUCCESS)
                           : isDefault ? Br(ParseColor("#FBBF24"))
                                         : Br(C_TXT),
                FontWeight = isConnected ? FontWeights.SemiBold : FontWeights.Normal,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 1);
            inner.Children.Add(lbl);

            frame.Child = inner;

            frame.MouseEnter += (_, _) => { if (!isSelected) frame.Background = Br(ParseColor("#222226")); };
            frame.MouseLeave += (_, _) => { frame.Background = _selectedIdx == idx ? Br(C_ACTIVE) : Brushes.Transparent; };
            frame.MouseLeftButtonDown += (_, _) => SelectServer(idx);
            _serverFrames.Add(frame);
            _serverListPanel.Children.Add(frame);
        }
        UpdateTrayMenu();
    }

    private void SelectServer(int index)
    {
        if (_editingName) CancelEditName();
        if (index < 0 || index >= _servers.Count) return;
        if (_selectedIdx >= 0 && _selectedIdx < _serverFrames.Count)
            _serverFrames[_selectedIdx].Background = Brushes.Transparent;
        _selectedIdx = index;
        _serverFrames[index].Background = Br(C_ACTIVE);

        var sv = _servers[index];
        _nameLabel.Text = sv.Name;
        UpdateHostDisplay();
        _pingLabel.Text = "";

        _serverInfoCard.BorderBrush = (_proxyProcess != null && _connectedIdx == index)
            ? Br(C_CON_BRD) : Br(C_BORDER);

        _pingBtn.IsEnabled = true;
        _connectBtn.IsEnabled = true;
        _deleteBtn.IsEnabled = true;
        _defaultBtn.IsEnabled = true;
        _defaultBtn.Foreground = sv.Name == _settings.DefaultServer ? Br(C_SUCCESS) : Br(C_DANGER);
        UpdateTrayMenu();
    }

    private void UpdateHostDisplay()
    {
        if (_selectedIdx < 0 || _selectedIdx >= _servers.Count) return;
        var sv = _servers[_selectedIdx];
        _hostLabel.Text = _hideIp ? MaskHost(sv.Host) : $"{sv.Host}:{sv.Port}";
    }

    private static string MaskHost(string host)
    {
        if (host.Length <= 4) return "***";
        return host[..2] + new string('*', host.Length - 4) + host[^2..];
    }

    private void StartEditName()
    {
        if (_selectedIdx < 0 || _editingName) return;
        _editingName = true;

        var parent = _nameLabel.Parent as Border;
        if (parent == null) return;

        _editNameBox = new TextBox
        {
            Text = _servers[_selectedIdx].Name,
            Background = Br(C_SIDEBAR),
            Foreground = Br(C_TXT),
            BorderBrush = Br(C_ACCENT),
            BorderThickness = new Thickness(1),
            CaretBrush = Br(C_TXT),
            FontSize = 16,
            FontWeight = FontWeights.Normal,
            Padding = new Thickness(4, 2, 4, 2),
            MinWidth = 150
        };
        _editNameBox.KeyDown += EditNameBox_KeyDown;
        _editNameBox.LostFocus += EditNameBox_LostFocus;

        parent.Child = _editNameBox;
        _editNameBox.Focus();
        _editNameBox.SelectAll();
    }

    private void EditNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ApplyEditName();
        else if (e.Key == Key.Escape)
            CancelEditName();
    }

    private void EditNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ApplyEditName();
    }

    private void ApplyEditName()
    {
        if (!_editingName || _selectedIdx < 0 || _editNameBox == null) return;
        string newName = _editNameBox.Text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            var oldName = _servers[_selectedIdx].Name;
            if (newName != oldName)
            {
                _servers[_selectedIdx].Name = newName;
                if (_settings.DefaultServer == oldName)
                {
                    _settings.DefaultServer = newName;
                    SaveSettings();
                }
                SaveServers();
                RefreshServerList();
                _nameLabel.Text = newName;
                _defaultBtn.Foreground = newName == _settings.DefaultServer ? Br(C_SUCCESS) : Br(C_DANGER);
            }
        }
        CancelEditName();
    }

    private void CancelEditName()
    {
        if (!_editingName) return;
        _editingName = false;
        if (_editNameBox != null)
        {
            var parent = _editNameBox.Parent as Border;
            if (parent != null)
                parent.Child = _nameLabel;
            _editNameBox = null;
        }
    }

    private void ToggleConnection()
    {
        if (_proxyProcess == null)
        {
            if (_selectedIdx < 0) return;
            _connectBtn.IsEnabled = false;
            ShowConnecting();
            Task.Run(ConnectWorker);
        }
        else
        {
            StopProxy();
        }
    }

    private void ShowToast(string title, string message, ToastType type)
    {
        if (!_settings.EnableNotifications) return;
        Dispatcher.Invoke(() =>
        {
            var toast = new ToastWindow(title, message, type);
            toast.Show();
        });
    }

    private void ConnectWorker()
    {
        var configPath = Path.Combine(Paths.DataDir, $"cfg_{Path.GetRandomFileName()}.json");
        try
        {
            var sv = _servers[_selectedIdx];
            string? tunIfName = null;
            SingBoxConfig.Generate(sv, _settings, _routingTextBacking, _proxyPort, Paths.LogFile, configPath, out tunIfName);

            var singBox = Path.Combine(Paths.Base, "core", "sing-box.exe");
            if (!File.Exists(singBox))
            {
                Dispatcher.Invoke(() => OnConnectFailed("sing-box.exe не найден в папке core"));
                return;
            }

            if (_settings.UseTunMode && !IsAdministrator())
            {
                Dispatcher.Invoke(() => OnConnectFailed("TUN-режим требует прав администратора.\nПерезапустите приложение от имени администратора."));
                return;
            }

            var psi = new ProcessStartInfo(singBox, $"run -c \"{configPath}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError  = true,
                RedirectStandardOutput = true
            };
            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.Start();

            Thread.Sleep(1500);

            if (proc.HasExited)
            {
                try
                {
                    var stderr = proc.StandardError.ReadToEnd();
                    var stdout = proc.StandardOutput.ReadToEnd();
                    var diagText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] sing-box завершился с кодом {proc.ExitCode}\n";
                    if (!string.IsNullOrWhiteSpace(stdout)) diagText += $"STDOUT:\n{stdout}\n";
                    if (!string.IsNullOrWhiteSpace(stderr)) diagText += $"STDERR:\n{stderr}\n";
                    Paths.AppendLog(Paths.LogFile, diagText);
                }
                catch { }
                Dispatcher.Invoke(() => OnConnectFailed("Процесс sing-box завершился сразу"));
                return;
            }

            if (!_settings.UseTunMode)
            {
                SystemProxy.Set(true, _proxyPort);
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"TUN-режим активирован. Виртуальный интерфейс {tunIfName} создан.\n" +
                        "Весь сетевой трафик системы направляется через прокси.\n" +
                        "Для отключения TUN-режима остановите соединение и снимите галочку в настройках.\n\n" +
                        "Этот режим обеспечивает максимальную анонимность, так как никакое приложение не может обойти прокси.",
                        "TUN Mode Active", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }

            _proxyProcess = proc;
            _connectedIdx = _selectedIdx;

            Dispatcher.Invoke(() =>
            {
                _connectBtn.Content = Tr.Get("btn_disconnect");
                _connectBtn.Background = Br(C_DANGER);
                _connectBtn.IsEnabled = true;
                _serverInfoCard.BorderBrush = Br(C_CON_BRD);
                _autoReconnectAttempts = 0;
                if (_titleStatusDot != null) _titleStatusDot.Fill = Br(C_SUCCESS);
                SmoothStatusTransition(Tr.Get("status_connected"), C_SUCCESS, null);
                UpdateTray(connected: true);
                SetWindowIcon(true);
                RefreshServerList();

                ShowToast(Tr.Get("notification_connected"), sv.Name, ToastType.Success);
            });

            StartMonitors();
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => OnConnectFailed(ex.Message));
        }
        finally
        {
            try { if (File.Exists(configPath)) File.Delete(configPath); } catch { }
        }
    }

    private void StopProxy()
    {
        _dotTimer?.Stop();
        _monitorCts.Cancel();
        _reconnectCts.Cancel();

        if (!_settings.UseTunMode)
            SystemProxy.Set(false);
        KillSwitch.Set(false);

        if (_proxyProcess != null)
        {
            try { _proxyProcess.Kill(); _proxyProcess.WaitForExit(3000); } catch { }
            _proxyProcess = null;
        }

        _connectedIdx = -1;
        _autoReconnectAttempts = 0;
        _noNetwork = false;

        SmoothStatusTransition(Tr.Get("status_disconnected"), C_MUTED);
        _connectBtn.Content = Tr.Get("btn_connect");
        _connectBtn.Background = Br(C_ACCENT);
        _connectBtn.IsEnabled = true;
        _serverInfoCard.BorderBrush = Br(C_BORDER);
        if (_titleStatusDot != null) _titleStatusDot.Fill = Br(C_MUTED);
        UpdateTray(connected: false);
        SetWindowIcon(false);
        RefreshServerList();

        ShowToast(Tr.Get("notification_disconnected"), Tr.Get("status_disconnected"), ToastType.Info);
    }

    private void OnConnectFailed(string reason = "")
    {
        _proxyProcess = null;
        SetWindowIcon(false);
        if (_settings.AutoReconnect)
        {
            ScheduleReconnect();
            return;
        }
        SmoothStatusTransition(Tr.Get("status_error"), C_DANGER);
        _connectBtn.Content = Tr.Get("btn_connect");
        _connectBtn.Background = Br(C_ACCENT);
        _connectBtn.IsEnabled = true;
        _serverInfoCard.BorderBrush = Br(C_BORDER);
        if (_titleStatusDot != null) _titleStatusDot.Fill = Br(C_DANGER);
        UpdateTray(connected: false);
    }

    private void ScheduleReconnect()
    {
        _reconnectCts.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var ct = _reconnectCts.Token;

        const int delaySeconds = 8;
        Task.Run(async () =>
        {
            for (int i = delaySeconds; i > 0; i--)
            {
                if (ct.IsCancellationRequested) return;
                var sec = i;
                Dispatcher.Invoke(() =>
                {
                    if (_proxyProcess != null) return;
                    _statusLabel.Text = $"{Tr.Get("status_reconnecting")} {sec} сек...";
                });
                await Task.Delay(1000, ct).ContinueWith(_ => { });
            }

            if (ct.IsCancellationRequested) return;

            Dispatcher.Invoke(() =>
            {
                if (_proxyProcess != null) return;
                _connectBtn.IsEnabled = false;
                ShowConnecting();
                Task.Run(ConnectWorker);
            });
        }, ct);
    }

    private bool TryNextServer()
    {
        if (_autoReconnectAttempts >= _servers.Count) { _autoReconnectAttempts = 0; return false; }
        _autoReconnectAttempts++;
        var next = (_selectedIdx + 1) % _servers.Count;
        SelectServer(next);
        SmoothStatusTransition($"{Tr.Get("status_connecting")} [{_autoReconnectAttempts}/{_servers.Count}]", C_MUTED);
        Dispatcher.BeginInvoke(ToggleConnection, DispatcherPriority.Background);
        return true;
    }

    private void StartMonitors()
    {
        _monitorCts = new CancellationTokenSource();
        var cts = _monitorCts;
        Task.Run(() => MonitorProxy(cts.Token));
        Task.Run(() => MonitorNetwork(cts.Token));
    }

    private void MonitorProxy(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Thread.Sleep(1000);
            var proc = _proxyProcess;
            if (proc == null) return;
            if (proc.HasExited)
            {
                SystemProxy.Set(false);
                _proxyProcess = null;
                Dispatcher.Invoke(OnProxyDied);
                return;
            }
        }
    }

    private void MonitorNetwork(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Thread.Sleep(5000);
            if (_proxyProcess == null) return;
            bool hasNet = CheckInternet();
            Dispatcher.Invoke(() =>
            {
                if (!hasNet && !_noNetwork)
                {
                    _noNetwork = true;
                    SmoothStatusTransition(Tr.Get("status_no_network"), C_DANGER);
                }
                else if (hasNet && _noNetwork)
                {
                    _noNetwork = false;
                    SmoothStatusTransition(Tr.Get("status_connected"), C_SUCCESS, null);
                }
            });
        }
    }

    private void OnProxyDied()
    {
        _connectedIdx = -1;
        SetWindowIcon(false);
        ShowToast(Tr.Get("notification_server_died"), Tr.Get("status_error"), ToastType.Error);
        if (_settings.AutoReconnect)
        {
            if (_settings.KillSwitch) KillSwitch.Set(true);
            ScheduleReconnect();
            return;
        }
        if (_settings.KillSwitch)
        {
            KillSwitch.Set(true);
            SmoothStatusTransition(Tr.Get("kill_switch_active"), C_DANGER);
        }
        else SmoothStatusTransition(Tr.Get("status_error"), C_DANGER);
        _connectBtn.Content = Tr.Get("btn_connect");
        _connectBtn.Background = Br(C_ACCENT);
        _serverInfoCard.BorderBrush = Br(C_BORDER);
        UpdateTray(connected: false);
        RefreshServerList();
    }

    private static bool CheckInternet()
    {
        try { using var c = new TcpClient(); c.Connect("8.8.8.8", 53); return true; }
        catch { return false; }
    }

    private DispatcherTimer? _fadeTimer;
    private double _fadeProgress = 0;
    private string _fadeTargetText = "";
    private Color _fadeTargetColor;
    private Action? _fadeCallback;

    private void SmoothStatusTransition(string newText, Color newColor, Action? callback = null)
    {
        _dotTimer?.Stop();
        _fadeTimer?.Stop();

        _fadeTargetText = newText;
        _fadeTargetColor = newColor;
        _fadeCallback = callback;
        _fadeProgress = 0;

        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        _fadeTimer.Tick += FadeTick;
        _fadeTimer.Start();
    }

    private void FadeTick(object? s, EventArgs e)
    {
        _fadeProgress += 1.0 / 20;
        if (_fadeProgress >= 1.0)
        {
            _fadeTimer?.Stop();
            _statusLabel.Text = _fadeTargetText;
            _statusLabel.Foreground = Br(_fadeTargetColor);
            _statusColor = _fadeTargetColor;
            _fadeCallback?.Invoke();
            return;
        }
        if (_fadeProgress < 0.5)
        {
            double t = _fadeProgress * 2;
            _statusLabel.Foreground = Br(LerpColor(_statusColor, C_CARD, t));
        }
        else
        {
            _statusLabel.Text = _fadeTargetText;
            double t = (_fadeProgress - 0.5) * 2;
            _statusLabel.Foreground = Br(LerpColor(C_CARD, _fadeTargetColor, t));
        }
    }

    private static Color LerpColor(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));

    private void ShowConnecting()
    {
        _dotPhase = 0;
        SmoothStatusTransition(Tr.Get("status_connecting"), C_MUTED,
            callback: () =>
            {
                _dotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _dotTimer.Tick += (_, _) =>
                {
                    _dotPhase = (_dotPhase + 1) % 4;
                    var dots = new string('.', _dotPhase);
                    _statusLabel.Text = Tr.Get("status_connecting") + dots;
                };
                _dotTimer.Start();
            });
    }

    private void StartConnectedAnimation() { }

    private void AddFromClipboard()
    {
        string raw;
        try { raw = Clipboard.GetText().Trim(); } catch { return; }
        if (string.IsNullOrEmpty(raw)) return;

        var direct = new[] { "vless://", "hysteria2://", "ss://", "trojan://" };
        if (direct.Any(s => raw.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
        {
            var sv = LinkParser.Parse(raw);
            if (sv != null) { _servers.Add(sv); SaveServers(); RefreshServerList(); ShowPanel("servers"); }
            else MessageBox.Show("Не удалось распарсить ссылку.", Tr.Get("error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else if (raw.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            Task.Run(() => FetchAndImport(raw));
        else
            MessageBox.Show("Неизвестный формат.", Tr.Get("error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ImportServersFromFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Config files (*.txt;*.conf;*.cfg;*.json)|*.txt;*.conf;*.cfg;*.json|All files (*.*)|*.*",
            Title = "Import servers from file"
        };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            string content = File.ReadAllText(dlg.FileName);
            List<ServerModel> newServers = new List<ServerModel>();

            var links = LinkParser.ExtractAll(content);
            if (links.Any())
            {
                newServers.AddRange(links);
            }
            else
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(content.Trim()));
                    links = LinkParser.ExtractAll(decoded);
                    if (links.Any())
                        newServers.AddRange(links);
                    else
                    {
                        MessageBox.Show("No valid server links found in file.", Tr.Get("error"));
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("No valid server links found in file.", Tr.Get("error"));
                    return;
                }
            }

            if (newServers.Count == 0)
            {
                MessageBox.Show("No servers found in file.", Tr.Get("error"));
                return;
            }

            foreach (var sv in newServers)
                _servers.Add(sv);
            SaveServers();
            RefreshServerList();
            ShowPanel("servers");
            MessageBox.Show($"Added {newServers.Count} server(s).", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading file: {ex.Message}", Tr.Get("error"));
        }
    }

    private async Task FetchAndImport(string url)
    {
        try
        {
            using var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            hc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            var resp = await hc.GetStringAsync(url);

            var list = LinkParser.ExtractAll(resp);
            if (!list.Any())
            {
                try { var dec = Encoding.UTF8.GetString(Convert.FromBase64String(resp.Trim())); list = LinkParser.ExtractAll(dec); }
                catch { }
            }
            if (!list.Any()) { Dispatcher.Invoke(() => MessageBox.Show("Ссылки не найдены.", Tr.Get("error"))); return; }

            Dispatcher.Invoke(() =>
            {
                foreach (var sv in list) _servers.Add(sv);
                SaveServers(); RefreshServerList(); ShowPanel("servers");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => MessageBox.Show($"Ошибка загрузки URL:\n{ex.Message}", Tr.Get("error")));
        }
    }

    private void DeleteCurrentServer()
    {
        if (_selectedIdx < 0) return;
        if (_proxyProcess != null && _connectedIdx == _selectedIdx) StopProxy();
        var name = _servers[_selectedIdx].Name;
        if (_settings.DefaultServer == name) { _settings.DefaultServer = ""; SaveSettings(); }
        _servers.RemoveAt(_selectedIdx);
        _selectedIdx = -1;
        _nameLabel.Text = Tr.Get("server_not_selected");
        _hostLabel.Text = "—";
        _pingLabel.Text = "";
        _connectBtn.IsEnabled = false;
        _pingBtn.IsEnabled = false;
        _deleteBtn.IsEnabled = false;
        _defaultBtn.IsEnabled = false;
        SaveServers();
        RefreshServerList();
    }

    private void SetCurrentDefault()
    {
        if (_selectedIdx < 0) return;
        _settings.DefaultServer = _servers[_selectedIdx].Name;
        _defaultBtn.Foreground = Br(C_SUCCESS);
        SaveSettings();
        RefreshServerList();
    }

    private async Task CheckPingAsync()
    {
        if (_selectedIdx < 0) return;
        var sv = _servers[_selectedIdx];
        _pingBtn.IsEnabled = false;
        _pingLabel.Text = Tr.Get("ping_checking");
        _pingLabel.Foreground = Br(C_MUTED);

        var ms = await Task.Run(() => MeasurePing(sv.Host, sv.Port));
        _pingBtn.IsEnabled = true;
        if (ms < 0)
        {
            _pingLabel.Text = "timeout";
            _pingLabel.Foreground = Br(C_DANGER);
        }
        else
        {
            _pingLabel.Text = $"{ms} ms";
            _pingLabel.Foreground = Br(ms < 150 ? C_SUCCESS : ms < 400 ? ParseColor("#F59E0B") : C_DANGER);
        }
    }

    private static long MeasurePing(string host, int port)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            using var tc = new TcpClient();
            tc.Connect(host, port);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
        catch { return -1; }
    }

    private void OnSplitToggle()
    {
        SaveCurrentRoutes();
        SaveSettings();
        RestartIfNeeded();
        UpdateTrayMenu();
    }

    private void UpdateSplitState()
    {
        _routesCombo.IsEnabled = true;
        if (_proxyRoutingText != null)
            _proxyRoutingText.IsEnabled = true;
        if (_proxyRoutesCombo != null)
            _proxyRoutesCombo.IsEnabled = true;
        if (_proxySaveBtn != null)
            _proxySaveBtn.IsEnabled = true;
    }

    private void RestartIfNeeded()
    {
        if (_proxyProcess != null && _selectedIdx >= 0) { StopProxy(); ToggleConnection(); }
    }

    private void CreateRoutesFile()
    {
        var dlg = new InputDialog(Tr.Get("new_routes_file"), Tr.Get("enter_name"), "new_list.txt");
        dlg.Owner = this;
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;
        var name = dlg.Result.EndsWith(".txt") ? dlg.Result : dlg.Result + ".txt";
        var path = Path.Combine(Paths.ListDir, name);
        if (File.Exists(path)) { MessageBox.Show($"Файл {name} уже существует.", Tr.Get("error")); return; }
        File.WriteAllText(path, "");
        RefreshRoutesListForCombos();
        SaveCurrentRoutes();
        _currentRoutesFile = name;
        _routesCombo.SelectedItem = name;
        if (_proxyRoutesCombo != null) _proxyRoutesCombo.SelectedItem = name;
        _routingTextBacking = "";
        if (_proxyRoutingText != null) _proxyRoutingText.Text = "";
        RestartIfNeeded();
        UpdateTrayMenu();
    }

    private void DeleteRoutesFile()
    {
        var files = (_routesCombo.ItemsSource as IEnumerable<string>)?.ToList() ?? new();
        if (files.Count <= 1) { MessageBox.Show(Tr.Get("cannot_delete_last"), Tr.Get("error")); return; }
        if (MessageBox.Show(string.Format(Tr.Get("confirm_delete_text"), _currentRoutesFile),
                Tr.Get("confirm_delete"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            File.Delete(Path.Combine(Paths.ListDir, _currentRoutesFile));
            RefreshRoutesListForCombos();
            _currentRoutesFile = files.First(f => f != _currentRoutesFile);
            _routesCombo.SelectedItem = _currentRoutesFile;
            if (_proxyRoutesCombo != null) _proxyRoutesCombo.SelectedItem = _currentRoutesFile;
            LoadRoutesFile(_currentRoutesFile);
            RestartIfNeeded();
            UpdateTrayMenu();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, Tr.Get("error")); }
    }

    private void RenameRoutesFile()
    {
        var dlg = new InputDialog(Tr.Get("rename_routes_file"), Tr.Get("enter_name"), _currentRoutesFile);
        dlg.Owner = this;
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;
        var newName = dlg.Result.EndsWith(".txt") ? dlg.Result : dlg.Result + ".txt";
        if (newName == _currentRoutesFile) return;
        try
        {
            File.Move(Path.Combine(Paths.ListDir, _currentRoutesFile), Path.Combine(Paths.ListDir, newName));
            _currentRoutesFile = newName;
            RefreshRoutesListForCombos();
            _routesCombo.SelectedItem = newName;
            if (_proxyRoutesCombo != null) _proxyRoutesCombo.SelectedItem = newName;
            UpdateTrayMenu();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, Tr.Get("error")); }
    }

    private void ImportSitesFromFile()
    {
        var dlg = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*", Title = Tr.Get("select_sites_file") };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            var content = File.ReadAllText(dlg.FileName);
            var baseName = Path.GetFileName(dlg.FileName);
            var dest = Path.Combine(Paths.ListDir, baseName);
            int i = 1;
            while (File.Exists(dest)) dest = Path.Combine(Paths.ListDir, $"{Path.GetFileNameWithoutExtension(baseName)}_{i++}.txt");
            File.WriteAllText(dest, content);
            RefreshRoutesListForCombos();
            _currentRoutesFile = Path.GetFileName(dest);
            _routesCombo.SelectedItem = _currentRoutesFile;
            if (_proxyRoutesCombo != null) _proxyRoutesCombo.SelectedItem = _currentRoutesFile;
            LoadRoutesFile(_currentRoutesFile);
            ShowPanel("servers");
            UpdateTrayMenu();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, Tr.Get("error")); }
    }

    private void RefreshSettingsUI()
    {
        _autostartChk!.IsChecked = Autostart.IsEnabled();
        _minimizeChk!.IsChecked = _settings.MinimizeOnClose;
        _killSwitchChk!.IsChecked = _settings.KillSwitch;
        _autoReconnectChk!.IsChecked = _settings.AutoReconnect;
        _debugChk!.IsChecked = _settings.DebugMode;
        _notificationsChk!.IsChecked = _settings.EnableNotifications;
        if (_tunModeChk != null) _tunModeChk.IsChecked = _settings.UseTunMode;
        _dnsTypeCombo!.SelectedIndex = _settings.DnsType switch { "doh" => 1, "dot" => 2, _ => 0 };
        _dnsAddrBox!.Text = _settings.DnsServer;
        _dnsProxyChk!.IsChecked = _settings.DnsThroughProxy;
        RefreshAppExcListUI();
        RefreshDomExcListUI();
    }

    private void SaveDns()
    {
        _settings.DnsType = _dnsTypeCombo!.SelectedIndex switch { 1 => "doh", 2 => "dot", _ => "system" };
        _settings.DnsServer = _dnsAddrBox!.Text.Trim();
        _settings.DnsThroughProxy = _dnsProxyChk!.IsChecked == true;
        SaveSettings();
        if (_proxyProcess != null) RestartIfNeeded();
        ShowPanel("servers");
    }

    private void TestDns()
    {
        if (_dnsTestLabel == null) return;
        _dnsTestLabel.Text = "...";
        Task.Run(() =>
        {
            bool ok;
            try
            {
                var addr = (_dnsAddrBox?.Text ?? "").Trim().Replace("https://", "").Replace("tls://", "").Split('/')[0].Split(':')[0];
                using var tc = new TcpClient();
                tc.Connect(addr, 443);
                ok = true;
            }
            catch { ok = false; }
            Dispatcher.Invoke(() =>
            {
                _dnsTestLabel.Text = ok ? Tr.Get("dns_test_success") : Tr.Get("dns_test_fail");
                _dnsTestLabel.Foreground = Br(ok ? C_SUCCESS : C_DANGER);
            });
        });
    }

    private void ViewLogs()
    {
        if (File.Exists(Paths.LogFile)) Process.Start(new ProcessStartInfo(Paths.LogFile) { UseShellExecute = true });
        else MessageBox.Show(Tr.Get("log_not_found"));
    }

    private void RestartApp()
    {
        CleanupAndExit(restart: true);
    }

    private void ShowAddAppMenu()
    {
        var menu = new ContextMenu();
        var byPath = new MenuItem { Header = Tr.Get("add_app_by_path") };
        byPath.Click += (_, _) => AddAppByPath();
        var byName = new MenuItem { Header = Tr.Get("add_app_by_name") };
        byName.Click += (_, _) => AddAppByName();
        menu.Items.Add(byPath);
        menu.Items.Add(byName);
        menu.IsOpen = true;
    }

    private void AddAppByPath()
    {
        var dlg = new OpenFileDialog { Filter = "Executable (*.exe)|*.exe|All (*.*)|*.*", Title = Tr.Get("select_exe") };
        if (dlg.ShowDialog(this) != true) return;
        if (_settings.AppExceptions.Any(a => a.ExType == "path" && a.Value.Equals(dlg.FileName, StringComparison.OrdinalIgnoreCase))) return;
        _settings.AppExceptions.Add(new AppException { ExType = "path", Value = dlg.FileName, Name = Path.GetFileName(dlg.FileName) });
        SaveSettings(); RefreshAppExcListUI(); RestartIfNeeded();
    }

    private void AddAppByName()
    {
        var dlg = new InputDialog(Tr.Get("add_app_by_name"), Tr.Get("enter_process"), "");
        dlg.Owner = this;
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;
        var name = dlg.Result.Trim();
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
        if (_settings.AppExceptions.Any(a => a.ExType == "name" && a.Value.Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        _settings.AppExceptions.Add(new AppException { ExType = "name", Value = name, Name = name });
        SaveSettings(); RefreshAppExcListUI(); RestartIfNeeded();
    }

    private void RemoveAppException()
    {
        if (_selAppExcIdx < 0 || _selAppExcIdx >= _settings.AppExceptions.Count) return;
        _settings.AppExceptions.RemoveAt(_selAppExcIdx);
        _selAppExcIdx = -1;
        SaveSettings(); RefreshAppExcListUI(); RestartIfNeeded();
    }

    private void RefreshAppExcListUI()
    {
        if (_appExcList == null) return;
        _appExcList.Children.Clear();
        if (!_settings.AppExceptions.Any())
        {
            _appExcList.Children.Add(MakeText(Tr.Get("no_exceptions"), 12, false, C_MUTED));
            _removeAppBtn!.IsEnabled = false; return;
        }
        for (int i = 0; i < _settings.AppExceptions.Count; i++)
        {
            var exc = _settings.AppExceptions[i];
            var idx = i;
            var row = new Border
            {
                Padding = new Thickness(6, 4, 6, 4),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Background = idx == _selAppExcIdx ? Br(C_ACTIVE) : Brushes.Transparent
            };
            var tag = exc.ExType == "path" ? "[Путь]" : "[Имя]";
            row.Child = MakeText($"{tag} {exc.Name}", 13, false, C_TXT);
            row.MouseLeftButtonDown += (_, _) =>
            {
                _selAppExcIdx = idx; _removeAppBtn!.IsEnabled = true;
                RefreshAppExcListUI();
            };
            _appExcList.Children.Add(row);
        }
    }

    private void AddDomainException()
    {
        var dlg = new InputDialog(Tr.Get("add_domain"), Tr.Get("enter_domain"), "");
        dlg.Owner = this;
        if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;
        var dom = dlg.Result.Trim();
        if (string.IsNullOrEmpty(dom)) return;

        var clean = dom.TrimStart('.');
        if (clean.Contains("..") || clean.EndsWith('.') || clean.Length == 0)
        { MessageBox.Show(Tr.Get("domain_invalid"), Tr.Get("error")); return; }

        string stored;
        bool startsWithDot = dom.StartsWith('.');
        string part = startsWithDot ? dom.Substring(1) : dom;
        try
        {
            string puny = _idn.GetAscii(part);
            stored = startsWithDot ? "." + puny : puny;
        }
        catch
        {
            stored = dom;
        }

        if (_settings.DomainExceptions.Contains(stored)) return;
        _settings.DomainExceptions.Add(stored);
        SaveSettings();
        RefreshDomExcListUI();
        RestartIfNeeded();
    }

    private void RemoveDomainException()
    {
        if (_selectedDomainException == null) return;
        _settings.DomainExceptions.Remove(_selectedDomainException);
        _selectedDomainException = null;
        SaveSettings();
        RefreshDomExcListUI();
        if (_removeDomBtn != null) _removeDomBtn.IsEnabled = false;
        RestartIfNeeded();
    }

    private void RefreshDomExcListUI()
    {
        if (_domExcList == null) return;
        _domExcList.Children.Clear();
        if (!_settings.DomainExceptions.Any())
        {
            _domExcList.Children.Add(MakeText(Tr.Get("no_exceptions"), 12, false, C_MUTED));
            if (_removeDomBtn != null) _removeDomBtn.IsEnabled = false;
            _selectedDomainException = null;
            return;
        }

        foreach (var dom in _settings.DomainExceptions)
        {
            string display = dom;
            try
            {
                bool startsWithDot = dom.StartsWith('.');
                string part = startsWithDot ? dom.Substring(1) : dom;
                string unicode = _idn.GetUnicode(part);
                display = startsWithDot ? "." + unicode : unicode;
            }
            catch { }

            var isSelected = _selectedDomainException == dom;
            var row = new Border
            {
                Padding = new Thickness(6, 4, 6, 4),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Background = isSelected ? Br(C_ACTIVE) : Brushes.Transparent
            };
            row.Child = MakeText(display, 13, false, C_TXT);
            row.MouseLeftButtonDown += (_, _) =>
            {
                _selectedDomainException = dom;
                if (_removeDomBtn != null) _removeDomBtn.IsEnabled = true;
                RefreshDomExcListUI();
            };
            _domExcList.Children.Add(row);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            Dispatcher.Invoke(() => OnHotkeyTriggered(id));
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void RegisterAllHotkeys()
    {
        if (_hwndSource == null) return;
        var hwnd = _hwndSource.Handle;
        var map = new (int id, string combo)[]
        {
            (HK_TOGGLE,  _settings.HotkeyToggle),
            (HK_ROUTING, _settings.HotkeyRouting),
            (HK_TUN,     _settings.HotkeyTun),
            (HK_EXIT,    _settings.HotkeyExit),
        };
        foreach (var (id, _) in map)
            NativeInterop.UnregisterHotKey(hwnd, id);
        foreach (var (id, combo) in map)
        {
            if (string.IsNullOrEmpty(combo)) continue;
            if (HotkeyManager.TryParse(combo, out var mods, out var vk))
            {
                if (!NativeInterop.RegisterHotKey(hwnd, id, mods, vk))
                    Dispatcher.BeginInvoke(() =>
                        MessageBox.Show($"{Tr.Get("hk_failed_reg")}\n{combo}", Tr.Get("error"),
                            MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }
    }

    private void UnregisterAllHotkeys()
    {
        if (_hwndSource == null) return;
        var hwnd = _hwndSource.Handle;
        foreach (var id in new[] { HK_TOGGLE, HK_ROUTING, HK_TUN, HK_EXIT })
            NativeInterop.UnregisterHotKey(hwnd, id);
    }

    private void OnHotkeyTriggered(int id)
    {
        switch (id)
        {
            case HK_TOGGLE:
                if (_proxyProcess == null && _selectedIdx >= 0)
                {
                    _connectBtn.IsEnabled = false;
                    ShowConnecting();
                    Task.Run(ConnectWorker);
                }
                else if (_proxyProcess != null)
                {
                    StopProxy();
                }
                break;

            case HK_ROUTING:
                _settings.SplitTunneling = !_settings.SplitTunneling;
                _splitSwitch.IsChecked = _settings.SplitTunneling;
                OnSplitToggle();
                break;

            case HK_TUN:
                if (_tunModeChk == null) break;
                var newVal = !(_tunModeChk.IsChecked ?? false);
                if (newVal && !IsAdministrator())
                {
                    MessageBox.Show(
                        "TUN-режим требует прав администратора.\nПерезапустите приложение от имени администратора.",
                        "TUN Mode", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                _tunModeChk.IsChecked = newVal;
                _settings.UseTunMode = newVal;
                SaveSettings();
                if (_proxyProcess != null) RestartIfNeeded();
                break;

            case HK_EXIT:
                CleanupAndExit();
                break;
        }
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_hkListeningBtn == null) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            _hkListeningBtn.Content = GetHkDisplay(GetHkValue(_hkListeningProp!));
            _hkListeningBtn.BorderBrush = Br(C_BORDER);
            _hkListeningBtn = null;
            _hkListeningProp = null;
            e.Handled = true;
            return;
        }

        bool ctrl  = Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl);
        bool alt   = Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt);
        bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        var combo = HotkeyManager.Build(ctrl, alt, shift, key);
        
        if (string.IsNullOrEmpty(combo))
        {
            MessageBox.Show(Tr.Get("hk_need_modifier"), Tr.Get("error"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
            return;
        }

        var curProp = _hkListeningProp!;
        foreach (var other in new[] { "Toggle", "Routing", "Tun", "Exit" })
        {
            if (other != curProp && GetHkValue(other) == combo)
            {
                MessageBox.Show(Tr.Get("hk_conflict"), Tr.Get("error"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }
        }

        SetHkValue(curProp, combo);
        _hkListeningBtn.Content = combo;
        _hkListeningBtn.BorderBrush = Br(C_BORDER);
        _hkListeningBtn = null;
        _hkListeningProp = null;
        SaveSettings();
        RegisterAllHotkeys();
        e.Handled = true;
    }

    private void StartHotkeyListening(string prop, Button btn)
    {
        if (_hkListeningBtn != null && _hkListeningBtn != btn)
        {
            _hkListeningBtn.Content = GetHkDisplay(GetHkValue(_hkListeningProp!));
            _hkListeningBtn.BorderBrush = Br(C_BORDER);
        }
        _hkListeningBtn = btn;
        _hkListeningProp = prop;
        btn.Content = Tr.Get("hk_listening");
        btn.BorderBrush = Br(C_ACCENT);
        
        this.Focus();
        Keyboard.Focus(this);
    }

    private string GetHkValue(string prop) => prop switch
    {
        "Toggle"   => _settings.HotkeyToggle,
        "Routing"  => _settings.HotkeyRouting,
        "Tun"      => _settings.HotkeyTun,
        "Exit"     => _settings.HotkeyExit,
        _          => ""
    };

    private void SetHkValue(string prop, string val)
    {
        switch (prop)
        {
            case "Toggle":   _settings.HotkeyToggle   = val; break;
            case "Routing":  _settings.HotkeyRouting  = val; break;
            case "Tun":      _settings.HotkeyTun      = val; break;
            case "Exit":     _settings.HotkeyExit     = val; break;
        }
    }

    private string GetHkDisplay(string val)
        => string.IsNullOrEmpty(val) ? Tr.Get("hk_none") : val;

    private TabItem BuildTabHotkeys()
    {
        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(MakeText(Tr.Get("hk_title"), 18, true, C_TXT, margin: new Thickness(0, 0, 0, 6)));
        sp.Children.Add(MakeText(Tr.Get("hk_desc"), 12, false, C_MUTED, margin: new Thickness(0, 0, 0, 20)));

        _hkButtons.Clear();

        var actions = new[]
        {
            ("Toggle",   Tr.Get("hk_toggle")),
            ("Routing",  Tr.Get("hk_routing")),
            ("Tun",      Tr.Get("hk_tun")),
            ("Exit",     Tr.Get("hk_exit")),
        };

        foreach (var (propName, label) in actions)
        {
            var pn = propName;

            var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = MakeText(label, 13, false, C_TXT);
            lbl.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(lbl, 0);
            row.Children.Add(lbl);

            var bindBtn = new Button
            {
                Content = GetHkDisplay(GetHkValue(pn)),
                MinWidth = 150,
                Height = 32,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Br(C_SIDEBAR),
                Foreground = Br(C_TXT),
                BorderBrush = Br(C_BORDER),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Padding = new Thickness(10, 0, 10, 0),
                Template = GetBtnTemplate(),
                Tag = new CornerRadius(6)
            };
            bindBtn.Click += (_, _) => StartHotkeyListening(pn, bindBtn);
            Grid.SetColumn(bindBtn, 1);
            row.Children.Add(bindBtn);

            var bindLblBtn = MakeOutlineBtn(Tr.Get("hk_bind"), h: 32,
                margin: new Thickness(4, 0, 0, 0), fontSize: 12, click: () => StartHotkeyListening(pn, bindBtn), w: 80);
            Grid.SetColumn(bindLblBtn, 2);
            row.Children.Add(bindLblBtn);

            var clearBtn = MakeOutlineBtn("×", fg: C_DANGER, h: 32,
                margin: new Thickness(4, 0, 0, 0), fontSize: 14, w: 34, click: () =>
                {
                    if (_hkListeningProp == pn)
                    { _hkListeningBtn = null; _hkListeningProp = null; }
                    SetHkValue(pn, "");
                    bindBtn.Content = Tr.Get("hk_none");
                    bindBtn.BorderBrush = Br(C_BORDER);
                    SaveSettings();
                    RegisterAllHotkeys();
                });
            Grid.SetColumn(clearBtn, 3);
            row.Children.Add(clearBtn);

            sp.Children.Add(row);
            _hkButtons[propName] = bindBtn;
        }

        var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return new TabItem { Header = Tr.Get("tab_hotkeys"), Content = scroll, Foreground = Br(C_TXT) };
    }

    private void InitTray()
    {
        Forms.ToolStripManager.Renderer = new Forms.ToolStripProfessionalRenderer(new LightProfessionalColorTable());

        _tray = new Forms.NotifyIcon
        {
            Text = "Taa Proxy",
            Visible = true
        };
        try
        {
            var ico = Paths.Resource("pic\\ico.ico");
            if (File.Exists(ico)) _tray.Icon = new System.Drawing.Icon(ico);
            else _tray.Icon = System.Drawing.SystemIcons.Application;
        }
        catch { _tray.Icon = System.Drawing.SystemIcons.Application; }

        _tray.DoubleClick += (_, _) => ShowWindow();
        UpdateTrayMenu();
    }

    private void UpdateTray(bool connected)
    {
        if (_tray == null) return;
        try
        {
            var icoName = connected ? "icoon.ico" : "ico.ico";
            var icoPath = Paths.Resource($"pic\\{icoName}");
            if (File.Exists(icoPath)) _tray.Icon = new System.Drawing.Icon(icoPath);
        }
        catch { }
        UpdateTrayMenu();
    }

    private void UpdateTrayMenu()
    {
        if (_tray == null) return;
        var menu = new Forms.ContextMenuStrip();

        var status = _proxyProcess != null ? "● Подключено" : "○ Отключено";
        menu.Items.Add(status).Enabled = false;
        menu.Items.Add(new Forms.ToolStripSeparator());

        if (_proxyProcess == null && _selectedIdx >= 0)
        {
            var ci = menu.Items.Add(Tr.Get("btn_connect"));
            ci.Font = new System.Drawing.Font(ci.Font, System.Drawing.FontStyle.Bold);
            ci.Click += (_, _) => Dispatcher.Invoke(ToggleConnection);
        }
        if (_proxyProcess != null)
        {
            var di = menu.Items.Add(Tr.Get("btn_disconnect"));
            di.Click += (_, _) => Dispatcher.Invoke(StopProxy);
        }
        menu.Items.Add(new Forms.ToolStripSeparator());

        var routingItem = new Forms.ToolStripMenuItem(Tr.Get("split_tunneling"));
        routingItem.CheckOnClick = true;
        routingItem.Checked = _settings.SplitTunneling;
        routingItem.Click += (_, _) =>
        {
            _settings.SplitTunneling = !_settings.SplitTunneling;
            _splitSwitch.IsChecked = _settings.SplitTunneling;
            OnSplitToggle();
        };
        menu.Items.Add(routingItem);

        var routesSubMenu = new Forms.ToolStripMenuItem("Список маршрутизации");
        var files = Directory.Exists(Paths.ListDir)
            ? Directory.GetFiles(Paths.ListDir, "*.txt").Select(Path.GetFileName).OfType<string>().OrderBy(x => x).ToList()
            : new List<string>();
        foreach (var file in files)
        {
            var item = new Forms.ToolStripMenuItem(file.Length > 35 ? file[..35] + "…" : file);
            item.Checked = file == _currentRoutesFile;
            item.Click += (_, _) =>
            {
                if (file != _currentRoutesFile)
                {
                    SaveCurrentRoutes();
                    _currentRoutesFile = file;
                    LoadRoutesFile(file);
                    if (_routesCombo.SelectedItem as string != file)
                        _routesCombo.SelectedItem = file;
                    if (_proxyRoutesCombo != null && _proxyRoutesCombo.SelectedItem as string != file)
                        _proxyRoutesCombo.SelectedItem = file;
                    RestartIfNeeded();
                    UpdateTrayMenu();
                }
            };
            routesSubMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(routesSubMenu);
        menu.Items.Add(new Forms.ToolStripSeparator());

        var sub = new Forms.ToolStripMenuItem(Tr.Get("app_name"));
        for (int i = 0; i < _servers.Count; i++)
        {
            var idx = i; var name = _servers[i].Name;
            var item = new Forms.ToolStripMenuItem(name.Length > 35 ? name[..35] + "…" : name);
            item.Checked = idx == _selectedIdx;
            item.Click += (_, _) => Dispatcher.Invoke(() => SelectServer(idx));
            sub.DropDownItems.Add(item);
        }
        menu.Items.Add(sub);
        menu.Items.Add(new Forms.ToolStripSeparator());

        var open = menu.Items.Add(Tr.Get("tray_open"));
        open.Click += (_, _) => Dispatcher.Invoke(ShowWindow);
        open.Font = new System.Drawing.Font(open.Font, System.Drawing.FontStyle.Bold);
        menu.Items.Add(new Forms.ToolStripSeparator());

        var quit = menu.Items.Add(Tr.Get("tray_exit"));
        quit.Click += (_, _) => Dispatcher.Invoke(() => CleanupAndExit());

        _tray.ContextMenuStrip = menu;
    }

    private void OnClosing(object? s, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        if (_settings.MinimizeOnClose) Hide();
        else CleanupAndExit();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
        RegisterAllHotkeys();
        SetWindowIcon(_proxyProcess != null);
    }

    private void CleanupAndExit(bool restart = false)
    {
        _dotTimer?.Stop();
        _fadeTimer?.Stop();
        _monitorCts.Cancel();
        SaveCurrentRoutes();
        SaveSettings();
        StopProxy();
        UnregisterAllHotkeys();
        _hwndSource?.RemoveHook(HwndHook);
        _tray?.Dispose();
        if (restart)
        {
            Program.ReleaseMutex();
            Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule!.FileName!) { UseShellExecute = true });
        }
        Application.Current.Shutdown();
    }

    private void CenterIfDefault()
    {
        if (_settings.WindowLeft < 0)
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }
    }
}

internal enum ToastType { Info, Success, Error }

internal class ToastWindow : Window
{
    private static readonly Color C_BG = (Color)ColorConverter.ConvertFromString("#1C1C1F");
    private static readonly Color C_BORDER = (Color)ColorConverter.ConvertFromString("#3F3F46");
    private static readonly Color C_TXT = (Color)ColorConverter.ConvertFromString("#F8FAFC");
    private static readonly Color C_MUTED = (Color)ColorConverter.ConvertFromString("#A1A1AA");
    private static readonly Color C_SUCCESS = (Color)ColorConverter.ConvertFromString("#10B981");
    private static readonly Color C_INFO = (Color)ColorConverter.ConvertFromString("#3B82F6");
    private static readonly Color C_ERROR = (Color)ColorConverter.ConvertFromString("#EF4444");

    private static SolidColorBrush Br(Color c) => new(c);

    public ToastWindow(string title, string message, ToastType type)
    {
        Width = 300;
        Height = 90;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;

        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width - 20;
        Top = screen.Bottom - Height - 20;

        var border = new Border
        {
            Background = Br(C_BG),
            BorderBrush = Br(C_BORDER),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Opacity = 0
        };

        var grid = new Grid { Margin = new Thickness(16, 12, 16, 12) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        var color = type switch { ToastType.Success => C_SUCCESS, ToastType.Error => C_ERROR, _ => C_INFO };
        var dot = new Ellipse { Width = 10, Height = 10, Fill = Br(color), Margin = new Thickness(0, 0, 8, 0) };
        var titleBlock = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Br(C_TXT) };
        header.Children.Add(dot);
        header.Children.Add(titleBlock);
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var msgBlock = new TextBlock { Text = message, FontSize = 12, Foreground = Br(C_MUTED), TextWrapping = TextWrapping.Wrap };
        Grid.SetRow(msgBlock, 1);
        grid.Children.Add(msgBlock);

        border.Child = grid;
        Content = border;

        Loaded += async (_, _) =>
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            border.BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(4000);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (_, _) => Close();
            border.BeginAnimation(OpacityProperty, fadeOut);
        };
    }
}

internal class LightProfessionalColorTable : Forms.ProfessionalColorTable
{
    public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(200, 200, 200);
    public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(230, 230, 230);
    public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(240, 240, 240);
    public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(240, 240, 240);
    public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(220, 220, 220);
    public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(220, 220, 220);
    public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(180, 180, 180);
    public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.White;
    public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.White;
    public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.White;
    public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.White;
    public override System.Drawing.Color ToolStripBorder => System.Drawing.Color.FromArgb(180, 180, 180);
    public override System.Drawing.Color MenuStripGradientBegin => System.Drawing.Color.White;
    public override System.Drawing.Color MenuStripGradientEnd => System.Drawing.Color.White;
    public override System.Drawing.Color CheckBackground => System.Drawing.Color.FromArgb(220, 220, 220);
    public override System.Drawing.Color CheckPressedBackground => System.Drawing.Color.FromArgb(200, 200, 200);
    public override System.Drawing.Color CheckSelectedBackground => System.Drawing.Color.FromArgb(200, 200, 200);
    public override System.Drawing.Color ButtonSelectedBorder => System.Drawing.Color.FromArgb(180, 180, 180);
}

internal class InputDialog : Window
{
    private readonly TextBox _input;
    public string Result => _input.Text;

    private static readonly Color C_BG = (Color)ColorConverter.ConvertFromString("#18181B");
    private static readonly Color C_HEAD = (Color)ColorConverter.ConvertFromString("#111113");
    private static readonly Color C_BORD = (Color)ColorConverter.ConvertFromString("#27272A");
    private static readonly Color C_TXT = (Color)ColorConverter.ConvertFromString("#F8FAFC");
    private static readonly Color C_MUTED = (Color)ColorConverter.ConvertFromString("#A1A1AA");
    private static readonly Color C_ACC = (Color)ColorConverter.ConvertFromString("#4F46E5");

    private static SolidColorBrush Br(Color c) => new(c);

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Title = title;
        Width = 420; Height = 185;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Br(C_BG);
        WindowStyle = WindowStyle.None;
        FontFamily = new FontFamily("Segoe UI");

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 36,
            ResizeBorderThickness = new Thickness(0),
            UseAeroCaptionButtons = false,
            GlassFrameThickness = new Thickness(0)
        });

        var titleBar = new Border
        {
            Background = Br(C_HEAD),
            Height = 36,
            BorderBrush = Br(C_BORD),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var tbGrid = new Grid();
        tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Br(C_MUTED),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0)
        };
        Grid.SetColumn(titleText, 0);
        tbGrid.Children.Add(titleText);

        var closeBtn = new Button
        {
            Content = "⛌",
            Width = 40,
            Height = 36,
            Background = Brushes.Transparent,
            Foreground = Br(C_MUTED),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            FontSize = 11
        };
        closeBtn.MouseEnter += (_, _) => { closeBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); closeBtn.Foreground = Brushes.White; };
        closeBtn.MouseLeave += (_, _) => { closeBtn.Background = Brushes.Transparent; closeBtn.Foreground = Br(C_MUTED); };
        closeBtn.Click += (_, _) => { DialogResult = false; };
        WindowChrome.SetIsHitTestVisibleInChrome(closeBtn, true);
        Grid.SetColumn(closeBtn, 1);
        tbGrid.Children.Add(closeBtn);
        titleBar.Child = tbGrid;

        var body = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };
        body.Children.Add(new TextBlock
        {
            Text = prompt,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = Br(C_MUTED),
            TextWrapping = TextWrapping.Wrap
        });

        _input = new TextBox
        {
            Text = defaultValue,
            FontSize = 14,
            Height = 36,
            Background = Br((Color)ColorConverter.ConvertFromString("#09090B")),
            Foreground = Br(C_TXT),
            BorderBrush = Br(C_ACC),
            BorderThickness = new Thickness(1),
            CaretBrush = Br(C_TXT),
            Padding = new Thickness(9, 0, 9, 0),
            Margin = new Thickness(0, 0, 0, 14),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _input.KeyDown += (_, e) => { if (e.Key == Key.Enter) DialogResult = true; if (e.Key == Key.Escape) DialogResult = false; };

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = MakeDialogBtn("OK", C_ACC, true);
        var cancelBtn = MakeDialogBtn("Отмена", (Color)ColorConverter.ConvertFromString("#27272A"), false);
        okBtn.Click += (_, _) => { DialogResult = true; };
        cancelBtn.Click += (_, _) => { DialogResult = false; };
        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);

        body.Children.Add(_input);
        body.Children.Add(btnRow);

        var outer = new DockPanel();
        DockPanel.SetDock(titleBar, Dock.Top);
        outer.Children.Add(titleBar);
        outer.Children.Add(body);
        Content = outer;

        Loaded += (_, _) => { _input.Focus(); _input.SelectAll(); };
    }

    private static Button MakeDialogBtn(string text, Color bg, bool isOk)
    {
        var btn = new Button
        {
            Content = text,
            Width = 80,
            Height = 32,
            Margin = isOk ? new Thickness(0, 0, 8, 0) : new Thickness(0),
            Background = new SolidColorBrush(bg),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(isOk ? 0 : 1),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")),
            Cursor = Cursors.Hand,
            FontSize = 13
        };
        const string x = @"<ControlTemplate TargetType='Button'
            xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
          <Border x:Name='bd' Background='{TemplateBinding Background}'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  CornerRadius='6'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
        </ControlTemplate>";
        btn.Template = (ControlTemplate)XamlReader.Parse(x);
        return btn;
    }
}
