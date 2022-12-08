_This answer consolidates and incorporates info and ideas from other SO posts [here](https://stackoverflow.com/a/10355905/5438626) and [here](https://stackoverflow.com/q/2416748/5438626)._
***

Your [question](https://stackoverflow.com/q/74721398/5438626) is how to distinguish a physical mouse click from a virtual one. I see that in your code you're using a mouse hook and one way "close the loop" is by examining the `dwExtraInfo` flag in the callback per Gabriel Luci's exellent suggestion. But what I set out to do is find a threadsafe approach that doesn't rely on a hook to detect auto clicks so I ended up discarding the mouse hook for my testing. And I tried several things, but what I found was most reliable in my experience is to essentially set a ~100 ms watchdog timer using a thread synchronization object (like the easy-to-use `SemaphoreSlim`). This semaphore can be tested by whatever target ultimately consumes the click. All it needs to do is test whether the WDT is has expired by calling Wait(0) on the semaphore and looking at the `bool` return value. 

In the first of two tests, I checked the AutoClick button and let it run. As expected, the physical click shows up in black and the auto clicks show up in blue. The indicators all light up as expected.

![Screenshot](https://github.com/IVSoftware/auto-clicker-demo/blob/master/auto-clicker/Screenshots/simple.png)
***

For the autoClick methods, I used `SendInput` since `mouse_event` is obsolete (see Hans Passant comment on this [post](https://stackoverflow.com/q/22744531/5438626).

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
        // Go ahead and decorate with a flag as Gabriel Luci suggests.
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

The handler for the `Auto Click 5` checkbox positions and clicks the mouse to light up 5 "indicator" check boxes.

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

As a more rigorous test, I repeated this but while the 5x autoclick is running I did a few manual clicks to make sure they intersperse and print in black. Again, it worked as expected.

![Screenshot](https://github.com/IVSoftware/auto-clicker-demo/blob/master/auto-clicker/Screenshots/interspersed.png)

If you'd like to browse the full code or experiment with it, the [full sample code](https://github.com/IVSoftware/auto-clicker-demo.git) is on GitHub.