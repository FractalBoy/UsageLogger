using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static UsageLogger.NativeMethods;

namespace UsageLogger
{
    public partial class Form1 : Form
    {
        WinEventDelegate dele = null;
        Timer timer;

        public Form1()
        {
            InitializeComponent();
            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += Timer_Tick;
        }

        private byte[] TakeScreenshot()
        {
            var bounds = Screen.GetBounds(Point.Empty);
            var memoryStream = new MemoryStream();

            using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                bitmap.Save(memoryStream, ImageFormat.Jpeg);
            }

            return memoryStream.ToArray();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var imageBytes = TakeScreenshot();

            using (var context = new WindowLoggingContext())
            {
                var currLog = context.WindowLogs.FirstOrDefault(p => !p.End.HasValue);

                if (currLog == null)
                {
                    return;
                }

                context.Screenshots.Add(new Screenshot { Image = imageBytes, WindowLog_Id = currLog.Id });
                context.SaveChanges();
            }
        }

        void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var imageBytes = TakeScreenshot();

            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            string programName = GetWindowProgramName(hwnd);

            using (var context = new WindowLoggingContext())
            {
                var now = DateTime.UtcNow;

                var prevLog = context.WindowLogs.FirstOrDefault(p => !p.End.HasValue);

                if (prevLog != null)
                {
                    prevLog.End = now;
                }

                var newLog = new WindowLog
                {
                    ProgramName = programName,
                    Start = now
                };

                context.WindowLogs.Add(newLog);
                context.SaveChanges();

                context.Screenshots.Add(new Screenshot { Image = imageBytes, WindowLog_Id = newLog.Id });
                context.SaveChanges();
            }
        }
    }
}
