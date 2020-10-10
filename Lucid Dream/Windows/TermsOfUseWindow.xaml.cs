using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Lucid_Dream.Windows
{
    public partial class TermsOfUseWindow : Window
    {
        private readonly DoubleAnimation fadeIn = new DoubleAnimation
        {
            To = 1.0,
            Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300)),
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
        }, fadeOut = new DoubleAnimation
        {
            To = 0.2,
            Duration = new Duration(new TimeSpan(0, 0, 0, 0, 300)),
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
        };

        public TermsOfUseWindow()
        {
            InitializeComponent();

            var bitmap = Properties.Resources.icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;
        }

        private void Agree_Button_MouseEnter(object sender, MouseEventArgs e) => Agree_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void Agree_Button_MouseLeave(object sender, MouseEventArgs e) => Agree_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Cancel_Button_MouseEnter(object sender, MouseEventArgs e) => Cancel_Button.BeginAnimation(OpacityProperty, fadeIn);

        private void Cancel_Button_MouseLeave(object sender, MouseEventArgs e) => Cancel_Button.BeginAnimation(OpacityProperty, fadeOut);

        private void Agree_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void Cancel_Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Environment.Exit(0);
            Application.Current.Shutdown(0);
        }
    }
}
