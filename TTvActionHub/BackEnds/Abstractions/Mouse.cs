using TTvActionHub.BackEnds.HardwareWrapper;

namespace TTvActionHub.BackEnds.Abstractions
{
    public static class Mouse
    {
        // --- Public methods ---

        public enum Button : byte
        {
            Left = 0,
            Middle = 1,
            Right = 2,
        }

        public static void Press(Button button)
        {
            var input = InputWrapper.ConstructMouseButtonDown((NativeInputs.MouseButton)button);
            InputWrapper.DispatchInput([input]);
        }

        public static void XPress(int xid)
        {
            var input = InputWrapper.ConstructXMouseButtonDown(xid);
            InputWrapper.DispatchInput([input]);
        }

        public static void Release(Button button)
        {
            var input = InputWrapper.ConstructMouseButtonUp((NativeInputs.MouseButton)button);
            InputWrapper.DispatchInput([input]);
        }

        public static void XRelease(int xid)
        {
            var input = InputWrapper.ConstructXMouseButtonUp(xid);
            InputWrapper.DispatchInput([input]);
        }

        public static void Hold(Button button, int duration = 1000)
        {
            if (duration < 200)
            {
                Click(button);
                return;
            }
            var durStep = duration / 100;
            for (var totalDuration = 0; totalDuration < duration; totalDuration += durStep)
            {
                Press(button);
                Thread.Sleep(durStep);
                Press(button);
            }
        }

        public static void XHold(int xid, int duration = 1000)
        {
            if (duration < 200)
            {
                XClick(xid);
                return;
            }
            var durStep = duration / 100;
            for (var totalDuration = 0; totalDuration < duration; totalDuration += durStep)
            {
                XPress(xid);
                Thread.Sleep(durStep);
                XPress(xid);
            }
        }

        public static void Click(Button button)
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

        public static void SetPosition(int x, int y)
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
