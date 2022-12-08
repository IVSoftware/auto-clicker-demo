_This answer consolidates and incorporates information from other SO posts [here](https://stackoverflow.com/a/10355905/5438626) and [here](https://stackoverflow.com/q/22744531/5438626) and [here](https://stackoverflow.com/q/2416748/5438626)._


Your question is how to distinguish a physical click from a programmatic one and I agree with the idea of a flag but would like to see something more threadsafe than a `bool` to implement it. The `SemaphoreSlim` is an easy-to-use synchronization object and is something I use a lot in my own projects to solve problems of this nature. I enjoyed some preliminary success using the this 5x auto clicker that starts when the check box is set.

    /// <summary>
    /// Responds to the button by auto-clicking 5 times.
    /// </summary>
    private async void onAutoClick(object sender, EventArgs e)
    {
        if (checkBoxAutoClick.Checked)
        {
            checkBoxAutoClick.Enabled = false;
            for (int i = 1; i <= 5; i++)
            {
                await _sslimMouseEvent.WaitAsync(millisecondsTimeout: 100);
                richTextBox.AppendTextEx($"{i}", color: Color.Red);

                var client = new Point(50, 50 + (i * 50));
                clickOnPoint(client);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            checkBoxAutoClick.Enabled = true;
            checkBoxAutoClick.Checked = false;
        }
    }   

    public void autoClick(Point clientPoint)
    {
        var screen = PointToScreen(clientPoint);
        Cursor.Position = new Point(screen.X, screen.Y);

        var inputMouseDown = new INPUT { Type = Win32Consts.INPUT_MOUSE };
        inputMouseDown.Data.Mouse.Flags = (uint)MOUSEEVENTF.LEFTDOWN;

        var inputMouseUp = new INPUT { Type = Win32Consts.INPUT_MOUSE };
        inputMouseUp.Data.Mouse.Flags = (uint)MOUSEEVENTF.LEFTUP;

        var inputs = new INPUT[]
        {
            inputMouseDown,
            inputMouseUp,
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

 The idea here is to `set` the semaphore before an auto-click and not release it until the hook event is actually received. The semaphore can be tested in the callback using Wait(0) and if `true` (the semaphore _can be entered_) it indicates a physical click and we'll print the position in black'. Otherwise it's considered an autoclick and the position will print in blue.

    private IntPtr callback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            switch ((int)wParam)
            {
                case WM_LBUTTONDOWN:
                    onWmLButtonDown(Marshal.PtrToStructure<MOUSEHOOKSTRUCT>(lParam));
                    break;
                case WM_LBUTTONUP:
                    // N O O P
                    break;
                default:
                    break;
            }
        }
        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private void onWmLButtonDown(MOUSEHOOKSTRUCT mouseInfo)
    {
        try
        {
            if (_sslimMouseEvent.Wait(0))
            {
                richTextBox.AppendTextEx($"{PointToClient(mouseInfo.pt)}");
            }
            else
            {
                richTextBox.AppendTextEx($"{PointToClient(mouseInfo.pt)}", color: Color.Blue);
            }
        }
        finally
        {
            _sslimMouseEvent.Release();
        }
    }

When the checkbox is clicked, that mousedown registers in black followed by 5 autoclicks in blue.

![Screenshot](https://github.com/IVSoftware/auto-clicker/blob/master/auto-clicker/Screenshots/simple.png)

Then to test it out, while the 5x autoclick is running, I did a few manual clicks to make sure they intersperse and print in black.

![Screenshot](https://github.com/IVSoftware/auto-clicker/blob/master/auto-clicker/Screenshots/interspersed.png)

There's too much code to show here but you can [browse](https://github.com/IVSoftware/auto-clicker.git) the full sample code on GitHub. 