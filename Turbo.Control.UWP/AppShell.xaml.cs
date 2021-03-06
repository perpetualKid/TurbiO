﻿using System;
using System.Collections.Generic;
using System.Linq;
using Devices.Controllers.Base;
using Turbo.Control.UWP.Controls;
using Turbo.Control.UWP.Controllers;
using Turbo.Control.UWP.Views;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Turbo.Control.UWP
{
    /// <summary>
    /// The "chrome" layer of the app that provides top-level navigation with
    /// proper keyboarding navigation.
    /// </summary>
    public sealed partial class AppShell : Page
    {
        private bool isPaddingAdded = false;
        // Declare the top level nav items
        private List<NavigationMenuItem> navlist = new List<NavigationMenuItem>(
            new[]
            {
                new NavigationMenuItem()
                {
                    Symbol = Symbol.View,
                    Label = "Explore",
                    DestinationPage = typeof(ExplorePage)
                },
                /*
                new NavigationMenuItem()
                {
                    Symbol = Symbol.Edit,
                    Label = "CommandBar Page",
//                    DestPage = typeof(CommandBarPage)
                },
                new NavigationMenuItem()
                {
                    Symbol = Symbol.Favorite,
                    Label = "Drill In Page",
//                    DestPage = typeof(DrillInPage)
                },*/
                new NavigationMenuItem()
                {
                    Symbol = Symbol.SyncFolder,
                    Label = "OneDrive Login",
                    DestinationPage = typeof(OneDriveLoginPage)
                },
                new NavigationMenuItem()
                {
                    Symbol = Symbol.Camera,
                    Label = "Onboard Camera",
                    DestinationPage = typeof(OnboardCameraPage)
                },
                new NavigationMenuItem()
                {
                    Symbol = Symbol.Repair,
                    Label = "Debug Page",
                    DestinationPage = typeof(DebugPage)
                },
                new NavigationMenuItem()
                {
                    Symbol = Symbol.Manage,
                    Label = "Control Panel",
                    DestinationPage = typeof(DebugPage)
                },

            });

        public static AppShell Current = null;

        /// <summary>
        /// Initializes a new instance of the AppShell, sets the static 'Current' reference,
        /// adds callbacks for Back requests and changes in the SplitView's DisplayMode, and
        /// provide the nav menu list with the data to display.
        /// </summary>
        public AppShell()
        {
            this.InitializeComponent();

            this.Loaded += (sender, args) =>
            {
                Current = this;

                this.CheckTogglePaneButtonSizeChanged();

                var titleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
                titleBar.IsVisibleChanged += TitleBar_IsVisibleChanged;
            };

            this.RootSplitView.RegisterPropertyChangedCallback(
                SplitView.DisplayModeProperty,
                (s, a) =>
                {
                    // Ensure that we update the reported size of the TogglePaneButton when the SplitView's
                    // DisplayMode changes.
                    this.CheckTogglePaneButtonSizeChanged();
                });

            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;


            NavMenuList.ItemsSource = navlist;
        }

        public Frame AppFrame { get { return this.frame; } }

        /// <summary>
        /// Invoked when window title bar visibility changes, such as after loading or in tablet mode
        /// Ensures correct padding at window top, between title bar and app content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void TitleBar_IsVisibleChanged(Windows.ApplicationModel.Core.CoreApplicationViewTitleBar sender, object args)
        {
            if (!this.isPaddingAdded && sender.IsVisible)
            {
                //add extra padding between window title bar and app content
                double extraPadding = (Double)App.Current.Resources["DesktopWindowTopPadding"];
                this.isPaddingAdded = true;

                Thickness margin = NavMenuList.Margin;
                NavMenuList.Margin = new Thickness(margin.Left, margin.Top + extraPadding, margin.Right, margin.Bottom);
                margin = AppFrame.Margin;
                AppFrame.Margin = new Thickness(margin.Left, margin.Top + extraPadding, margin.Right, margin.Bottom);
                margin = TogglePaneButton.Margin;
                TogglePaneButton.Margin = new Thickness(margin.Left, margin.Top + extraPadding, margin.Right, margin.Bottom);
            }
        }

        /// <summary>
        /// Default keyboard focus movement for any unhandled keyboarding
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppShell_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            FocusNavigationDirection direction = FocusNavigationDirection.None;
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Left:
                case Windows.System.VirtualKey.GamepadDPadLeft:
                case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
                case Windows.System.VirtualKey.NavigationLeft:
                    direction = FocusNavigationDirection.Left;
                    break;
                case Windows.System.VirtualKey.Right:
                case Windows.System.VirtualKey.GamepadDPadRight:
                case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
                case Windows.System.VirtualKey.NavigationRight:
                    direction = FocusNavigationDirection.Right;
                    break;

                case Windows.System.VirtualKey.Up:
                case Windows.System.VirtualKey.GamepadDPadUp:
                case Windows.System.VirtualKey.GamepadLeftThumbstickUp:
                case Windows.System.VirtualKey.NavigationUp:
                    direction = FocusNavigationDirection.Up;
                    break;

                case Windows.System.VirtualKey.Down:
                case Windows.System.VirtualKey.GamepadDPadDown:
                case Windows.System.VirtualKey.GamepadLeftThumbstickDown:
                case Windows.System.VirtualKey.NavigationDown:
                    direction = FocusNavigationDirection.Down;
                    break;
            }

            if (direction != FocusNavigationDirection.None)
            {
                var control = FocusManager.FindNextFocusableElement(direction) as Windows.UI.Xaml.Controls.Control;
                if (control != null)
                {
                    control.Focus(FocusState.Keyboard);
                    e.Handled = true;
                }
            }
        }

        #region BackRequested Handlers

        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            bool handled = e.Handled;
            this.BackRequested(ref handled);
            e.Handled = handled;
        }

        private void BackRequested(ref bool handled)
        {
            // Get a hold of the current frame so that we can inspect the app back stack.

            if (this.AppFrame == null)
                return;

            // Check to see if this is the top-most page on the app back stack.
            if (this.AppFrame.CanGoBack && !handled)
            {
                // If not, set the event to handled and go back to the previous page in the app.
                handled = true;
                this.AppFrame.GoBack();
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Navigate to the Page for the selected <paramref name="listViewItem"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="listViewItem"></param>
        private void NavMenuList_ItemInvoked(object sender, ListViewItem listViewItem)
        {
            foreach (var i in navlist)
            {
                i.IsSelected = false;
            }

            var item = (NavigationMenuItem)((NavigationMenuListView)sender).ItemFromContainer(listViewItem);

            if (item != null)
            {
                item.IsSelected = true;
                if (item.DestinationPage != null &&
                    item.DestinationPage != this.AppFrame.CurrentSourcePageType)
                {
                    this.AppFrame.Navigate(item.DestinationPage, item.Arguments);
                }
            }
        }

        /// <summary>
        /// Ensures the nav menu reflects reality when navigation is triggered outside of
        /// the nav menu buttons.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNavigatingToPage(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                var item = (from p in this.navlist where p.DestinationPage == e.SourcePageType select p).SingleOrDefault();
                if (item == null && this.AppFrame.BackStackDepth > 0)
                {
                    // In cases where a page drills into sub-pages then we'll highlight the most recent
                    // navigation menu item that appears in the BackStack
                    foreach (var entry in this.AppFrame.BackStack.Reverse())
                    {
                        item = (from p in this.navlist where p.DestinationPage == entry.SourcePageType select p).SingleOrDefault();
                        if (item != null)
                            break;
                    }
                }

                foreach (var i in navlist)
                {
                    i.IsSelected = false;
                }
                if (item != null)
                {
                    item.IsSelected = true;
                }

                var container = (ListViewItem)NavMenuList.ContainerFromItem(item);

                // While updating the selection state of the item prevent it from taking keyboard focus.  If a
                // user is invoking the back button via the keyboard causing the selected nav menu item to change
                // then focus will remain on the back button.
                if (container != null) container.IsTabStop = false;
                NavMenuList.SetSelectedItem(container);
                if (container != null) container.IsTabStop = true;
            }
        }

        #endregion

        public Rect TogglePaneButtonRect
        {
            get;
            private set;
        }

        /// <summary>
        /// An event to notify listeners when the hamburger button may occlude other content in the app.
        /// The custom "PageHeader" user control is using this.
        /// </summary>
        public event TypedEventHandler<AppShell, Rect> TogglePaneButtonRectChanged;

        /// <summary>
        /// Public method to allow pages to open SplitView's pane.
        /// Used for custom app shortcuts like navigating left from page's left-most item
        /// </summary>
        public void OpenNavePane()
        {
            TogglePaneButton.IsChecked = true;
            NavPaneDivider.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hides divider when nav pane is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void RootSplitView_PaneClosed(SplitView sender, object args)
        {
            NavPaneDivider.Visibility = Visibility.Collapsed;

            // Prevent focus from moving to elements when they're not visible on screen
            SettingsNavPaneButton.IsTabStop = false;
        }

        /// <summary>
        /// Callback when the SplitView's Pane is toggled closed.  When the Pane is not visible
        /// then the floating hamburger may be occluding other content in the app unless it is aware.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogglePaneButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.CheckTogglePaneButtonSizeChanged();
        }

        /// <summary>
        /// Callback when the SplitView's Pane is toggled opened.
        /// Restores divider's visibility and ensures that margins around the floating hamburger are correctly set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {
            NavPaneDivider.Visibility = Visibility.Visible;
            this.CheckTogglePaneButtonSizeChanged();

            SettingsNavPaneButton.IsTabStop = true;
        }

        /// <summary>
        /// Check for the conditions where the navigation pane does not occupy the space under the floating
        /// hamburger button and trigger the event.
        /// </summary>
        private void CheckTogglePaneButtonSizeChanged()
        {
            if (this.RootSplitView.DisplayMode == SplitViewDisplayMode.Inline ||
                this.RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                var transform = this.TogglePaneButton.TransformToVisual(this);
                var rect = transform.TransformBounds(new Rect(0, 0, this.TogglePaneButton.ActualWidth, this.TogglePaneButton.ActualHeight));
                this.TogglePaneButtonRect = rect;
            }
            else
            {
                this.TogglePaneButtonRect = new Rect();
            }

            var handler = this.TogglePaneButtonRectChanged;
            if (handler != null)
            {
                // handler(this, this.TogglePaneButtonRect);
                handler.DynamicInvoke(this, this.TogglePaneButtonRect);
            }
        }

        /// <summary>
        /// Enable accessibility on each nav menu item by setting the AutomationProperties.Name on each container
        /// using the associated Label of each item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NavMenuItemContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue && args.Item != null && args.Item is NavigationMenuItem)
            {
                args.ItemContainer.SetValue(AutomationProperties.NameProperty, ((NavigationMenuItem)args.Item).Label);
            }
            else
            {
                args.ItemContainer.ClearValue(AutomationProperties.NameProperty);
            }
        }

        private void SettingsNavPaneButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavMenuList.SetSelectedItem(null);
            if (this.AppFrame.CurrentSourcePageType != typeof(AppSettingsPage))
                this.AppFrame.Navigate(typeof(AppSettingsPage), e.OriginalSource, new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void HomeNavPaneButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavMenuList.SetSelectedItem(null);
            if (this.AppFrame.CurrentSourcePageType != typeof(LandingPage))
                this.AppFrame.Navigate(typeof(LandingPage), e.OriginalSource, new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void DebugToggleButton_Click(object sender, RoutedEventArgs e)
        {
            DebugHandler.Instance.Enabled = (sender as AppBarToggleButton).IsChecked.Value;
        }

        private async void ConnectDeviceToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            await Connect(sender);
        }

        private async Task Connect(object sender)
        {
            try
            {

                ConnectDeviceToggleButton.IsEnabled = false;
                symbolIcon.Symbol = Symbol.Sync;
                ConnectingActivity.Begin();
                ConnectionFlyoutText.Text = DeviceConnectionHandler.ConnectionFlyoutText;

                if (!DeviceConnectionHandler.Instance.CheckParametersSet())
                {
                    if (await DeviceConnectionHandler.Instance.ShowMissingHostParametersDialog() == ContentDialogResult.Primary)
                    {
                        if (this.AppFrame.CurrentSourcePageType != typeof(AppSettingsPage))
                            this.AppFrame.Navigate(typeof(AppSettingsPage), sender, new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    ConnectDeviceToggleButton.IsChecked = false;
                    return;
                }

                ContentDialogResult result = ContentDialogResult.Primary;
                ConnectionFlyout.ShowAt(sender as FrameworkElement);
                while (result == ContentDialogResult.Primary && !await DeviceConnectionHandler.Instance.Connect())
                {
                    ConnectionFlyout.ShowAt(sender as FrameworkElement);
                    result = await DeviceConnectionHandler.Instance.ShowConnectionFailedDialog();
                }
                if (result == ContentDialogResult.Secondary)
                {
                    if (this.AppFrame.CurrentSourcePageType != typeof(AppSettingsPage))
                        this.AppFrame.Navigate(typeof(AppSettingsPage), sender, new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    ConnectDeviceToggleButton.IsChecked = false;
                }
            }
            finally
            {
                ConnectionFlyout.Hide();
                ConnectingActivity.Stop();
                symbolIcon.Symbol = Symbol.Remote;
                ConnectDeviceToggleButton.IsEnabled = true;
            }
        }

        private async void ConnectDeviceToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectDeviceToggleButton.IsEnabled = false;
                ConnectingActivity.Begin();
                await DeviceConnectionHandler.Instance.Disconnect();
            }
            finally
            {
                ConnectingActivity.Stop();
                ConnectDeviceToggleButton.IsEnabled = true;
            }
        }

        private async void ShutdownSystemButton_Click(object sender, RoutedEventArgs e)
        {
            if (ControllerHandler.Connected)
            {
                ContentDialog shutdownDialog = new ContentDialog()
                {
                    Title = "Shutdown Device!",
                    Content = "Shutdown or Restart the device.",
                    PrimaryButtonText = "Shutdown",
                    SecondaryButtonText = "Reboot"
                };

                ContentDialogResult result = await shutdownDialog.ShowAsync();
                if (result != ContentDialogResult.None)
                {
                    GenericController shutdownController = await GenericController.GetNamedInstance<GenericController>("RootController", "Root").ConfigureAwait(false);
                    JsonObject request = new JsonObject
                    {
                        { "Target", JsonValue.CreateStringValue("Root") },
                        { "Action", JsonValue.CreateStringValue("Shutdown") }
                    };
                    request.Add("Restart", JsonValue.CreateBooleanValue(result == ContentDialogResult.Secondary));
                    await shutdownController.SendRequest(request, true).ConfigureAwait(false);
                    await ControllerHandler.Disconnect().ConfigureAwait(false);
                }
            }
        }
    }
}
