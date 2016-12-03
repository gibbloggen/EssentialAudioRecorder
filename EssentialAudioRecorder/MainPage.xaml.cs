using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Media.Transcoding;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EssentialAudioRecorder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public ResourceLoader languageLoader;
        private StoreContext context = null;
        StorageFile audioFile = null;  // This is the file that it will record to.  Changeable by user.
        private AudioGraph graph;
        private AudioFileOutputNode fileOutputNode;
        private AudioDeviceInputNode deviceInputNode;
        private AudioDeviceOutputNode deviceOutputNode;
        private DeviceInformationCollection outputDevices;

        private string thanks;
        private string f;
        StorageFolder captureFolder;

        public   MainPage()
        {
            this.InitializeComponent();

            Versatile.Height = 640;
            Versatile.Width = 480;
            
            InitAudio();

          //  languageLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            //thanks = languageLoader.GetString("ManyThanks");
        }

        public async void PurchaseAddOn(string storeId)
        {


            try
            {


                if (context == null)
                {
                    context = StoreContext.GetDefault();

                }

                workingProgressRing.IsActive = true;
                StorePurchaseResult result = await context.RequestPurchaseAsync(storeId);
                workingProgressRing.IsActive = false;

                /*if (result.ExtendedError != null)
                {
                    // The user may be offline or there might be some other server failure.
                    storeResult.Text = $"ExtendedError: {result.ExtendedError.Message}";
                    storeResult.Visibility = Visibility.Visible;
                    return;
                }*/

                switch (result.Status)
                {
                    case StorePurchaseStatus.AlreadyPurchased:
                        storeResult.Text = "The user has already purchased the product.";
                        storeResult.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.Succeeded:
                        //storeResult.Text = "The purchase was successful.";
                        ManyThanks.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.NotPurchased:
                        storeResult.Text = "The user cancelled the purchase.";
                        storeResult.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.NetworkError:
                        storeResult.Text = "The purchase was unsuccessful due to a network error.";
                        storeResult.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.ServerError:
                        storeResult.Visibility = Visibility.Visible;
                        storeResult.Text = "The purchase was unsuccessful due to a server error.";
                        break;

                    default:
                        storeResult.Text = "The purchase was unsuccessful due to an unknown error.";
                        storeResult.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                storeResult.Text = "The purchase was unsuccessful due to an unknown error.";
                storeResult.Visibility = Visibility.Visible;
            }
        }


        private async void InitAudio()

        {

            await PopulateDeviceList();

            //stopRecording.Visibility = Visibility.Collapsed;
            var audioLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            captureFolder = audioLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;


            DeviceInformationCollection j = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);




            var z = j.Count();

            if (j.Count == 0) //messagebox in all languages, no device.
            {

                NoMicrophone.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                foreach (DeviceInformation q in j)
                {
                    ComboBoxItem comboBoxItem = new ComboBoxItem();
                    comboBoxItem.Content = q.Name;
                    comboBoxItem.Tag = q;
                    Microphones.Items.Add(comboBoxItem);

                }
            }
            catch (Exception e)
            {

                BadDevice.Visibility = Visibility.Visible;

            }

            try
            {
                /*
                DeviceInformation gotCamera = (DeviceInformation)j.First();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.VideoDeviceId = gotCamera.Id;
                _mediaCapture = new MediaCapture();

                await _mediaCapture.InitializeAsync();
                _mediaCapture.Failed += _mediaCapture_Failed;

                _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


                GetTheVideo.Source = _mediaCapture;

                await _mediaCapture.StartPreviewAsync();

                PopulateSettingsComboBox();
                */
            }
            catch (Exception e)
            {

                BadSetting.Visibility = Visibility.Visible;
            }
        }

        private async void GetFileName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("MP3 audio", new List<string>() { ".mp3" });
            fileSavePicker.FileTypeChoices.Add("Wave audio", new List<string>() { ".wav" });
            fileSavePicker.FileTypeChoices.Add("Windows Media Audio", new List<string>() { ".wma" });
            fileSavePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            StorageFile file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                audioFile = file;
                AudioFileName.Text = audioFile.Name;
            }
            else return;
            //rootPage.NotifyUser(String.Format("Recording to {0}", file.Name.ToString()), NotifyType.StatusMessage);
            MediaEncodingProfile fileProfile = CreateMediaEncodingProfile(file);


            //my add here,,,
       
        await CreateAudioGraph();

            // Operate node at the graph format, but save file at the specified format
            CreateAudioFileOutputNodeResult fileOutputNodeResult = await graph.CreateFileOutputNodeAsync(file, fileProfile);

            if (fileOutputNodeResult.Status != AudioFileNodeCreationStatus.Success)
            {
                // FileOutputNode creation failed
                //rootPage.NotifyUser(String.Format("Cannot create output file because {0}", fileOutputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
                //fileButton.Background = new SolidColorBrush(Colors.Red);
                return;
            }

            fileOutputNode = fileOutputNodeResult.FileOutputNode;
           GetFileName.Background = new SolidColorBrush(Colors.YellowGreen);

            // Connect the input node to both output nodes
            deviceInputNode.AddOutgoingConnection(fileOutputNode);
            deviceInputNode.AddOutgoingConnection(deviceOutputNode);
            StartStop.IsEnabled = true;

        }
        private async Task ToggleRecordStop()
        {
            if (StartStop.Content.Equals("Record"))
            {
                graph.Start();
                StartStop.Content = "Stop";
               // audioPipe1.Fill = new SolidColorBrush(Colors.Blue);
               // audioPipe2.Fill = new SolidColorBrush(Colors.Blue);
            }
            else if (StartStop.Content.Equals("Stop"))
            {
                // Good idea to stop the graph to avoid data loss
                graph.Stop();
              //  audioPipe1.Fill = new SolidColorBrush(Color.FromArgb(255, 49, 49, 49));
              //  audioPipe2.Fill = new SolidColorBrush(Color.FromArgb(255, 49, 49, 49));

                TranscodeFailureReason finalizeResult = await fileOutputNode.FinalizeAsync();
                if (finalizeResult != TranscodeFailureReason.None)
                {
                    // Finalization of file failed. Check result code to see why
                  //  rootPage.NotifyUser(String.Format("Finalization of file failed because {0}", finalizeResult.ToString()), NotifyType.ErrorMessage);
                    GetFileName.Background = new SolidColorBrush(Colors.Red);
                    return;
                }

                StartStop.Content = "Record";
                //rootPage.NotifyUser("Recording to file completed successfully!", NotifyType.StatusMessage);
                GetFileName.Background = new SolidColorBrush(Colors.Green);
                StartStop.IsEnabled = false;
               // createGraphButton.IsEnabled = false;
            }
        }

        private async Task PopulateDeviceList()
        {
            Speakers.Items.Clear();
            outputDevices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());
            Speakers.Items.Add("-- Pick output device --");
            foreach (var device in outputDevices)
            {
                Speakers.Items.Add(device.Name);
            }
        }
        private async Task CreateAudioGraph()
        {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency;


            //this hard coded, needs to be dynamically set for output devices
            settings.PrimaryRenderDevice = outputDevices[Speakers.SelectedIndex -1];

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                //rootPage.NotifyUser(String.Format("AudioGraph Creation Error because {0}", result.Status.ToString()), NotifyType.ErrorMessage);
                return;
            }

            graph = result.Graph;
            //rootPage.NotifyUser("Graph successfully created!", NotifyType.StatusMessage);

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();
            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output node
               // rootPage.NotifyUser(String.Format("Audio Device Output unavailable because {0}", deviceOutputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
                //outputDeviceContainer.Background = new SolidColorBrush(Colors.Red);
                return;
            }

            deviceOutputNode = deviceOutputNodeResult.DeviceOutputNode;
           // rootPage.NotifyUser("Device Output connection successfully created", NotifyType.StatusMessage);
           // outputDeviceContainer.Background = new SolidColorBrush(Colors.Green);
