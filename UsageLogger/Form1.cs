using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static UsageLogger.NativeMethods;

namespace UsageLogger
{
    public partial class Form1 : Form
    {
        WinEventDelegate dele = null;
        Timer timer;
        DataTable dataTable = new DataTable();

        public Form1()
        {
            InitializeComponent();
            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            timer = new Timer();
            timer.Interval = 5000;
            timer.Start();
            timer.Tick += Timer_Tick;

            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Start", typeof(DateTime));
            dataTable.Columns.Add("End", typeof(DateTime));
            dataTable.Columns.Add("ProgramName", typeof(string));
            dataTable.Columns.Add("Screenshot", typeof(byte[]));
            dataTable.PrimaryKey = new[] { dataTable.Columns[0] };

            dataGridView1.DataSource = dataTable;
            GetData();
        }

        private void GetData()
        {
            using (var context = new WindowLoggingContext())
            {
                var dataAdapter = new SQLiteDataAdapter(@"
                    SELECT
                        s.Id,
                        w.Start,
                        w.End,
                        w.ProgramName,
                        s.Image as Screenshot
                    FROM Screenshots s
                    JOIN WindowLogs w
                        ON s.WindowLog_Id = w.Id
                ", context.Database.Connection.ConnectionString);

                dataTable.Clear();
                dataAdapter.Fill(dataTable);
            }

            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
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
            var bounds = new Rectangle(winRect.Left + diffX /2 , winRect.Top, winRect.Right - winRect.Left - diffX, winRect.Bottom - winRect.Top - diffY);
            var memoryStream = new MemoryStream();

            if (bounds.Width == 0 || bounds.Height == 0)
            {
                return null;
            }

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

            if (imageBytes == null)
            {
                return;
            }

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

            GetData();
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

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var gridView = (DataGridView)sender;
            var cell = gridView[e.ColumnIndex, e.RowIndex];

            // Open up popup with image
            if (cell.OwningColumn.Name == "Screenshot" && typeof(Image).IsAssignableFrom(cell.FormattedValueType))
            {
                var imageCell = (DataGridViewImageCell)cell;
                var pictureBox = new PictureBox();
                var image = (Image)imageCell.FormattedValue;
                pictureBox.Image = image;
                pictureBox.Size = image.Size;
                var imagePopup = new Form();
                imagePopup.AutoSizeMode = AutoSizeMode.GrowOnly;
                imagePopup.AutoSize = true;
                imagePopup.Controls.Add(pictureBox);
                imagePopup.ShowDialog();
            }
        }
    }
}
