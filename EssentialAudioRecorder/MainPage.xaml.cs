using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Store;
using Windows.Storage;
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
        private string thanks;
        private string f;
        StorageFolder captureFolder;

        public MainPage()
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

        private void GetFileName_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
