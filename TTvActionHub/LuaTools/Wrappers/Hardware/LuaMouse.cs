using Lua;
using TTvActionHub.LuaTools.Hardware;

namespace TTvActionHub.LuaTools.Wrappers.Hardware;

[LuaObject]
public partial class LuaMouse
{
    [LuaMember]
    public static void PressButton(int button)
    {
        Mouse.Press((Mouse.Button)button);
    }

    [LuaMember]
    public static void ReleaseButton(int button)
    {
        Mouse.Release((Mouse.Button)button);
    }

    [LuaMember]
    public static void XPressButton(int xid)
    {
        Mouse.XPress(xid);
    }

    [LuaMember]
    public static void XReleaseButton(int xid)
    {
        Mouse.XRelease(xid);
    }

    [LuaMember]
    public static void HoldButton(int button, int duration = 1000)
    {
        Mouse.Hold((Mouse.Button)button, duration);
    }

    [LuaMember]
    public static void XHoldButton(int button, int duration = 1000)
    {
        Mouse.Hold((Mouse.Button)button, duration);
    }

    [LuaMember]
    public static void ClickButton(int button)
    {
        Mouse.Click((Mouse.Button)button);
    }

    [LuaMember]
    public static void XClickButton(int button)
    {
        Mouse.XClick(button);
    }

    [LuaMember]
    public static void HScroll(int distance)
    {
        Mouse.HScroll(distance);
    }

    [LuaMember]
    public static void VScroll(int distance)
    {
        Mouse.VScroll(distance);
    }

    [LuaMember]
    public static void SetPosition(int x, int y)
    {
        Mouse.SetPosition(x, y);
    }

    [LuaMember]
    public static void Move(int dx, int dy)
    {
        Mouse.Move(dx, dy);
    }

    [LuaMember]
    public static int Button(string button) =>  button switch
    {
        "Left" => 0, "Middle" => 1, "Right" => 2,
        _ => throw new NotImplementedException(),
    };
}