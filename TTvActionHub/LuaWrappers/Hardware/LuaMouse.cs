using Lua;
using WIWrapper;
using WIWrapper.BackEnd.NativeInputs.Mouse;
using TTvActionHub.BackEnds.Abstractions;

namespace TTvActionHub.LuaWrappers.Hardware;

[LuaObject]
public partial class LuaMouse
{
    private static readonly InputWrapper wrapper = new();
    private static readonly object _inputLock = new();

    [LuaMember]
    public static void PressButton(int button)
    {
        lock (_inputLock)
        {
            wrapper.MouseButton((MouseButton)button).SendInputs();
        }
    }

    [LuaMember]
    public static void ReleaseButton(int button)
    {
        lock (_inputLock)
        {
            wrapper.MouseButton((MouseButton)button, false).SendInputs();
        }
    }

    [LuaMember]
    public static void XPressButton(int xid)
    {
        lock (_inputLock)
        {
            wrapper.MouseXButton(xid).SendInputs();
        }
    }

    [LuaMember]
    public static void XReleaseButton(int xid)
    {
        lock (_inputLock)
        {
            wrapper.MouseXButton(xid, false).SendInputs();
        }
    }

    [LuaMember]
    public static void HoldButton(int button, int duration = 1000)
    {
        if (duration < 200)
        {
            ClickButton(button);
            return;
        }
        var durStep = duration / 100;
        for (var totalDuration = 0; totalDuration < duration; totalDuration += durStep)
        {
            PressButton(button);
            Thread.Sleep(durStep);
            PressButton(button);
        }
    }

    [LuaMember]
    public static void XHoldButton(int button, int duration = 1000)
    {
        if (duration < 200)
        {
            XClickButton(button);
            return;
        }
        var durStep = duration / 100;
        for (var totalDuration = 0; totalDuration < duration; totalDuration += durStep)
        {
            XPressButton(button);
            Thread.Sleep(durStep);
            XPressButton(button);
        }
    }

    [LuaMember]
    public static void ClickButton(int button)
    {
        PressButton(button);
        Thread.Sleep(100);
        ReleaseButton(button);
    }

    [LuaMember]
    public static void XClickButton(int button)
    {
        XPressButton(button);
        Thread.Sleep(100);
        XReleaseButton(button);
    }

    [LuaMember]
    public static void HScroll(uint distance)
    {
        lock (_inputLock)
        {
            wrapper.WheelScroll(distance).SendInputs();
        }
    }

    [LuaMember]
    public static void VScroll(uint distance)
    {
        lock (_inputLock)
        {
            wrapper.WheelScroll(distance, true).SendInputs();
        }
    }

    [LuaMember]
    public static void SetPosition(int x, int y)
    {
        lock (_inputLock)
        {
            wrapper.MouseSet(x, y).SendInputs();
        }
    }

    [LuaMember]
    public static void Move(int dx, int dy)
    {
        lock (_inputLock)
        {
            wrapper.MouseMove(dx, dy).SendInputs();
        }
    }

    [LuaMember]
    public static uint Button(string button) =>  button switch
    {
        "Left" => 0, "Middle" => 1, "Right" => 2,
        _ => throw new ArgumentException("Undefined button"),
    };
}