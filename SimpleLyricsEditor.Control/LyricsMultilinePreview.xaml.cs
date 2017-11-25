﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SimpleLyricsEditor.Control.Models;
using SimpleLyricsEditor.DAL;
using SimpleLyricsEditor.Events;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SimpleLyricsEditor.Control
{
    public sealed partial class LyricsMultilinePreview : LyricsPreviewBase
    {
        private static readonly object InterpolationLocker = new object();
        
        private int _interpolation;

        public LyricsMultilinePreview()
        {
            this.InitializeComponent();
            _interpolation = (int) ComputeInterpolation();
        }

        public event ItemClickEventHandler ItemClick;

        private double ComputeInterpolation()
        {
            return Main_ListView.ActualHeight / 44 / 2;
        }

        private void LyricsMultilinePreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (InterpolationLocker)
                _interpolation = (int) ComputeInterpolation();
        }

        private void LyricsMultilinePreview_Refreshed(LyricsPreviewBase sender, LyricsPreviewRefreshEventArgs args)
        {
            foreach (Lyric lyric in Lyrics.Where(l => l.IsSelected))
                lyric.IsSelected = false;

            if (args.CurrentLyric.Equals(Lyric.Empty))
            {
                Main_ListView.ScrollIntoView(Lyrics[0]);
                return;
            }

            args.CurrentLyric.IsSelected = true;
            
            int itemId = Lyrics.IndexOf(args.CurrentLyric);

            Main_ListView.ScrollIntoView(Lyrics[itemId - _interpolation > 0 ? itemId - _interpolation : 0], ScrollIntoViewAlignment.Leading);
        }

        private void Main_ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ItemClick?.Invoke(this, e);
        }
    }
}
