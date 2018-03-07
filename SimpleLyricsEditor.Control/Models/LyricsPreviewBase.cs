﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SimpleLyricsEditor.DAL;
using SimpleLyricsEditor.Events;

namespace SimpleLyricsEditor.Control.Models
{
    public abstract class LyricsPreviewBase : UserControl
    {
        private static readonly ObservableCollection<Lyric> EmptyLyricList = new ObservableCollection<Lyric>();

        public static readonly DependencyProperty LyricsProperty = DependencyProperty.Register(
            nameof(Lyrics), typeof(IList<Lyric>), typeof(LyricsPreviewBase), new PropertyMetadata(EmptyLyricList));

        public static readonly DependencyProperty CurrentLyricProperty = DependencyProperty.Register(
            nameof(CurrentLyric), typeof(Lyric), typeof(LyricsPreviewBase), new PropertyMetadata(Lyric.Empty));
        
        protected Lyric BackLyric;
        protected int NextIndex;

        public IList<Lyric> Lyrics
        {
            get => (IList<Lyric>) GetValue(LyricsProperty);
            set => SetValue(LyricsProperty, value);
        }

        public Lyric CurrentLyric
        {
            get => (Lyric) GetValue(CurrentLyricProperty);
            set
            {
                BackLyric = GetValue(CurrentLyricProperty) as Lyric;
                SetValue(CurrentLyricProperty, value); 
                Refreshed?.Invoke(this, new LyricsPreviewRefreshEventArgs(value));
            }
        }

        public event TypedEventHandler<LyricsPreviewBase, LyricsPreviewRefreshEventArgs> Refreshed;

        protected bool CanPreview => IsEnabled && Visibility == Visibility.Visible && Lyrics != null && Lyrics.Any();
        
        public void RefreshLyric(TimeSpan position)
        {
            if (!CanPreview)
                return;

            if (NextIndex >= Lyrics.Count)
                NextIndex = 0;

            long currentTimeTicks = position.Ticks;
            Lyric nextLyric = Lyrics[NextIndex];
            long nextTimeTicks = nextLyric.Time.Ticks;
            long nextEndTimeTicks = nextTimeTicks + TimeSpan.TicksPerSecond;

            if (currentTimeTicks >= nextTimeTicks && currentTimeTicks <= nextEndTimeTicks)
            {
                NextIndex++;
                CurrentLyric = nextLyric;
            }
            else if (currentTimeTicks > nextEndTimeTicks)
                Reposition(position);
        }

        public void Reposition(TimeSpan position)
        {
            if (!CanPreview)
                return;
            
            if (position.CompareTo(Lyrics.First().Time) <= 0)
            {
                NextIndex = 0;
                CurrentLyric = Lyric.Empty;
                return;
            }

            if (position.CompareTo(Lyrics.Last().Time) >= 0)
            {
                NextIndex = 0;
                CurrentLyric = Lyrics.Last();
                return;
            }

            for (var i = 0; i < Lyrics.Count; i++)
            {
                if (position.CompareTo(Lyrics[i].Time) < 0)
                {
                    NextIndex = i;
                    CurrentLyric = Lyrics[i - 1];
                    break;
                }
            }
        }
    }
}