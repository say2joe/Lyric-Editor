﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using HappyStudio.UwpToolsLibrary.Auxiliarys;
using HappyStudio.UwpToolsLibrary.Information;
using SimpleLyricsEditor.Core;
using SimpleLyricsEditor.Events;
using SimpleLyricsEditor.Models;
using WinRTExceptions;
using AppInfo = HappyStudio.UwpToolsLibrary.Information.AppInfo;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace SimpleLyricsEditor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private const string Emill = "kljzndx@outlook.com";
        
        private static bool _isPressCtrl;
        private static bool _isPressShift;
        private static CoreWindow _coreWindow;
        private static ExceptionHandlingSynchronizationContext _exceptionHandlingSynchronizationContext;

        public static bool IsInputing;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += OnUnhandledException;

            Settings settings = Settings.Current;
            if (settings.PageTheme != ElementTheme.Default)
                this.RequestedTheme = (ApplicationTheme) ((int) settings.PageTheme - 1);
        }

        private void KeyEventInitializer()
        {
            if (_coreWindow != null)
                return;

            _coreWindow = CoreWindow.GetForCurrentThread();
            _coreWindow.KeyDown += Window_KeyDown;
            _coreWindow.KeyUp += Window_KeyUp;
        }

        private void EnsureSyncContext()
        {
            if (_exceptionHandlingSynchronizationContext != null)
                return;

            _exceptionHandlingSynchronizationContext = ExceptionHandlingSynchronizationContext.Register();
            _exceptionHandlingSynchronizationContext.UnhandledException += OnSynchronizationContextUnhandledException;
        }

        private static async Task ShowErrorDialog(Exception ex)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(CharacterLibrary.ErrorTable.GetString("OperationProcess"));
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine($"{CharacterLibrary.ErrorTable.GetString("SystemVersion")} {SystemInfo.BuildVersion}");
            builder.AppendLine(ex.ToLongString());

            string errorReportEmillTitle =
                $"{AppInfo.Name} {AppInfo.Version} {CharacterLibrary.MessageBox.GetString("ErrorReportEmillTitle")}";

            var buttons = new Dictionary<string, UICommandInvokedHandler>
            {
                {
                    CharacterLibrary.MessageBox.GetString("SendErrorReport"),
                    async c => await EmailEx.SendAsync(Emill, errorReportEmillTitle, builder.ToString())
                },
                {CharacterLibrary.MessageBox.GetString("Close"), null}
            };

            await MessageBox.ShowAsync(CharacterLibrary.MessageBox.GetString("ErrorReportDialogTitle"),
                ex.ToShortString(), buttons);
        }

        private void AppInitializer(IActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var normalLaunchArgs = args as LaunchActivatedEventArgs;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (normalLaunchArgs != null)
                if (normalLaunchArgs.PrelaunchActivated)
                    return;

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(Views.UiFramework), normalLaunchArgs?.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            KeyEventInitializer();
            EnsureSyncContext();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            AppInitializer(args);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            AppInitializer(e);
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            AppInitializer(args);

            LyricsFileNotifier.ChangeFile(args.Files.Last() as StorageFile);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void Window_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Control:
                    _isPressCtrl = true;
                    break;
                case VirtualKey.Shift:
                    _isPressShift = true;
                    break;
            }

            GlobalKeyNotifier.PressKey(args.VirtualKey, _isPressCtrl, _isPressShift, IsInputing);
        }

        private void Window_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Control:
                    _isPressCtrl = false;
                    break;
                case VirtualKey.Shift:
                    _isPressShift = false;
                    break;
            }

            GlobalKeyNotifier.ReleaseKey(args.VirtualKey, _isPressCtrl, _isPressShift, IsInputing);
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            await ShowErrorDialog(e.Exception);
        }

        private async void OnSynchronizationContextUnhandledException(object sender, WinRTExceptions.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            await ShowErrorDialog(e.Exception);
        }
    }
}
