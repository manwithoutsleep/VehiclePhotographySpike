// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VehiclePhotographySpike.WebUI.Models;
using Windows.ApplicationModel;
using Windows.Storage.Search;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT;
using WinRT.Interop;
using Microsoft.UI.Composition.SystemBackdrops;
using VehiclePhotographySpike.WebUI.Helpers;
using System.Runtime.Intrinsics.X86;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VehiclePhotographySpike.WebUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        private AppWindow m_AppWindow;
        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See below for implementation.
        MicaController m_backdropController;
        SystemBackdropConfiguration m_configurationSource;

        public MainWindow()
        {
            this.InitializeComponent();
            GetItemsAsync();

            var isMica = TrySetSystemBackdrop();

            SetTitleBarColors();
            m_AppWindow = GetAppWindowForCurrentWindow();
            m_AppWindow.Title = $"Vehicle Photography Spike {isMica}";
        }

        public ObservableCollection<ImageFileInfo> Images { get; } =
            new ObservableCollection<ImageFileInfo>();

        private async Task GetItemsAsync()
        {
            // See the Remarks section of the KnownFolders.DocumentsLibrary Property page:
            // https://learn.microsoft.com/en-us/uwp/api/windows.storage.knownfolders.documentslibrary?view=winrt-22621#remarks
            // In particular:
            // If your app has to create and update files that only your app uses,
            // consider using the app's LocalCache folder. For more information on
            // which folders you should use for your app's data, see ApplicationData
            // class.
            // 
            // Alternatively, let the user select the file location by using a file
            // picker.For more info, see Open files and folders with a picker and in
            // particular the SuggestedStartLocation property which can be set to
            // DocumentsLibrary. If the user selects the Documents Library from within
            // the picker, you do not need to use this property nor do you need the
            // documentsLibrary capability.


            var userProfileFolderName = KnownFolders.PicturesLibrary;
            StorageFolder folder = userProfileFolderName;


            //StorageFolder appInstalledFolder = Package.Current.InstalledLocation;
            //StorageFolder picturesFolder = await appInstalledFolder.GetFolderAsync("Assets\\Samples");

            var result = picturesFolder.CreateFileQueryWithOptions(new QueryOptions());
            IReadOnlyList<StorageFile> imageFiles = await result.GetFilesAsync();
            foreach (StorageFile file in imageFiles)
            {
                Images.Add(await LoadImageInfo(file));
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr windowHandle = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            return AppWindow.GetFromWindowId(windowId);
        }
        private bool SetTitleBarColors()
        {
            // Check to see if customization is supported.
            // Currently only supported on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                if (m_AppWindow is null)
                {
                    m_AppWindow = GetAppWindowForCurrentWindow();
                }
                var titleBar = m_AppWindow.TitleBar;

                // Set active window colors
                titleBar.ForegroundColor = Colors.White;
                titleBar.BackgroundColor = Colors.Green;
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonBackgroundColor = Colors.SeaGreen;
                titleBar.ButtonHoverForegroundColor = Colors.Gainsboro;
                titleBar.ButtonHoverBackgroundColor = Colors.DarkSeaGreen;
                titleBar.ButtonPressedForegroundColor = Colors.Gray;
                titleBar.ButtonPressedBackgroundColor = Colors.LightGreen;

                // Set inactive window colors
                titleBar.InactiveForegroundColor = Colors.Gainsboro;
                titleBar.InactiveBackgroundColor = Colors.SeaGreen;
                titleBar.ButtonInactiveForegroundColor = Colors.Gainsboro;
                titleBar.ButtonInactiveBackgroundColor = Colors.SeaGreen;
                return true;
            }
            return false;
        }

        public async static Task<ImageFileInfo> LoadImageInfo(StorageFile file)
        {
            var properties = await file.Properties.GetImagePropertiesAsync();
            ImageFileInfo info = new(properties,
                                     file, file.DisplayName, file.DisplayType);

            return info;
        }
        private void ImageGridView_ContainerContentChanging(
            ListViewBase sender,
            ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                var templateRoot = args.ItemContainer.ContentTemplateRoot as Grid;
                var image = templateRoot.FindName("ItemImage") as Image;
                image.Source = null;
            }

            if (args.Phase == 0)
            {
                args.RegisterUpdateCallback(ShowImage);
                args.Handled = true;
            }
        }

        private async void ShowImage(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase == 1)
            {
                // It's phase 1, so show this item's image.
                var templateRoot = args.ItemContainer.ContentTemplateRoot as Grid;
                var image = templateRoot.FindName("ItemImage") as Image;
                var item = args.Item as ImageFileInfo;
                image.Source = await item.GetImageThumbnailAsync();
            }
        }

        bool TrySetSystemBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Create the policy object.
                m_configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_backdropController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();
    
                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_backdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            // so it doesn't try to use this closed window.
            if (m_backdropController != null)
            {
                m_backdropController.Dispose();
                m_backdropController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }
    }
}
