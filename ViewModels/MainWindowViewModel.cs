using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace MP3Player_WPF.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {

        // このクラスをシングルトンに設定
        private static MainWindowViewModel? _instance;

        public static MainWindowViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MainWindowViewModel();
                }
                return _instance;
            }
        }




        // 出力デバイスのオブジェクト
        private readonly WaveOutEvent outputDevice;

        //音楽ファイルを読み込むためのオブジェクト
        private AudioFileReader _audioFileReader;

        // 再生用のバーを更新するためのタイマー
        private DispatcherTimer _timer;

        private readonly List<string> musicFilePaths = new();

        // play と pause の切り替えフラグ
        private bool musicRunning = false;

        // 音量 0 との切り替えフラグ
        private bool volumeMute = false;

        // 音楽ファイルが取得されていなければ
        private bool getMusicFile = false;

        // Slider 手動操作用フラグ
        public bool sliderMove = false;

        // 現在選択されている曲の番号
        private int musicNumber = 0;

        // 選択したフォルダ内の全曲数
        private int musicFileCount;

        // 再生ボタン、音量ボタンは、適時画像を切り替える
        private string[] ImagePath = { "../../assets/play.png", "../../assets/pause.png", "../../assets/volume.png", "../../assets/volume-mute.png" };


        [ObservableProperty]
        // 曲名表示用
        private string _musicTitleString = "Enjoy The Music!";

        [ObservableProperty]
        // 再生ボタンの画像入れ替え用
        private string _playImageUrl;

        [ObservableProperty]
        // 音量ボタンの画像入れ替え用
        private string _volumeImageUrl;


        [ObservableProperty]
        // 曲の長さ Slider Maximum
        private double _musicLength;

        // 現在の再生時間の変更を通知
        [NotifyPropertyChangedFor(nameof(CurrentPositionString))]

        // 残りの再生時間の変更を通知
        [NotifyPropertyChangedFor(nameof(TimeLeftString))]

        [ObservableProperty]
        // 現在の再生時間の数値
        private double _currentPosition;

        // 現在の再生時間の数値を、時間に変換
        public string CurrentPositionString => TimeSpan.FromSeconds(CurrentPosition).ToString(@"mm\:ss");

        // 残りの再生時間の数値を、時間に変換
        public string TimeLeftString => TimeSpan.FromSeconds(MusicLength - CurrentPosition).ToString(@"mm\:ss");

        [ObservableProperty]
        // 音量
        private double _volumesValue = 50;


        // Singleton仕様ならば、コンストラクターは、private
        private MainWindowViewModel()
        {
            // 出力デバイスのオブジェクト
            outputDevice = new WaveOutEvent();

            // 初期値 Image Play をセット
            PlayImageUrl = ImagePath[0];

            // 初期値 Image Volume をセット
            VolumeImageUrl = ImagePath[2];
        }



        // アプリの終了作業
        [RelayCommand]
        private void WindowClose(Window window)
        {
            // イベントハンドラを一度削除
            outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;

            StopMusic();

            if (_audioFileReader != null)
            {
                // 読み込んだ音楽ファイルを閉じる
                _audioFileReader.Close();

                // デバイスの破棄
                outputDevice.Dispose();
            }

            window.Close();
        }

        [RelayCommand]
        private void VolumeImage()
        {
            if (_audioFileReader == null || getMusicFile == false) return;

            if (!volumeMute)
            {
                VolumeImageUrl = ImagePath[3];
                volumeMute = true;

                // 音量を0に
                outputDevice.Volume = 0;
            }
            else
            {
                volumeMute = false;
                VolumeImageUrl = ImagePath[2];

                // mute解除で、音量を戻す
                VolumeChanged();
            }
        }

        [RelayCommand]
        private void VolumeChanged()
        {
            if (volumeMute) return;

            outputDevice.Volume = (float)(VolumesValue / 100f);
        }

        // 設定したタイマーの間隔で、こいつが呼ばれる
        private void Timer_Tick(object? sender, EventArgs e)
        {

            // Slider 操作中は、何もしない
            if (sliderMove) return;

            // タイマーの間隔で現在の再生位置を取得し、バーに反映
            CurrentPosition = _audioFileReader.CurrentTime.TotalSeconds;

        }

        // コードビハインドから呼び出し。 Sliderを手動操作して、マウスが離れたら
        public void RunningSliderValue(double clickBarPos)
        {

            // ファイルがまだ読み込まれていなければ、何もしない
            if (!getMusicFile) return;

            // 再生位置が、0より小さくならないように
            if (clickBarPos < 0) clickBarPos = 0;

            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(clickBarPos);

        }

        // 曲が終了した事を検知して、ここに来る この関数は通さないとダメらしい
        private void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // イベントハンドラを一度削除
            outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;


            // もし、musicNumber が、musicFileCount - 1 より小さければ、次を再生
            if (musicRunning && musicNumber < musicFileCount - 1)
            {
                NextMusic();
            }
            else
            {
                // 曲の再生が終了
                StopMusic();

                // 最初の曲をセットしておく
                SetupPlayFile(musicFilePaths[0]);
            }

        }

        // ファイルパスを受け取って、再生出来るようにセットアップ
        private void SetupPlayFile(string filePath)
        {
            // 音楽ファイルを読み込むためのオブジェクト
            _audioFileReader = new AudioFileReader(filePath);

            // 曲名.mp3 を取得
            // MusicTitleString = System.IO.Path.GetFileName(filePath);

            // 曲名のみを取得(拡張子を除外)
            MusicTitleString = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // WaveOutEventの Init() メソッドを呼び出して出力デバイスにAudioFileReaderを設定
            outputDevice.Init(_audioFileReader);

            // 曲の長さを取得
            MusicLength = _audioFileReader.TotalTime.TotalSeconds;


            // 再生用のバーを更新するためのタイマーを設定
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.1);

            // _timer.Interval プロパティに設定した間隔（ここでは 0.1 秒）ごとに、Timer_Tick メソッドが実行され、再生位置の更新が行われます
            _timer.Tick += Timer_Tick;

            // volumeについては、このように使うらしい
            outputDevice.Volume = (float)(VolumesValue / 100f);


        }

        // コードビハインドから呼び出す
        // 音楽ファイルが、windowに放り込まれた時の処理
        public void OpenDropFile(string filePath)
        {
            // 再生中なら、止める
            if (musicRunning) StopMusic();

            // 取り込んでいた物を閉じ、破棄する
            if (_audioFileReader != null)
            {
                _audioFileReader.Close();
                outputDevice.Dispose();
            }

            musicNumber = 0;

            getMusicFile = true;
            
            // 前回取り込んだファイルパスのリストをクリア
            musicFilePaths.Clear(); 

            // 新たなファイルパスをリストにアドする、ここでは1曲のみ
            musicFilePaths.Add(filePath);

            SetupPlayFile(filePath);

        }

        // コードビハインドから、呼び出す
        public void OpenDropDir(string dirPath)
        {

            // 再生中なら、止める
            if (musicRunning) StopMusic();

            // 取り込んでいた物を閉じ、破棄する
            if (_audioFileReader != null)
            {
                _audioFileReader.Close();
                outputDevice.Dispose();
            }

            getMusicFile = true;
            musicNumber = 0;
            musicFilePaths.Clear(); // リストをクリア

            // フォルダ内の.mp3ファイルを取得してリストに追加
            musicFilePaths.AddRange(Directory.GetFiles(dirPath, "*.mp3", SearchOption.TopDirectoryOnly));

            // 曲数を取得
            musicFileCount = musicFilePaths.Count;

            // 最初のファイルを取得して再生
            string selectedFilePath = musicFilePaths[musicNumber];

            SetupPlayFile(selectedFilePath);
        }


        // ダイアログから、ファイルを取り込む
        [RelayCommand]
        private void OpenFile()
        {

            if (musicRunning) return;

            if (_audioFileReader != null)
            {
                _audioFileReader.Close();
                outputDevice.Dispose();


            }
            musicNumber = 0;
            getMusicFile = false;

            using (var dialog = new OpenFileDialog())
            {

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    getMusicFile = true;

                    musicFilePaths.Clear(); // リストをクリア
                    musicFilePaths.Add(dialog.FileName);


                    SetupPlayFile(dialog.FileName);
                }
            }
        }

        // ダイアログから、フォルダを読み込む
        [RelayCommand]
        private void OpenFolder()
        {
            if (musicRunning) return;

            if (_audioFileReader != null)
            {
                _audioFileReader.Close();
                outputDevice.Dispose();
            }
            musicNumber = 0;
            getMusicFile = false;

            // ブロックを抜ける際に自動的に FolderBrowserDialog の Dispose() メソッドが呼び出され、リソースが解放されます
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    getMusicFile = true;

                    string folderPath = dialog.SelectedPath;
                    musicFilePaths.Clear(); // リストをクリア

                    // フォルダ内の.mp3ファイルを取得してリストに追加
                    musicFilePaths.AddRange(Directory.GetFiles(folderPath, "*.mp3", SearchOption.TopDirectoryOnly));

                    // 曲数を取得
                    musicFileCount = musicFilePaths.Count;

                    // 最初のファイルを取得して再生
                    string selectedFilePath = musicFilePaths[musicNumber];

                    // 再生したファイルをリストから削除
                    // musicFilePaths.RemoveAt(0);

                    SetupPlayFile(selectedFilePath);

                }
            }
        }

        // Previous, Next 共通
        private void CommonProcess(int musicNum)
        {

            // イベントハンドラを一度削除
            outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;

            // ファイルのシーク位置を最初に戻します
            _audioFileReader.Position = 0;

            outputDevice.Stop();
            _timer.Stop();
            CurrentPosition = 0;

            SetupPlayFile(musicFilePaths[musicNum]);

            // 再生中に押されたら
            if (musicRunning)
            {
                // ここで1度falseにしないと、play関数で、pauseに行ってしまう
                musicRunning = false;
                PlayMusic();
            }
        }


        // 前の曲へ
        [RelayCommand]
        private void PreviousMusic()
        {

            // CurrentPositionが 1 より小さければ前の曲に。それより進んでいれば、現在の曲の頭に。つまり、ダブルクリックすると、前の曲へ
            if (CurrentPosition < 1)
            { // 前の曲へ
                musicNumber = musicNumber == 0 ? musicFileCount - 1 : musicNumber - 1;

            }
            else
            { // 現在の曲の頭へ
                // 何もしない、現在の musicNumber を渡す
            }


            CommonProcess(musicNumber);
        }

        // 次の曲へ
        [RelayCommand]
        private void NextMusic()
        {

            if (musicNumber < musicFileCount)
            {
                // 例: musicNumber が最後の番号の時、(musicNumber + 1) と、musicFileCount が同じになり、割った余りが0になるので、最初の曲に戻る
                musicNumber = (musicNumber + 1) % musicFileCount;

                CommonProcess(musicNumber);
            }

        }

        // 再生ボタンが押されたときの処理
        [RelayCommand]
        private void PlayMusic()
        {

            if (_audioFileReader == null || getMusicFile == false) return;

            if (!musicRunning) // playボタンが押されたときの処理
            {

                // 再生中は、 Pause画像をセット
                PlayImageUrl = ImagePath[1];

                // 現在の再生位置を反映させる
                _audioFileReader.CurrentTime = TimeSpan.FromSeconds(CurrentPosition);

                // 曲の再生が終了したことを検知する処理
                // outputDevice.PlaybackStopped イベントに OutputDevice_PlaybackStopped メソッドをイベントハンドラとして追加
                // ここでは、必ずイベントハンドラを呼び出さなければならない
                outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;

                // WaveOutEventの Play() メソッドを呼び出して音楽ファイルを再生
                outputDevice.Play();
                musicRunning = true;

                _timer.Start();

            }
            else  // pauseボタンが押されたときの処理
            {
                // 一時停止中は、 play画像をセット
                PlayImageUrl = ImagePath[0];

                outputDevice.Pause();
                musicRunning = false;

                _timer.Stop();
            }
        }

        // 停止ボタンが押されたときの処理
        [RelayCommand]
        public void StopMusic()
        {

            if (_audioFileReader == null || getMusicFile == false) return;

            // イベントハンドラを一度削除
            outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped; 

            // ファイルのシーク位置を最初に戻します
            _audioFileReader.Position = 0;

            musicRunning = false;
            outputDevice.Stop();
            _timer.Stop();
            CurrentPosition = 0;

            PlayImageUrl = ImagePath[0];
        }
    }
}