_This answer consolidates and incorporates information and ideas from other SO posts [here](https://stackoverflow.com/a/10355905/5438626) and [here](https://stackoverflow.com/q/22744531/5438626) and [here](https://stackoverflow.com/q/2416748/5438626)._
***

Your [question](https://stackoverflow.com/q/74721398/5438626) is how to distinguish a physical mouse click from a virtual one. I see that in your code you're using a mouse hook and one way "close the loop" is by examining the `dwExtraInfo` flag in the callback per Gabriel Luci's exellent suggestion'. But what I set out do do is find a threadsafe approach that doesn't rely on a hook to detect auto clicks so I ended up discarding the mouse hook for my testing. And I tried several things, but what I found was most reliable in my testing is to essentially set a ~100 ms watchdog timer using a thread synchronization object (like the easy-to-use `SemaphoreSlim`). This semaphore can be tested by whatever target ultimately consumes the click. All it needs to do is test whether the WDT is has expired by calling Wait(0) on the semaphore and looking at the `bool` return value. 

In the first of two tests, I checked the AutoClick button and let it run. As expected, the physical click shows up in black and the auto clicks show up in blue. The indicators all light up as expected.

![Screenshot](https://github.com/IVSoftware/auto-clicker-demo/blob/master/auto-clicker/Screenshots/simple.png)

Then to test it out, while the 5x autoclick is running, I did a few manual clicks to make sure they intersperse and print in black.

![Screenshot](https://github.com/IVSoftware/auto-clicker-demo/blob/master/auto-clicker/Screenshots/interspersed.png)

If you'd like to play with it, you can [clone](https://github.com/IVSoftware/auto-clicker-demo.git) the full sample code on GitHub.