using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace auto_clicker
{
    public partial class MainForm : Form
    {
        public MainForm() => InitializeComponent();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            indicators = new CheckBox[]
            {
                checkBox1, checkBox2, checkBox3, checkBox4, checkBox5,  
            };
            IterateControlTree(this, attachMouseDown, null);
            buttonClear.Click += onClear;
            checkBoxAutoClick.CheckedChanged += onAutoClick;
        }

        private void onClear(object sender, EventArgs e)
        {
            richTextBox.Clear();
            foreach (var indicator in indicators)
            {
                indicator.Checked = false;
            }
        }

        CheckBox[] indicators;

        private bool attachMouseDown(Control control, object args)
        {
            control.MouseDown+= onAnyMouseDown;
            return true;
        }

        private void onAnyMouseDown(object sender, EventArgs e)
        {
            var control = (Control)sender;
            if(_sslimAutoClick.Wait(0))
            {
                // No pending auto click detected
                try
                {
                    richTextBox.AppendTextEx($"{MousePosition}");
                }
                finally
                {
                    _sslimAutoClick.Release();
                }
            }
            else
            {
                // The settle delay for the auto click hasn't expired yet.
                richTextBox.AppendTextEx($"{control.Location}", color: Color.Blue);
            }
        }

        const int SETTLE = 100;
        SemaphoreSlim _sslimAutoClick = new SemaphoreSlim(1, 1);
        /// <summary>
        /// Responds to the button by auto-clicking 5 times.
        /// </summary>
        private async void onAutoClick(object sender, EventArgs e)
        {
            if (checkBoxAutoClick.Checked)
            {
                checkBoxAutoClick.Enabled = false;
                foreach (var indicator in indicators)
                {
                    await _sslimAutoClick.WaitAsync();
                    autoClick(indicator);
                    // Don't await here. It's for the benefit of clients.
                    Task
                        .Delay(SETTLE)
                        .GetAwaiter()
                        .OnCompleted(() =>_sslimAutoClick.Release());

                    // Interval between auto clicks.
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                checkBoxAutoClick.Enabled = true;
                checkBoxAutoClick.Checked = false;
            }
        }
        public void autoClick(Control control)
        {
            autoClick(new Point 
            { 
                X =  control.Location.X + control.Width / 2,
                Y =  control.Location.Y + control.Height / 2,
            });
        }
        public void autoClick(Point clientPoint)
        {
            var screen = PointToScreen(clientPoint);
            Cursor.Position = new Point(screen.X, screen.Y);

            var inputMouseDown = new INPUT { Type = Win32Consts.INPUT_MOUSE };
            inputMouseDown.Data.Mouse.Flags = (uint)MOUSEEVENTF.LEFTDOWN;
            inputMouseDown.Data.Mouse.ExtraInfo = (IntPtr)MOUSEEXTRA.AutoClick;

            var inputMouseUp = new INPUT { Type = Win32Consts.INPUT_MOUSE };
            inputMouseUp.Data.Mouse.Flags = (uint)MOUSEEVENTF.LEFTUP;

            var inputs = new INPUT[]
            {
                inputMouseDown,
                inputMouseUp,
            };
            if (0 == SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))))
            {
                Debug.Assert(false, "Error: SendInput has failed.");
            }
        }

        [Flags]
        enum MOUSEEXTRA : uint{ AutoClick = 0x40000000, }

#region P I N V O K E
        [Flags]
        internal enum MOUSEEVENTF : uint
        {
            ABSOLUTE = 0x8000,
            HWHEEL = 0x01000,
            MOVE = 0x0001,
            MOVE_NOCOALESCE = 0x2000,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            VIRTUALDESK = 0x4000,
            WHEEL = 0x0800,
            XDOWN = 0x0080,
            XUP = 0x0100
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEHOOKSTRUCT
        {
            public Point pt;
            public IntPtr hwnd;
            public uint wHitTestCode;
            public IntPtr dwExtraInfo;
        }
        // https://www.youtube.com/watch?v=Lt3H5swUl8Q

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }
        public class Win32Consts
        {
            // For use with the INPUT struct, see SendInput for an example
            public const int INPUT_MOUSE = 0;
            public const int INPUT_KEYBOARD = 1;
            public const int INPUT_HARDWARE = 2;
        }

        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }
#endregion P I N V O K E

        internal delegate bool FxControlDlgt(Control control, Object args);
        internal bool IterateControlTree(Control control, FxControlDlgt fx, Object args)
        {
            if (control == null)
            {
                control = this;
            }
            if (!fx(control, args))
            {
                return false;
            }
            foreach (Control child in control.Controls)
            {
                if (!IterateControlTree(child, fx, args))
                {
                    return false;
                }
            }
            return true;
        }
    }
    static class Extensions
    {
        public static void AppendTextEx(this RichTextBox @this, string text = "", bool newLine = true, Color? color = null)
        {
            @this.Select(@this.TextLength, 0);
            if (newLine) text = $"{text}{Environment.NewLine}";
            if (color == null)
            {
                @this.AppendText(text);
            }
            else
            {
                var colorB4 = @this.ForeColor;
                @this.SelectionColor = (Color)color;
                @this.AppendText(text);
                @this.SelectionColor = colorB4;
            }
            @this.Refresh();
        }
    }
}
