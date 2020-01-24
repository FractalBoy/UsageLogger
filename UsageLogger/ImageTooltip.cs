using System.Drawing;
using System.Windows.Forms;

namespace UsageLogger
{
    class ImageTooltip : ToolTip
    {
        public ImageTooltip() : base()
        {
            OwnerDraw = true;
            Popup += new PopupEventHandler(OnPopup);
            Draw += new DrawToolTipEventHandler(OnDraw);
        }

        private void OnDraw(object sender, DrawToolTipEventArgs e)
        {
            var graphics = e.Graphics;

            var parent = e.AssociatedControl;
            var image = parent.Tag as Image;
            var brush = new TextureBrush(new Bitmap(image));

            graphics.FillRectangle(brush, e.Bounds);
            brush.Dispose();
        }

        private void OnPopup(object sender, PopupEventArgs e)
        {
            e.ToolTipSize = new Size(600, 1000);
        }
    }
}
