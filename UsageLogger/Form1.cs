using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static UsageLogger.NativeMethods;

namespace UsageLogger
{
    public partial class Form1 : Form
    {
        WinEventDelegate dele = null;
        PicturePanel picturePanel = null;

        public Form1()
        {
            InitializeComponent();
            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);

            chart1.Dock = DockStyle.Fill;
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "HH:mm:ss";
            chart1.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart1.ChartAreas[0].AxisY.IntervalType = DateTimeIntervalType.Minutes;

            Application.ApplicationExit += Application_ApplicationExit;

            GetData();
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            using (var context = new WindowLoggingContext())
            {
                var noEnd = context.WindowLogs.FirstOrDefault(p => !p.End.HasValue);

                if (noEnd != null)
                {
                    noEnd.End = DateTime.Now;
                    context.SaveChanges();
                }
            }
        }

        private void GetData()
        {
            chart1.Series.Clear();

            using (var context = new WindowLoggingContext())
            {
                foreach (var log in context.WindowLogs.OrderBy(p => p.Start).ToList())
                {
                    var end = log.End;

                    if (end == null)
                    {
                        continue;
                    }

                    var series = new Series { ChartType = SeriesChartType.RangeBar };
                    chart1.Series.Add(series);
                    series.Points.AddXY(0, log.Start, end);
                    series.Points[0]["LogId"] = log.Id.ToString();
                    series.AxisLabel = "Usage";
                    series.IsVisibleInLegend = false;
                    series["DrawSideBySide"] = "false";
                }
            }
        }

        private byte[] TakeScreenshot()
        {
            var currentWindow = GetForegroundWindow();
            var winRect = new Rect();
            var cliRect = new Rect();
            GetWindowRect(currentWindow, ref winRect);
            GetClientRect(currentWindow, ref cliRect);
            int diffX = winRect.Right - winRect.Left - cliRect.Right + cliRect.Left;
            int diffY = winRect.Bottom - winRect.Top - cliRect.Bottom + cliRect.Top;
            var bounds = new Rectangle(winRect.Left + diffX / 2, winRect.Top, winRect.Right - winRect.Left - diffX, winRect.Bottom - winRect.Top - diffY);
            var memoryStream = new MemoryStream();

            if (bounds.Width == 0 || bounds.Height == 0)
            {
                return null;
            }

            try
            {
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                    bitmap.Save(memoryStream, ImageFormat.Jpeg);
                }
            }
            catch { return null; }

            return memoryStream.ToArray();
        }

        void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var imageBytes = TakeScreenshot();

            if (hwnd == IntPtr.Zero || imageBytes == null)
            {
                return;
            }

            string programName = GetWindowProgramName(hwnd);

            if (programName == null)
            {
                return;
            }

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

            GetData();
        }

        private void Chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var result = chart1.HitTest(e.X, e.Y);

            if (picturePanel != null)
            {
                picturePanel.Hide();
                Controls.Remove(picturePanel);
                picturePanel.Dispose();
                picturePanel = null;
            }

            if (result.Object is DataPoint)
            {
                var dataPoint = (DataPoint)result.Object;
                var logId = int.Parse(dataPoint["LogId"]);
                MemoryStream stream;

                using (var context = new WindowLoggingContext())
                {
                    var screenshot = context.WindowLogs
                        .Join(context.Screenshots,
                        w => w.Id,
                        s => s.WindowLog_Id,
                        (w, s) => new { w.Id, w.Start, w.End, w.ProgramName, s.Image })
                        .FirstOrDefault(p => p.Id == logId);

                    if(screenshot == null)
                    {
                        return;
                    }

                    stream = new MemoryStream(screenshot.Image);
                }

                picturePanel = new PicturePanel(new Bitmap(stream));
                Controls.Add(picturePanel);
                picturePanel.Width = 1280;
                picturePanel.Height = 720;
                picturePanel.Location = e.Location;
                picturePanel.Show();
                picturePanel.BringToFront();
                return;
            }
        }
    }
}
