using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Blockbloom.Game
{
    /// <summary>Bir karelik tek-isaretci ornegi (ekran pikseli).</summary>
    public struct PointerSample
    {
        /// <summary>Bu karede izlenen temas basladi.</summary>
        public bool Down;
        /// <summary>Izlenen temas suruyor (Down karesinde de true).</summary>
        public bool Held;
        /// <summary>Izlenen temas bu karede bitti (Position son bilinen konum).</summary>
        public bool Up;
        public Vector2 Position;
    }

    /// <summary>
    /// GDD §8 "Coklu dokunus": yalnizca ilk temas izlenir; o temas surerken diger dokunuslar yok sayilir.
    /// Yeni Input System (ENABLE_INPUT_SYSTEM) varsa onu kullanir; yalniz eski Input Manager aciksa ona duser.
    /// Dokunmatik once, sonra fare. Her karede bellek ayirmaz.
    /// </summary>
    public sealed class PointerInput
    {
        private const int None = int.MinValue;
        private const int MouseId = -1000;

        private int _tracked = None;
        private Vector2 _last;

        public bool IsTracking => _tracked != None;

        public PointerSample Read()
        {
            var s = new PointerSample();
#if ENABLE_INPUT_SYSTEM
            ReadInputSystem(ref s);
#elif ENABLE_LEGACY_INPUT_MANAGER
            ReadLegacy(ref s);
#endif
            return s;
        }

        private void Begin(ref PointerSample s, int id, Vector2 pos)
        {
            _tracked = id;
            _last = pos;
            s.Down = true;
            s.Held = true;
            s.Position = pos;
        }

        private void Hold(ref PointerSample s, Vector2 pos)
        {
            _last = pos;
            s.Held = true;
            s.Position = pos;
        }

        private void End(ref PointerSample s)
        {
            _tracked = None;
            s.Up = true;
            s.Position = _last;
        }

#if ENABLE_INPUT_SYSTEM
        private void ReadInputSystem(ref PointerSample s)
        {
            Touchscreen ts = Touchscreen.current;
            if (_tracked != None && _tracked != MouseId)
            {
                if (ts == null) { End(ref s); return; }
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    TouchControl t = touches[i];
                    if (t.touchId.ReadValue() != _tracked) continue;
                    if (t.press.isPressed) { Hold(ref s, t.position.ReadValue()); return; }
                    break;
                }
                End(ref s);
                return;
            }

            Mouse mouse = Mouse.current;
            if (_tracked == MouseId)
            {
                if (mouse != null && mouse.leftButton.isPressed) Hold(ref s, mouse.position.ReadValue());
                else End(ref s);
                return;
            }

            // Izlenen temas yok: yeni baslayan ilk dokunus ya da fare tiklamasi.
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    TouchControl t = touches[i];
                    if (t.press.wasPressedThisFrame)
                    {
                        Begin(ref s, t.touchId.ReadValue(), t.position.ReadValue());
                        return;
                    }
                }
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Begin(ref s, MouseId, mouse.position.ReadValue());
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
        private void ReadLegacy(ref PointerSample s)
        {
            if (_tracked != None && _tracked != MouseId)
            {
                int n = Input.touchCount;
                for (int i = 0; i < n; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.fingerId != _tracked) continue;
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) break;
                    Hold(ref s, t.position);
                    return;
                }
                End(ref s);
                return;
            }

            if (_tracked == MouseId)
            {
                if (Input.GetMouseButton(0)) Hold(ref s, Input.mousePosition);
                else End(ref s);
                return;
            }

            int count = Input.touchCount;
            if (count > 0)
            {
                // Dokunmatikte fare benzetimini (simulateMouseWithTouches) okumamak icin fareye dusulmez.
                for (int i = 0; i < count; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        Begin(ref s, t.fingerId, t.position);
                        return;
                    }
                }
                return;
            }
            if (Input.GetMouseButtonDown(0)) Begin(ref s, MouseId, Input.mousePosition);
        }
#endif
    }
}
