﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cireon.Audio;

namespace yatl
{
    /// <summary>
    /// Abstract class for all kinds of sound events
    /// </summary>
    abstract class SoundEvent
    {
        public double StartTime;

        public SoundEvent(double startTime)
        {
            this.StartTime = startTime;
        }

        public void AddOffset(double offsetTime)
        {
            this.StartTime += offsetTime;
        }

        public void MultiplyOffset(double multiplier)
        {
            this.StartTime *= multiplier;
        }

        public abstract void Execute();
    }

    class NoteOn : SoundEvent
    {
        public Note Note;
        public SoundFile Instrument;
        public Source Source = null;
        public double Volume;

        public NoteOn(double startTime, Note note, SoundFile Instrument, double volume)
            : base(startTime)
        {
            this.Note = note;
            this.Volume = volume;
            this.Instrument = Instrument;
        }

        public override void Execute()
        {
            this.Source = this.Instrument.GenerateSource();
            this.Source.Volume = (float) (this.Volume * 0.4); // Fix jitter
            this.Source.Pitch = (float)(this.Note.Frequency / 261.6); //130.8);
            this.Source.Play();
        }
    }

    class NoteOff : SoundEvent
    {
        NoteOn noteOn;

        public NoteOff(double startTime, NoteOn noteOn)
            : base(startTime)
        {
            this.noteOn = noteOn;
        }

        public override void Execute()
        {
            var source = this.noteOn.Source;
            if (source == null)
                throw new Exception("NoteOn must be executed before NoteOff.");
            if (!source.FinishedPlaying && !source.Disposed)
                source.Stop();
        }
    }
}