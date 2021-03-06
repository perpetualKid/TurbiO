﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BrickPi.Uwp;
using BrickPi.Uwp.Base;
using BrickPi.Uwp.Sensors.NXT;
using Devices.Communication;
using Devices.Components;
using Devices.Components.Common.Communication;
using Devices.Components.Common.Media;
using Turbo.BrickPi.Components.Lego;
using Turbo.BrickPi.Components.Storage;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Turbo.BrickPi.IoT
{

    public sealed class StartupTask : IBackgroundTask
    {

        NXTTouchSensor touch;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "Camera Roll\\photo.jpg";

        private OneDriveComponent oneDrive;
        private AzureBlobStorageComponent azureBlob;
        private BrickPiComponent brickComponent;
        private Brick brick;
        private DriveComponent omniDrive;
        private CameraComponent camera;
        //        int counter;
        BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            List<Task> setupTasks = new List<Task>();

            setupTasks.Add(ComponentHandler.RegisterComponent(new SocketListener(8027)));
            setupTasks.Add(ComponentHandler.RegisterComponent(new SocketListener(8031, DataFormat.Json)));

            oneDrive = new OneDriveComponent();
            setupTasks.Add(ComponentHandler.RegisterComponent(oneDrive));
            azureBlob = new AzureBlobStorageComponent();
            setupTasks.Add(ComponentHandler.RegisterComponent(azureBlob));
            brickComponent = new BrickPiComponent();
            setupTasks.Add(ComponentHandler.RegisterComponent(brickComponent));
            setupTasks.Add(ComponentHandler.RegisterComponent(new AppSettingsComponent()));
            setupTasks.Add(ComponentHandler.RegisterComponent(new TimerComponent()));

            await Task.WhenAll(setupTasks).ConfigureAwait(false);

            brick = brickComponent.BrickPi;
            Debug.WriteLine($"Brick Version: {brickComponent.Version}");

            touch = new NXTTouchSensor(SensorPort.Port_S1);
            touch.OnPressed += Touch_OnPressed;
            await brick.Sensors.Add(touch).ConfigureAwait(false);
            NXTColorSensor color;
            color = new NXTColorSensor(SensorPort.Port_S4, SensorType.COLOR_FULL);
            await brick.Sensors.Add(color).ConfigureAwait(false);
            color = new NXTColorSensor(SensorPort.Port_S3, SensorType.COLOR_FULL);
            await brick.Sensors.Add(color).ConfigureAwait(false);
            NXTUltraSonicSensor distance = new NXTUltraSonicSensor(SensorPort.Port_S2);
            await brick.Sensors.Add(distance).ConfigureAwait(false);
            await brickComponent.RegisterSensors();
            omniDrive = new DriveComponent("Drive", brickComponent, brick.Motors[MotorPort.Port_MA], brick.Motors[MotorPort.Port_MD], brick.Motors[MotorPort.Port_MB]);
            await ComponentHandler.RegisterComponent(omniDrive).ConfigureAwait(false);

            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask<DeviceInformationCollection>().ConfigureAwait(false);

            camera = new CameraComponent("FrontCamera",
                new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    PhotoCaptureSource = PhotoCaptureSource.Auto,
                    AudioDeviceId = string.Empty,
                    VideoDeviceId = videoDevices[0].Id
                });
            await ComponentHandler.RegisterComponent(camera).ConfigureAwait(false);

            //await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            //{
            //    StreamingCaptureMode = StreamingCaptureMode.Video,
            //    PhotoCaptureSource = PhotoCaptureSource.Auto,
            //    //AudioDeviceId = string.Empty,
            //    VideoDeviceId = allVideoDevices[0].Id
            //}).AsTask().ConfigureAwait(false);

            //var resolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).OrderByDescending( resolution => ((VideoEncodingProperties)resolution).Width);
            //foreach (var item in resolutions)
            //{
            //    Debug.WriteLine((item as VideoEncodingProperties).Width);
            //}

            //await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, resolutions.First());

            brick.Start();
            //while (true)
            //{
            //    brick.Arduino1Led.Toggle();
            //    await Task.Delay(500).ConfigureAwait(false);
            //}
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //a few reasons that you may be interested in.
            switch (reason)
            {
                case BackgroundTaskCancellationReason.Abort:
                    //app unregistered background task (amoung other reasons).
                    break;
                case BackgroundTaskCancellationReason.Terminating:
                    //system shutdown
                    break;
                case BackgroundTaskCancellationReason.ConditionLoss:
                    break;
                case BackgroundTaskCancellationReason.SystemPolicy:
                    break;
            }
            deferral.Complete();
        }

        private async void Touch_OnPressed(object sender, SensorChangedEventArgs e)
        {
            using (IRandomAccessStream stream = await camera.CaptureMediaStream(ImageEncodingProperties.CreateJpeg()).ConfigureAwait(false))
            {
                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(PHOTO_FILE_NAME, CreationCollisionOption.GenerateUniqueName);
                await oneDrive.UploadFile(stream, "/Pics", photoFile.Name);
            }
            //await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile).AsTask().ConfigureAwait(false);


            //CloudBlockBlob blockBlob = container.GetBlockBlobReference(photoFile.Name);
            //await blockBlob.UploadFromStreamAsync(stream.AsStreamForRead()).ConfigureAwait(false);
        }


    }
}
