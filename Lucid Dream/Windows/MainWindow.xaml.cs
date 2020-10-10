using Lucid_Dream.Classes;
using Lucid_Dream.Properties;
#if !DEBUG
using System.Diagnostics;
#endif
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Window = System.Windows.Window;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using DesktopToast;
using System.Reflection;
using System.ComponentModel;
using NotificationsExtensions.Toasts;
using NotificationsExtensions;
using System.Collections.Generic;

namespace Lucid_Dream
{
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenu contextMenu;
        private readonly MenuItem settingMenuItem,
            lockNowMenuItem,
            keyboardLockMenuItem,
            exitMenuItem;
        private const string MessageId = "Message";

        public string ToastResult
        {
            get { return (string)GetValue(ToastResultProperty); }
            set { SetValue(ToastResultProperty, value); }
        }
        public static readonly DependencyProperty ToastResultProperty =
            DependencyProperty.Register(
                nameof(ToastResult),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(string.Empty));

        public string ActivationResult
        {
            get { return (string)GetValue(ActivationResultProperty); }
            set { SetValue(ActivationResultProperty, value); }
        }
        public static readonly DependencyProperty ActivationResultProperty =
            DependencyProperty.Register(
                nameof(ActivationResult),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(string.Empty));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(string.Empty));

        public bool CanUseInteractiveToast
        {
            get { return (bool)GetValue(CanUseInteractiveToastProperty); }
            set { SetValue(CanUseInteractiveToastProperty, value); }
        }
        public static readonly DependencyProperty CanUseInteractiveToastProperty =
            DependencyProperty.Register(
                nameof(CanUseInteractiveToast),
                typeof(bool),
                typeof(MainWindow),
                new PropertyMetadata(Environment.OSVersion.Version.Major >= 10));

        private readonly DoubleAnimation fadeIn = new DoubleAnimation
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

        private bool isLockNow = false;

        public MainWindow()
        {
            InitializeComponent();

            var bitmap = Properties.Resources.icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;

            contextMenu = new ContextMenu();
            settingMenuItem = new MenuItem
            {
                Text = "설정"
            };
            lockNowMenuItem = new MenuItem
            {
                Text = "즉시 잠금"
            };
            keyboardLockMenuItem = new MenuItem
            {
                Text = "키보드 잠금"
            };
            exitMenuItem = new MenuItem
            {
                Text = "종료"
            };
            contextMenu.MenuItems.AddRange(new MenuItem[] { settingMenuItem, lockNowMenuItem, keyboardLockMenuItem, exitMenuItem });
            settingMenuItem.Click += (sender, e) => Show();
            lockNowMenuItem.Click += (sender, e) => isLockNow = true;
            keyboardLockMenuItem.Click += (sender, e) =>
            {
                keyboardLockMenuItem.Checked = !keyboardLockMenuItem.Checked;
                if (keyboardLockMenuItem.Checked)
                    Hook.Start(true);
                else
                    Hook.Stop();
            };
            exitMenuItem.Click += (sender, e) => Close();
            notifyIcon = new NotifyIcon()
            {
                BalloonTipText = "Lucid Dream",
                BalloonTipTitle = "Lucid Dream",
                ContextMenu = contextMenu,
                Icon = Properties.Resources.icon,
                Text = "Lucid Dream",
                Visible = true
            };
            notifyIcon.MouseDoubleClick += (sender, e) => Show();
        }

        private WINDOWPLACEMENT GetPlacement()
        {
            var hwnd = Import.GetForegroundWindow();
            var placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            Import.GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        private uint GetIdleTime()
        {
            var lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            Import.GetLastInputInfo(ref lastInPut);

            return (uint)Environment.TickCount - lastInPut.dwTime;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hide();
#if DEBUG
            Settings.Default.Reset();
#endif
            if (string.IsNullOrEmpty(Settings.Default.Password))
            {
                var newPasswordWindow = new NewPasswordWindow();
                newPasswordWindow.ShowDialog();
            }
            if (string.IsNullOrEmpty(Settings.Default.Password))
            {
                Close();
                return;
            }
#if !DEBUG
            var ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = "Lucid Dream";

            td.Principal.UserId = $"{Environment.UserDomainName}\\{Environment.UserName}";
            td.Principal.LogonType = TaskLogonType.InteractiveToken;
            td.Principal.RunLevel = TaskRunLevel.Highest;

            var lt = new LogonTrigger();
            td.Triggers.Add(lt);

            td.Actions.Add(new ExecAction(Process.GetCurrentProcess().MainModule.FileName));
            ts.RootFolder.RegisterTaskDefinition("Lucid Dream", td);
#endif
            Picture.Delete();
            ThreadPool.QueueUserWorkItem((x) =>
            {
                for (; ; )
                {
                    if (isLockNow || ((TimeSpan.FromMilliseconds(GetIdleTime()).TotalSeconds >= Settings.Default.Timer) && (GetPlacement().showCmd != AnotherWindowState.Maximized)))
                    {
                        isLockNow = false;
                        Dispatcher.Invoke(async () =>
                        {
                            Hook.Stop();
                            var lockWindow = new LockWindow();
                            lockWindow.ShowDialog();
                            if (keyboardLockMenuItem.Checked)
                            {
                                Hook.Start(true);
                            }
                            if (LockWindow.capture)
                            {
                                Clear();
                                ToastResult = await ShowInteractiveToastAsync();
                            }
                        });
                    }
                    Thread.Sleep(100);
                }
            });
            Guid_Label.Content = "안녕하세요?\n도움이 필요하시면 언제든지 불러주세요.";
            Timer_TextBox.Text = Settings.Default.Timer.ToString();
            Animation_CheckBox.IsChecked = Settings.Default.Animation;
        }

        private void OnActivated(string arguments, Dictionary<string, string> data)
        {
            var result = "Activated";
            if ((arguments?.StartsWith("action=")).GetValueOrDefault())
            {
                result = arguments.Substring("action=".Length);

                if ((data?.ContainsKey(MessageId)).GetValueOrDefault())
                    Dispatcher.Invoke(() => Message = data[MessageId]);
            }
            Dispatcher.Invoke(() => ActivationResult = result);
        }

        private void Clear()
        {
            ToastResult = "";
            ActivationResult = "";
            Message = "";
        }

        private async Task<string> ShowInteractiveToastAsync()
        {
            var request = new ToastRequest
            {
                ToastXml = ComposeInteractiveToast(),
                ShortcutFileName = "Lucid Dream.lnk",
                ShortcutTargetFilePath = Assembly.GetExecutingAssembly().Location,
                AppId = "Lucid Dream",
                ActivatorId = typeof(NotificationActivator).GUID
            };

            var result = await ToastManager.ShowAsync(request);

            return result.ToString();
        }

        private string ComposeInteractiveToast()
        {
            var toastVisual = new ToastVisual
            {
                BindingGeneric = new ToastBindingGeneric
                {
                    Children =
                    {
                        new AdaptiveText { Text = "Lucy" }, // Title
						new AdaptiveText { Text = "이전에 로그인을 시도했던 사람이 있어요." }, // Body
					}
                    //AppLogoOverride = new ToastGenericAppLogo
                    //{
                    //    Source = string.Format("file:///{0}", Path.GetFullPath("Resources/toast128.png")),
                    //    AlternateText = "Logo"
                    //}
                }
            };
            var toastContent = new ToastContent
            {
                Visual = toastVisual,
                Duration = ToastDuration.Long
            };

            return toastContent.GetContent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            NotificationActivatorBase.RegisterComType(typeof(NotificationActivator), OnActivated);
            NotificationHelper.RegisterComServer(typeof(NotificationActivator), Assembly.GetExecutingAssembly().Location);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            NotificationActivatorBase.UnregisterComType();
            NotificationHelper.UnregisterComServer(typeof(NotificationActivator));
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Hook.Stop();
            Environment.Exit(0);
            Application.Current.Shutdown(0);
        }

        private void Close_Button_MouseEnter(object sender, MouseEventArgs e) => Close_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void DragMove_Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Timer_TextBox_GotFocus(object sender, RoutedEventArgs e) => Guid_Label.Content = "설정한 시간만큼 사용하지 않으면 노트북이 잠기게 됩니다.\n설정할 수 있는 수의 범위는 최소 60에서 최대 999999입니다.";

        private void Cancel_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Hide();

        private void Cancel_Button_MouseEnter(object sender, MouseEventArgs e) => Cancel_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void Cancel_Button_MouseLeave(object sender, MouseEventArgs e) => Cancel_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Close_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Hide();

        private void Timer_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = Regex.IsMatch(e.Text, @"\D+");

        private void Save_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var timer = int.Parse(Timer_TextBox.Text);
                if (timer < 60)
                {
                    Timer_TextBox.Text = "60";
                    Settings.Default.Timer = 60;
                }
                else if (timer > 999999)
                {
                    Timer_TextBox.Text = "999999";
                    Settings.Default.Timer = 999999;
                }
                else
                {
                    Settings.Default.Timer = timer;
                }
            }
            catch (OverflowException)
            {
                Timer_TextBox.Text = "999999";
                Settings.Default.Timer = 999999;
            }
            catch (FormatException)
            {
                Timer_TextBox.Text = "60";
                Settings.Default.Timer = 60;
            }
            Settings.Default.Animation = (bool)Animation_CheckBox.IsChecked;
            Settings.Default.Save();
            Guid_Label.Content = "성공적으로 저장되었습니다.";
        }

        private void Save_Button_MouseEnter(object sender, MouseEventArgs e) => Save_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void Password_TextBox_GotFocus(object sender, RoutedEventArgs e) => Guid_Label.Content = "원래 비밀번호를 입력한 후 비밀번호 변경하기 버튼을 누르면\n새로운 비밀번호를 설정할 수 있습니다.";

        private void ChangePassword_Button_MouseEnter(object sender, MouseEventArgs e) => ChangePassword_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void ChangePassword_Button_MouseLeave(object sender, MouseEventArgs e) => ChangePassword_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void ChangePassword_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            using var hash = SHA512.Create();
            var buf = Encoding.UTF8.GetBytes($"{Password_TextBox.Password}{Settings.Default.Salt}");
            using var stream = new MemoryStream(buf);
            var cryptPasswordBuf = hash.ComputeHash(stream);
            var cryptPassword = Encoding.UTF8.GetString(cryptPasswordBuf);
            if (cryptPassword == Settings.Default.Password)
            {
                new NewPasswordWindow().ShowDialog();
            }
        }

        private void Save_Button_MouseLeave(object sender, MouseEventArgs e) => Save_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Close_Button_MouseLeave(object sender, MouseEventArgs e) => Close_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Mini_Button_MouseEnter(object sender, MouseEventArgs e) => Mini_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void Mini_Button_MouseLeave(object sender, MouseEventArgs e) => Mini_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Mini_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;
    }
}
