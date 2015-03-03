using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;

using SharpMotion.Annotations;
using SharpMotion.Core;

namespace SharpMotion
{
    /// <summary>
    /// Represents an active editing session in the GUI.
    /// </summary>
    public class SharpMotionViewModel : INotifyPropertyChanged
    {
        private WebcamSourceViewModel _selectedWebcam;
        private BitmapImage _currentImage;
        private BitmapImage _lastImage;

        private Take _take = new Take();

        public SharpMotionViewModel()
        {
            Webcams = new ObservableCollection<WebcamSourceViewModel>();
            var foo = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            for (int i = 0; i < foo.Count; i++)
            {
                var captureDevice = new VideoCaptureDevice(foo[i].MonikerString);
                var vm = new WebcamSourceViewModel(foo[i].Name, captureDevice);
                Webcams.Add(vm);
            }

            int indexToSelect = 0;
            foreach (var webcam in Webcams)
            {
                if (webcam.SupportedResolutions.Count == 0)
                {
                    indexToSelect++;
                }
                else
                {
                    break;
                }
            }
            if (indexToSelect > Webcams.Count)
            {
                indexToSelect = 0;
            }
            SelectedWebcam = Webcams[indexToSelect];

            GoButtonCommand = new GoCommand(this);
            SnapshotCommand = new TakeSnapshotCommand(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapImage CurrentImage
        {
            get { return _currentImage; }
            set
            {
                if (Equals(value, _currentImage)) return;
                _currentImage = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand GoButtonCommand { get; private set; }

        public BitmapImage LastImage
        {
            get { return _lastImage; }
            set
            {
                if (Equals(value, _lastImage)) return;
                _lastImage = value;
                NotifyPropertyChanged();
            }
        }

        public WebcamSourceViewModel SelectedWebcam
        {
            get { return _selectedWebcam; }
            set
            {
                if (Equals(value, _selectedWebcam)) return;
                if (_selectedWebcam != null)
                {
                    _selectedWebcam.PropertyChanged -= SelectedWebcamOnPropertyChanged;
                }
                _selectedWebcam = value;
                if (_selectedWebcam != null)
                {
                    _selectedWebcam.PropertyChanged += SelectedWebcamOnPropertyChanged;
                }

                NotifyPropertyChanged();
            }
        }

        public ICommand SnapshotCommand { get; private set; }

        public ObservableCollection<WebcamSourceViewModel> Webcams { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectedWebcamOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "CurrentImage")
            {
                CurrentImage = SelectedWebcam.CurrentImage;
            }
        }

        public void Shutdown()
        {
            foreach (var webcam in Webcams)
            {
                webcam.Stop();
            }
        }

        /// <summary>
        /// Starts and stops the selected webcam.
        /// </summary>
        public class GoCommand : ICommand
        {
            private SharpMotionViewModel _parent;

            public GoCommand(SharpMotionViewModel parent)
            {
                _parent = parent;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return _parent.SelectedWebcam != null;
            }

            public void Execute(object parameter)
            {
                try
                {
                    if (_parent.SelectedWebcam.IsRunning)
                    {
                        _parent.SelectedWebcam.Stop();
                        var tempFileName = _parent._take.WriteTempVideo(640, 480, 10, VideoCodec.MPEG4);
                        var now = DateTime.Now;
                        File.Move(tempFileName, string.Format("C:\\Temp\\video_{0:0000}_{1:00}_{2:00}_{3:00}_{4:00}.avi", now.Year, now.Month, now.Day, now.Hour, now.Minute));
                    }
                    else
                    {
                        _parent.SelectedWebcam.Start();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Damnit - " + e.Message);
                }
            }
        }

        /// <summary>
        /// Take a snapshot from the webcam
        /// </summary>
        public class TakeSnapshotCommand : ICommand
        {
            private SharpMotionViewModel _parent;
            private WebcamSourceViewModel _activeWebcam;

            public TakeSnapshotCommand(SharpMotionViewModel parent)
            {
                _parent = parent;
                _parent.PropertyChanged += ParentPropertyChanged;
                _activeWebcam = _parent.SelectedWebcam;
                if (_activeWebcam != null)
                {
                    _activeWebcam.PropertyChanged += ActiveWebcamOnPropertyChanged;
                }
            }

            public event EventHandler CanExecuteChanged;

            private void ActiveWebcamOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
            {
                if (propertyChangedEventArgs.PropertyName == "IsRunning")
                {
                    OnCanExecuteChanged();
                }
            }

            public bool CanExecute(object parameter)
            {
                return _parent.SelectedWebcam != null && _parent.SelectedWebcam.IsRunning;
            }

            private Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
            {
                //bitmapImage.CopyPixels(new Int32Rect(0, 0, bitmapImage.Width, bitmapImage.Height), );
                using (MemoryStream outStream = new MemoryStream())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                    enc.Save(outStream);
                    var bitmap = new Bitmap(outStream);

                    return new Bitmap(bitmap);
                }
            }

            public void Execute(object parameter)
            {
                var currentImage = _parent.CurrentImage;
                _parent.LastImage = currentImage;
                Bitmap image = ConvertBitmapImageToBitmap(currentImage);
                _parent._take.Add(image);
            }

            protected virtual void OnCanExecuteChanged()
            {
                EventHandler handler = CanExecuteChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }

            void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "SelectedWebcam")
                {
                    if (_activeWebcam != null)
                    {
                        _activeWebcam.PropertyChanged -= ActiveWebcamOnPropertyChanged;
                    }
                    _activeWebcam = _parent.SelectedWebcam;
                    if (_activeWebcam != null)
                    {
                        _activeWebcam.PropertyChanged += ActiveWebcamOnPropertyChanged;
                    }
                }
            }
        }
    }
}
