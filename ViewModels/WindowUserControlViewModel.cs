using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MP3Player_WPF.ViewModels
{
    public partial class WindowCloseUserControlViewModel : ObservableObject
    {

        [RelayCommand]
        private void WindowClose(Window window){
           window.Close();
        }
    }
}
