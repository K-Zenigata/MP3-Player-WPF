using System;
using System.Windows;
using MP3Player_WPF.ViewModels;

namespace MP3Player_WPF.Views
{
    public partial class DialogWindow : Window
    {
        public DialogWindow()
        {

            InitializeComponent();
        }

        public void HideDialogWindowAndFocusParent()
        {
            // 親ウィンドウにフォーカスを移す
            Owner?.Focus();

            // DialogWindow を非表示にする
            Hide();
        }


    }
}
