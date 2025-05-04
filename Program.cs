/* 
sources helping make this 

https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate
https://cboard.cprogramming.com/cplusplus-programming/112970-getasynckeystate-key-pressed.html
https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ everything in the list to the left
https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmsetwindowattribute
https://learn.microsoft.com/en-us/windows/win32/dwm/setwindowcompositionattribute
and my beloved stackoverflow but that was to many so i wont list them

and ofc @lukehjo he is the one who inspired me to make this app and helped with the above
*/

using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace KeyOverlay
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum DwmState
        {
            DISABLED = 0,
            ENABLE_GRADIENT = 1,
            ENABLE_TRANSPARENTGRADIENT = 2,
            ENABLE_BLURBEHIND = 3,
            ENABLE_ACRYLICBLURBEHIND = 4,
            ENABLE_HOSTBACKDROP = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DwmPolicy
        {
            public DwmState DwmAccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private Label? keyLabel;
        private System.Windows.Forms.Timer? updateTimer;

        //silliest way to construct
        public Form1()
        {
            InitializeComponent();
            SetupOverlay();
        }

        private void SetupOverlay()
        {
            //Icon = new Icon("icon.ico");
            DoubleBuffered = true; //dont know if needed yet
            ShowInTaskbar = false;

            Form form1 = new()
            {
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowInTaskbar = false
            };

            Owner = form1;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(Screen.PrimaryScreen!.WorkingArea.Width - 300, Screen.PrimaryScreen!.WorkingArea.Height - 100);
            Size = new Size(300, 100);
            BackColor = Color.Black;
            TransparencyKey = BackColor;
            Opacity = 0.7;

            GraphicsPath path = new();
            int radius = 20;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(Width - radius - 1, 0, radius, radius, 270, 90);
            path.AddArc(Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
            path.AddArc(0, Height - radius - 1, radius, radius, 90, 90);
            path.CloseFigure();
            Region = new Region(path);

            keyLabel = new Label()
            {
                ForeColor = Color.White,
                Font = new Font("Consolas", 20),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "",
                BackColor = Color.Transparent
            };

            Controls.Add(keyLabel);

            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 30;
            updateTimer.Tick += UpdateKeys;
            updateTimer.Start();

            EnableBlur();
        }

        private void EnableBlur()
        {
            var accent = new DwmPolicy
            {
                DwmAccentState = DwmState.ENABLE_BLURBEHIND
            };

            int sizeOfAccent = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(sizeOfAccent);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = sizeOfAccent,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);

            // rounded corners
            const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
            int preference = 2;
            DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }

        //kept the same function will change it shows in order
        private void UpdateKeys(object? sender, EventArgs e)
        {
            if (keyLabel == null) return;

            StringBuilder pressedKeys = new();
            for (int keyCode = 0; keyCode <= 255; keyCode++)
            {
                if ((GetAsyncKeyState(keyCode) & 0x8000) != 0)
                {
                    string keyName = GetKeyName(keyCode);
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        if (pressedKeys.Length > 0)
                            pressedKeys.Append(" + ");
                        pressedKeys.Append(keyName);
                    }
                }
            }

            string newText = pressedKeys.ToString();
            if (keyLabel.Text != newText)
            {
                keyLabel.Text = newText;
            }
        }

        private static readonly Dictionary<int, string> keyCache = new();

        private static string GetKeyName(int keyCode)
        {
            if (keyCache.TryGetValue(keyCode, out var name))
                return name;
            name = keyCode switch
            {
                0x01 => "LMB",
                0x02 => "RMB",
                0x09 => "Tab",
                0x20 => "Space",
                0x0D => "Enter",
                0x1B => "Esc",
                0x10 => "Shift",
                0x11 => "Ctrl",
                0x12 => "Alt",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x08 => "BackSpace",
                0x5B => "Win",
                0x70 => "F1",
                0x71 => "F2",
                0x72 => "F3",
                0x73 => "F4",
                0x74 => "F5",
                0x75 => "F6",
                0x76 => "F7",
                0x77 => "F8",
                0x78 => "F9",
                0x79 => "F10",
                0x7A => "F11",
                >= 0x30 and <= 0x39 => ((char)keyCode).ToString(),
                >= 0x41 and <= 0x5A => ((char)keyCode).ToString(),
                _ => "",
            };
            keyCache[keyCode] = name;
            return name;
        }
    }
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}