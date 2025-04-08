using TTvActionHub.BackEnds.Hardware;

namespace TTvActionHub.LuaTools.Hardware
{
    public static class Mouse
    {
        // --- Public methods ---

        public enum MouseButton : byte
        {
            Left = 0,
            Middle = 1,
            Right = 2,
        }

        public static void Press(MouseButton button)
        {
            var input = InputWrapper.ConstructMouseButtonDown((NativeInputs.MouseButton)button);
            InputWrapper.DispatchInput([input]);
        }

        public static void XPress(int xid)
        {
            var input = InputWrapper.ConstuctXMouseButtonDown(xid);
            InputWrapper.DispatchInput([input]);
        }

        public static void Release(MouseButton button)
        {
            var input = InputWrapper.ConstructMouseButtonUp((NativeInputs.MouseButton)button);
            InputWrapper.DispatchInput([input]);
        }

        public static void XRelease(int xid)
        {
            var input = InputWrapper.ConstuctXMouseButtonUp(xid);
            InputWrapper.DispatchInput([input]);
        }

        public static void Hold(MouseButton button, int timeDelay = 1000)
        {
            Press(button);
            Thread.Sleep(timeDelay);
            Release(button);
        }

        public static void XHold(int xid, int timeDelay = 1000)
        {
            XPress(xid);
            Thread.Sleep(timeDelay);
            XRelease(xid);
        }

        public static void Click(MouseButton button)
        {
            Press(button);
            Thread.Sleep(100);
            Release(button);
        }

        public static void XClick(int xid)
        {
            XPress(xid);
            Thread.Sleep(100);
            XRelease(xid);
        }

        public static void HScroll(int distance)
        {
            var input = InputWrapper.ConstructHWheelScroll(distance);
            InputWrapper.DispatchInput([input]);
        }

        public static void VScroll(int distance)
        {
            var input = InputWrapper.ConstructVWheelScroll(distance);
            InputWrapper.DispatchInput([input]);
        }

        public static void SetPostion(int x, int y)
        {
            var input = InputWrapper.ConstructAbsoluteMouseMove(x, y);
            InputWrapper.DispatchInput([input]);
        }

        public static void Move(int dx, int dy)
        {
            var input = InputWrapper.ConstructRelativeMouseMove(dx, dy);
            InputWrapper.DispatchInput([input]);
        }

    }
}
