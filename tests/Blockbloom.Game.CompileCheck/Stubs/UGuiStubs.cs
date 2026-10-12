// YALNIZ DERLEME KONTROLU ICIN STUB. Unity'de kullanilmaz (Assets disinda).
// com.unity.ugui 2.0 (Unity 6) API'sinin oyun kodunda kullanilan alt kumesini ayni imzalarla taklit eder.
// Gercek imzalar: UnityEngine.UI.Text : MaskableGraphic : Graphic : UIBehaviour : MonoBehaviour.
namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : MonoBehaviour { }
}

namespace UnityEngine.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class Graphic : UnityEngine.EventSystems.UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
    }

    public abstract class MaskableGraphic : Graphic { }

    public class Text : MaskableGraphic
    {
        public Font font { get; set; }
        public virtual string text { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public float lineSpacing { get; set; }
    }
}
