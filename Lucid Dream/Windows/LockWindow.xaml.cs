using Lucid_Dream.Classes;
using Lucid_Dream.Properties;
#if !DEBUG
using Microsoft.Win32;
#endif
using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Window = System.Windows.Window;

namespace Lucid_Dream
{
    public partial class LockWindow : Window
    {
        private bool isCheck = false,
            animationCheck1 = true,
            animationCheck2 = true,
            timerT = true,
            batteryT = true;

        private Ellipse ellipse;

        public static bool capture = false;

        public LockWindow()
        {
            InitializeComponent();

            var bitmap = Properties.Resources.icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;

            var random = new Random();
            ThreadPool.QueueUserWorkItem((nope) =>
            {
                for (var time = 0; ; time++)
                {
                    if (Settings.Default.Animation)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MakeParticle(Brushes.MediumPurple, random.Next(0, 1920), 1080);
                            MakeParticle(Brushes.DeepPink, random.Next(0, 1920), 1080);
                            MakeParticle(Brushes.Purple, random.Next(0, 1920), 1080);
                        });
                        if (time > 1200)
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                Dispatcher.Invoke(() => canvas.Children.Remove(canvas.Children[i]));
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
            });
        }

        private void MakeParticle(Brush brush, double x, double y)
        {
            var random = new Random();
            ellipse = new Ellipse();
            ellipse.Width = ellipse.Height = random.NextDouble() * 10d;
            ellipse.Fill = brush;
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            canvas.Children.Add(ellipse);
            AnimateEllipse(ellipse);
        }

        private void AnimateEllipse(Ellipse ellipse)
        {
            var random = new Random();
            var ran = random.Next(10, 30);

            var xAnimation = new DoubleAnimation
            {
                By = random.Next(-30, 30),
                Duration = new Duration(new TimeSpan(0, 0, random.Next(3, 6))),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            var yAnimation = new DoubleAnimation
            {
                By = -300,
                Duration = new Duration(new TimeSpan(0, 0, ran))
            };

            var opacityAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(new TimeSpan(0, 0, ran))
            };

            ellipse.BeginAnimation(Canvas.LeftProperty, xAnimation);
            ellipse.BeginAnimation(Canvas.TopProperty, yAnimation);
            ellipse.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private int GetBatteryPercentage()
        {
            var query = new ObjectQuery("SELECT * FROM Win32_Battery");
            using var searcher = new ManagementObjectSearcher(query);

            var collection = searcher.Get();

            foreach (var mo in collection)
            {
                foreach (var property in mo.Properties)
                {
                    if (property.Name == "EstimatedChargeRemaining")
                    {
                        return Convert.ToInt32(property.Value);
                    }
                }
            }
            return 0;
        }

        private ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                Import.DeleteObject(handle);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Topmost = false;
#else
            Hook.Start();
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            key.SetValue("DisableTaskMgr", "1");
            key.Close();
#endif
            Picture.Delete();
            Profile_Image.ImageSource = ImageSourceFromBitmap(Properties.Resources.Profile_Default);
            Battery_Image.Source = ImageSourceFromBitmap(Properties.Resources.Battery);
            var red = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            var yellow = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
            var green = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            ThreadPool.QueueUserWorkItem((x) =>
            {
                while (timerT)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Clock_Label_H.Content = DateTime.Now.ToString("hh");
                        Clock_Label_M.Content = DateTime.Now.ToString("mm");
                        Clock_Label_C.Visibility = Clock_Label_C.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                    });
                    Thread.Sleep(500);
                }
            });
            ThreadPool.QueueUserWorkItem((x) =>
            {
                while (batteryT)
                {
                    var battery = GetBatteryPercentage();
                    Dispatcher.Invoke(() =>
                    {
                        Battery_Gage.Fill = battery <= 33 ? red : battery <= 66 ? yellow : green;
                        Battery_Percentage.Content = $"{battery}%";
                        Battery_Gage.Width = battery * 0.73;
                    });
                    Thread.Sleep(1000);
                }
            });
            Password_TextBox.Focus();
        }

        private void Password_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                using var hash = SHA512.Create();
                var buf = Encoding.UTF8.GetBytes($"{Password_TextBox.Password}{Settings.Default.Salt}");
                using var stream = new MemoryStream(buf);
                var cryptPasswordBuf = hash.ComputeHash(stream);
                var cryptPassword = Encoding.UTF8.GetString(cryptPasswordBuf);
                if (cryptPassword == Settings.Default.Password)
                {
                    isCheck = true;
                    var fade = new DoubleAnimation
                    {
                        To = 0,
                        Duration = new Duration(new TimeSpan(0, 0, 0, 0, 200))
                    };
                    BeginAnimation(OpacityProperty, fade);
                    ThreadPool.QueueUserWorkItem((x) =>
                    {
                        Thread.Sleep(200);
                        Dispatcher.Invoke(Close);
                    });
                }
                else
                {
                    ThreadPool.QueueUserWorkItem((x) =>
                    {
                        try
                        {
                            using var videoCapture = new VideoCapture(0);
                            using var mat = new Mat();
                            videoCapture.Read(mat);
                            mat.SaveImage($@"C:\Lucid Dream\{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.png");
                            capture = true;
                        }
                        catch { }
                    });
                }
                e.Handled = true;
            }
        }

        private void ForgotPassword_Label_MouseEnter(object sender, MouseEventArgs e)
        {
            var fade = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300))
            };
            ForgotPassword_Label.BeginAnimation(OpacityProperty, fade);
        }

        private void ForgotPassword_Label_MouseLeave(object sender, MouseEventArgs e)
        {
            var fade = new DoubleAnimation
            {
                To = 0.2,
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300))
            };
            ForgotPassword_Label.BeginAnimation(OpacityProperty, fade);
        }

        private void ForgotPassword_Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("잠시 후 윈도우에서 로그아웃됩니다. 다시 로그인하신 후 비밀번호를 재설정해주세요.");
            isCheck = true;
            Import.LockWorkStation();
            new NewPasswordWindow().Show();
            Close();
        }

        private void Password_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!animationCheck1)
            {
                var fade = new DoubleAnimation
                {
                    To = 0.1,
                    Duration = new Duration(new TimeSpan(0, 0, 1))
                };
                Password_TextBox.BeginAnimation(OpacityProperty, fade);
                animationCheck1 = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
#if !DEBUG
            if (isCheck)
            {
                Hook.Stop();
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                key.DeleteValue("DisableTaskMgr");
                key.Close();
                timerT = false;
                batteryT = false;
                isCheck = false;
            }
            else
            {
                e.Cancel = true;
            }
#else
            timerT = false;
            batteryT = false;
            isCheck = false;
#endif
        }

        private void Password_TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (animationCheck1)
            {
                var fade = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = new Duration(new TimeSpan(0, 0, 1))
                };
                Password_TextBox.BeginAnimation(OpacityProperty, fade);
                animationCheck1 = false;
            }
        }

        private void Password_TextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (animationCheck2 && !string.IsNullOrEmpty(Password_TextBox.Password))
            {
                var fade = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = new Duration(new TimeSpan(0, 0, 0, 0, 600))
                };
                Password_Placeholder.BeginAnimation(OpacityProperty, fade);
                animationCheck2 = false;
            }
            else if (!animationCheck2 && string.IsNullOrEmpty(Password_TextBox.Password))
            {
                var fade = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = new Duration(new TimeSpan(0, 0, 0, 0, 600))
                };
                Password_Placeholder.BeginAnimation(OpacityProperty, fade);
                animationCheck2 = true;
            }
        }
    }
}
