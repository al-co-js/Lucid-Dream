using Lucid_Dream.Properties;
using Lucid_Dream.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Lucid_Dream
{
    public partial class NewPasswordWindow : Window
    {
        private bool animationCheck = true;
        private readonly DoubleAnimation
            trash = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500))
            },
            fade = new DoubleAnimation
            {
                From = 0.1,
                To = 1.0,
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(new TimeSpan(0, 0, 1))
            }, fadeIn = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300)),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
            }, fadeOut = new DoubleAnimation
            {
                To = 0.1,
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300)),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
            };

        public NewPasswordWindow()
        {
            InitializeComponent();

            var bitmap = Properties.Resources.icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;
        }

        private void Save()
        {
            if (Password_TextBox.Password == Password_TextBox_Check.Password && !string.IsNullOrEmpty(Password_TextBox.Password) && !string.IsNullOrEmpty(Password_TextBox_Check.Password))
            {
                var salt = new char[35];
                var random = new Random();
                var tasks = new List<Task>();
                for (var i = 0; i < 35; i++)
                {
                    tasks.Add(Task.Factory.StartNew((x) => salt[(int)x] = (char)random.Next(65, 123), i));
                }
                Task.WaitAll(tasks.ToArray());
                Settings.Default.Salt = new string(salt);
                Settings.Default.Save();
                Settings.Default.Password = $"{Encoding.UTF8.GetString(SHA512.Create().ComputeHash(new MemoryStream(Encoding.UTF8.GetBytes($"{Password_TextBox.Password}{Settings.Default.Salt}"))))}";
                Settings.Default.Save();
                Directory.CreateDirectory($@"C:\Lucid Dream");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Close();
            }
        }

        private void PasswordSave_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Save();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            if (string.IsNullOrEmpty(Settings.Default.Password))
            {
                new TermsOfUseWindow().ShowDialog();
            }
#endif
            Password_TextBox.Focus();
            Guid_Label.BeginAnimation(OpacityProperty, fade);
            Guid_Label.Content = "저는 루시드 드림의 사용을 도와주는\n도우미, 루시입니다.";
            var sw = new Stopwatch();
            sw.Start();
            for (; ; )
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1);
                sw.Stop();
                if (sw.Elapsed.TotalSeconds >= 2)
                {
                    sw.Restart();
                    break;
                }
                sw.Start();
            }
            Guid_Label.BeginAnimation(OpacityProperty, fade);
            Guid_Label.Content = "제가 지시하는 대로 따라주세요.";
            for (; ; )
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1);
                sw.Stop();
                if (sw.Elapsed.TotalSeconds >= 2)
                {
                    break;
                }
                sw.Start();
            }
            Guid_Label.BeginAnimation(OpacityProperty, trash);
            Password_TextBox.BeginAnimation(OpacityProperty, fade);
            Guid_Label.Content = "비밀번호를 입력하세요";
        }

        private void Password_TextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password_TextBox.BeginAnimation(OpacityProperty, trash);
            if (animationCheck && Password_TextBox.Password.Length < 4)
            {
                Guid_Label.Content = "비밀번호는 4자 이상으로 해주세요";
                animationCheck = false;
            }
            if (!animationCheck && Password_TextBox.Password.Length >= 4)
            {
                Password_TextBox_Check.BeginAnimation(OpacityProperty, fade);
                Guid_Label.Content = "비밀번호를 아래에 다시 적어주세요";
                animationCheck = true;
            }
        }

        private void Password_TextBox_Check_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Save();
                e.Handled = true;
            }
        }

        private void Close_Button_MouseEnter(object sender, MouseEventArgs e) => Close_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void DragMove_Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Close_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void Close_Button_MouseLeave(object sender, MouseEventArgs e) => Close_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Mini_Button_MouseEnter(object sender, MouseEventArgs e) => Mini_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void Mini_Button_MouseLeave(object sender, MouseEventArgs e) => Mini_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Mini_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;

        private void Password_TextBox_Check_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password_TextBox_Check.BeginAnimation(OpacityProperty, trash);
            if (animationCheck && Password_TextBox.Password != Password_TextBox_Check.Password)
            {
                Guid_Label.Content = "비밀번호가 일치하지 않습니다";
                animationCheck = false;
            }
            else if (!animationCheck && Password_TextBox.Password == Password_TextBox_Check.Password)
            {
                PasswordSave_Button.BeginAnimation(OpacityProperty, fade);
                Guid_Label.Content = "시작하세요";
                animationCheck = true;
            }
        }
    }
}
