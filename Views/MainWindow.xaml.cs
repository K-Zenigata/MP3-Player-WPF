using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MP3Player_WPF.ViewModels;

namespace MP3Player_WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        DialogWindow dialogWindow = new();
        VolumeWindow volumeWindow = new();

        public MainWindow()
        {
            InitializeComponent();

            vm = (MainWindowViewModel)DataContext;
        }

        // window の移動
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void OpenDialogWindow_Click(object sender, RoutedEventArgs e)
        {
            // volumeWindowが表示されていたら、隠す
            volumeWindow.Hide();

            double mainLeft = this.Left; // メインウィンドウの左上隅の X 座標
            double mainTop = this.Top;   // メインウィンドウの左上隅の Y 座標

            double xOffset = 50; // X 軸方向のオフセット
            double yOffset = 20;  // Y 軸方向のオフセット

            double dialogWindowLeft = mainLeft + xOffset;
            double dialogWindowTop = mainTop + yOffset;

            dialogWindow.Left = dialogWindowLeft;
            dialogWindow.Top = dialogWindowTop;

            // this は mainWindow 
            dialogWindow.Owner = this; 

            dialogWindow.Show();
        }

        private void OpenVolumeWindow_Click(object sender, RoutedEventArgs e)
        {

            Point clickPosition = Mouse.GetPosition(this); // マウス位置を取得

            double xOffset = 0; // X 軸方向のオフセット
            double yOffset = -50;  // Y 軸方向のオフセット

            // volumeWindow の位置を設定
            volumeWindow.Left = this.Left + clickPosition.X + xOffset;
            volumeWindow.Top =  this.Top + clickPosition.Y + yOffset;

            // volumeWindow を最前面に表示
            volumeWindow.Topmost = true; 
            volumeWindow.Show();
        }

        private void CloseVolumeWindow(object sender, RoutedEventArgs e)
        {
            volumeWindow.Hide();
            dialogWindow.Hide();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // mainWindow を閉じたら subWindow も閉じる
            volumeWindow.Close();
            dialogWindow.Close();
        }


        // 再生用スライダーのクリック設定
        private void PlaySlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            vm.sliderMove = true;
        }

        // マウスボタンが離されたら、そのクリック値をviewModelに渡す
        private void PlaySlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Slider? slider = sender as Slider;

            // クリックしたバーの位置を取得
            double clickPosition = e.GetPosition(slider).X;
            double sliderWidth = slider.ActualWidth;
            double percentage = clickPosition / sliderWidth;
            double clickBarPoint = percentage * (slider.Maximum - slider.Minimum) + slider.Minimum;

            vm.RunningSliderValue(clickBarPoint);
            vm.sliderMove = false;
        }



        // ドラッグアンドドロップ用
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // ドロップされたファイルまたはフォルダの処理
                foreach (string fileOrFolderPath in files)
                {
                    if (Directory.Exists(fileOrFolderPath))
                    {
                        // ドロップされたものがフォルダの場合の処理
                        vm.OpenDropDir(fileOrFolderPath);
                    }
                    else if (File.Exists(fileOrFolderPath))
                    {
                        // ドロップされたものがファイルの場合の処理
                        vm.OpenDropFile(fileOrFolderPath);
                    }
                }
            }
        }
    }
}
