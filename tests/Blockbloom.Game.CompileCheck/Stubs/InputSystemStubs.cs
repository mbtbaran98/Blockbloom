// YALNIZ DERLEME KONTROLU ICIN STUB. Unity'de kullanilmaz (Assets disinda).
// com.unity.inputsystem 1.11 API'sinin oyun kodunda kullanilan alt kumesini ayni tip/uye adlariyla taklit eder:
// Touchscreen.current, Touchscreen.touches (ReadOnlyArray<TouchControl>), TouchControl.touchId (IntegerControl),
// TouchControl.press (TouchPressControl : ButtonControl), TouchControl.position (Vector2Control),
// Mouse.current, Mouse.leftButton (ButtonControl), Pointer.position, InputControl<T>.ReadValue().
namespace UnityEngine.InputSystem.Utilities
{
    public struct ReadOnlyArray<TValue>
    {
        public int Count => 0;
        public TValue this[int index] => default(TValue);
    }
}

namespace UnityEngine.InputSystem
{
    public abstract class InputControl { }

    public abstract class InputControl<TValue> : InputControl where TValue : struct
    {
        public TValue ReadValue() => default(TValue);
    }

    public class InputDevice : InputControl { }

    public class Pointer : InputDevice
    {
        public Controls.Vector2Control position { get; protected set; }
        public static Pointer current { get; private set; }
    }

    public class Mouse : Pointer
    {
        public Controls.ButtonControl leftButton { get; protected set; }
        public new static Mouse current { get; private set; }
    }

    public class Touchscreen : Pointer
    {
        public Controls.TouchControl primaryTouch { get; protected set; }
        public Utilities.ReadOnlyArray<Controls.TouchControl> touches { get; protected set; }
        public new static Touchscreen current { get; private set; }
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public class AxisControl : InputControl<float> { }

    public class ButtonControl : AxisControl
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
        public bool wasReleasedThisFrame => false;
    }

    public class TouchPressControl : ButtonControl { }

    public class IntegerControl : InputControl<int> { }

    public class Vector2Control : InputControl<Vector2> { }

    public class TouchControl : InputControl<UnityEngine.InputSystem.LowLevel.TouchState>
    {
        public TouchPressControl press { get; set; }
        public IntegerControl touchId { get; set; }
        public Vector2Control position { get; set; }
    }
}

namespace UnityEngine.InputSystem.LowLevel
{
    public struct TouchState { }
}
