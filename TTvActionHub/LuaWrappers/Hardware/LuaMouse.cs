using Lua;
using TTvActionHub.BackEnds.Abstractions;
using TTvActionHub.BackEnds.HardwareWrapper;

namespace TTvActionHub.LuaWrappers.Hardware;

[LuaObject]
public partial class LuaMouse
{
    [LuaMember]
    public static void PressButton(int button)
    {
        var input = InputWrapper.ConstructMouseButtonDown((NativeInputs.MouseButton)button);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static void ReleaseButton(int button)
    {
        var input = InputWrapper.ConstructMouseButtonUp((NativeInputs.MouseButton)button);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static void XPressButton(int xid)
    {
        var input = InputWrapper.ConstructXMouseButtonDown(xid);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static void XReleaseButton(int xid)
    {
        var input = InputWrapper.ConstructXMouseButtonUp(xid);
        InputWrapper.DispatchInput([input]);
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
    public static void HScroll(int distance)
    {
        var input = InputWrapper.ConstructHWheelScroll(distance);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static void VScroll(int distance)
    {
        var input = InputWrapper.ConstructVWheelScroll(distance);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static void SetPosition(int x, int y)
    {
        var input = InputWrapper.ConstructAbsoluteMouseMove(x, y);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static void Move(int dx, int dy)
    {
        var input = InputWrapper.ConstructRelativeMouseMove(dx, dy);
        InputWrapper.DispatchInput([input]);
    }

    [LuaMember]
    public static int Button(string button) =>  button switch
    {
        "Left" => 0, "Middle" => 1, "Right" => 2,
        _ => throw new ArgumentException("Undefined button"),
    };
}