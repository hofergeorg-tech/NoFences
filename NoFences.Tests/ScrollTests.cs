using System.Reflection;
using NoFences.Themes;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class ScrollTests
    {
        private static void Draw(FenceWidget widget, int height)
        {
            using var bitmap = new Bitmap(360, height);
            using var g = Graphics.FromImage(bitmap);
            using var label = new Font("Segoe UI", 12f, GraphicsUnit.Pixel);
            using var big = new Font("Segoe UI", 16f, GraphicsUnit.Pixel);
            widget.Draw(new WidgetCanvas { G = g, Area = new Rectangle(10, 10, 340, height - 20), Theme = ThemeRegistry.Get("default"), Label = label, Big = big, S = 1 });
        }

        private static float Scroll(FenceWidget widget) =>
            (float)widget.GetType().GetField("scroll", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(widget)!;

        // Regression: after scrolling down and making the fence bigger, the list stayed shifted and the wheel did nothing.
    }
}