//
            // Create a device input node using the default audio input device
            CreateAudioDeviceInputNodeResult deviceInputNodeResult = await graph.CreateDeviceInputNodeAsync(MediaCategory.Other);

            if (deviceInputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device input node
               // rootPage.NotifyUser(String.Format("Audio Device Input unavailable because {0}", deviceInputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
              //  inputDeviceContainer.Background = new SolidColorBrush(Colors.Red);
                return;
            }

            deviceInputNode = deviceInputNodeResult.DeviceInputNode;
           // rootPage.NotifyUser("Device Input connection successfully created", NotifyType.StatusMessage);
           // inputDeviceContainer.Background = new SolidColorBrush(Colors.Green);

            // Since graph is successfully created, enable the button to select a file output
           // fileButton.IsEnabled = true;

            // Disable the graph button to prevent accidental click
            //createGraphButton.IsEnabled = false;

            // Because we are using lowest latency setting, we need to handle device disconnection errors
            graph.UnrecoverableErrorOccurred += Graph_UnrecoverableErrorOccurred;
        }


        private void Graph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
         /*   // Recreate the graph and all nodes when this happens
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                sender.Dispose();
                // Re-query for devices
                await PopulateDeviceList();
                // Reset UI
                fileButton.IsEnabled = false;
                recordStopButton.IsEnabled = false;
                recordStopButton.Content = "Record";
                outputDeviceContainer.Background = new SolidColorBrush(Color.FromArgb(255, 74, 74, 74));
                audioPipe1.Fill = new SolidColorBrush(Color.FromArgb(255, 49, 49, 49));
                audioPipe2.Fill = new SolidColorBrush(Color.FromArgb(255, 49, 49, 49));
            });*/
        }

        private MediaEncodingProfile CreateMediaEncodingProfile(StorageFile file)
        {
            switch (file.FileType.ToString().ToLowerInvariant())
            {
                case ".wma":
                    return MediaEncodingProfile.CreateWma(AudioEncodingQuality.High);
                case ".mp3":
                    return MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
                case ".wav":
                    return MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
                default:
                    throw new ArgumentException();
            }
        }

        private async void StartStop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ToggleRecordStop();
        }
    }
}
