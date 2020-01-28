using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UsageLogger
{
    class PicturePanel : Panel
    {
        private System.Drawing.Image _image = null;


        public PicturePanel(Image image)
        {
            _image = image;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawImage(_image, ClientRectangle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image.Dispose();
            }
        }
    }
}
