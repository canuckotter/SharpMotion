using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video.DirectShow;
using SharpMotion.Annotations;

namespace SharpMotion
{
    public class WebcamSourceViewModel : INotifyPropertyChanged 
    {
        public WebcamSourceViewModel(string name, VideoCaptureDevice source)
        {
            Name = name;
            Source = source;
            CurrentImage = new BitmapImage();
            SupportedResolutions = new ObservableCollection<string>(GetSupportedResolutions(source));
            SelectedResolutionIndex = 0;
        }

        private static IEnumerable<string> GetSupportedResolutions(VideoCaptureDevice source)
        {
            return source.VideoCapabilities.Select(foo => string.Format("{0}x{1}, max {2} fps", 
                                                                        foo.FrameSize.Width,
                                                                        foo.FrameSize.Height,
                                                                        foo.MaximumFrameRate));
        }

        private string _name;
        private BitmapImage _currentImage;
        private int _selectedResolutionIndex;

        public ObservableCollection<string> SupportedResolutions { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage CurrentImage
        {
            get { return _currentImage; }
            set
            {
                if (Equals(value, _currentImage)) return;
                _currentImage = value;
                OnPropertyChanged();
            }
        }

        public VideoCaptureDevice Source { get; private set; }

        public int SelectedResolutionIndex
        {
            get { return _selectedResolutionIndex; }
            set
            {
                if (value == _selectedResolutionIndex) return;
                _selectedResolutionIndex = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            private set
            {
                if (value.Equals(_isRunning)) return;
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public void Start()
        {
            if (Source == null)
            {
                return;
            }

            int selectedIndex = SelectedResolutionIndex;
            if (selectedIndex < 0 || selectedIndex > Source.VideoCapabilities.Length)
            {
                int highestWidthResolution = 0;
                for (int i = 0; i < Source.VideoCapabilities.Length; i++)
                {
                    if (Source.VideoCapabilities[i].FrameSize.Width > highestWidthResolution)
                    {
                        highestWidthResolution = Source.VideoCapabilities[i].FrameSize.Width;
                        selectedIndex = i;
                    }
                }
            }

            Source.VideoResolution = Source.VideoCapabilities[selectedIndex];
            Source.NewFrame += SourceNewFrame;
            Source.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            Source.SignalToStop();
        }

        private DateTime _nextUpdate = DateTime.MinValue;
        private bool _isRunning;

        public Bitmap CurrentBitmap { get; set; }

        void SourceNewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            DateTime now = DateTime.Now;
            if (now < _nextUpdate)
            {
                return;
            }
            _nextUpdate = now + TimeSpan.FromMilliseconds(100);

            try
            {
                Image img = (Bitmap)eventArgs.Frame.Clone();
                CurrentBitmap = (Bitmap) img;

                MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();

                bi.Freeze();
                img.Dispose();

                Dispatcher.CurrentDispatcher.Invoke(new ThreadStart(delegate
                {
                    CurrentImage = bi;
                }), DispatcherPriority.Render);
            }
            catch (Exception)
            {
                // Do nothing
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}