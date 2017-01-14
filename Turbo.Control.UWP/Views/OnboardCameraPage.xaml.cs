﻿using System;
using System.Collections.ObjectModel;
using Turbo.Control.UWP.Controller;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Turbo.Control.UWP.Views
{
    public sealed partial class OnboardCameraPage : Page
    {
        private ImageSourceController imageSource;

        public OnboardCameraPage()
        {
            this.InitializeComponent();
            imageSource = ImageSourceController.Instance;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            imageSource.OnImageReceived += ImageSource_OnImageReceived;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            imageSource.OnImageReceived -= ImageSource_OnImageReceived;
        }

        private void ImageSource_OnImageReceived(object sender, EventArgs e)
        {
            this.imgMain.Source = imageSource.CurrentImage;
        }

        public ObservableCollection<BitmapImage> Items
        {
            get { return this.imageSource.CachedImages; }
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            await imageSource.CaptureDeviceImage();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.imgMain.Source = this.imageSource.CachedImages[lvPictureCache.SelectedIndex];
        }
    }
}