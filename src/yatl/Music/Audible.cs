using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Cireon.Audio;
using yatl.Environment;

namespace yatl
{
    /// <summary>
    /// Configuration object for specifying how things should be rendered
    /// </summary>
    class RenderParameters
    {
        public MusicParameters MusicParameters;
        public Instrument Instrument;

        public RenderParameters(MusicParameters musicParameters, Instrument instrument)
        {
            this.MusicParameters = musicParameters;
            this.Instrument = instrument;
        }
    }

    /// <summary>
    /// Abstract class for objects that can be rendered to sound events
    /// </summary>
    abstract class Audible
    {
        public abstract double Duration { get; }

        public abstract IEnumerable<SoundEvent> Render(RenderParameters parameters, double start = 0);
    }

    /// <summary>
    /// Atomic music object
    /// </summary>
    class Note : Audible
    {
        public Pitch Pitch;
        double duration;

        public override double Duration { get { return this.duration; } }
        public double Frequency { get { return this.Pitch.Frequency; } }

        public Note(double duration, Pitch pitch)
        {
            this.duration = duration;
            this.Pitch = pitch;
        }

        public override IEnumerable<SoundEvent> Render(RenderParameters parameters, double start)
        {
            var on = new NoteOn(start, parameters.Instrument, this.Frequency);
            yield return on;
            var off = new NoteOff(start + this.Duration, on);
            yield return off;

            // Extra octave depends on density
            //if (MusicManager.Random.NextDouble() < parameters.Density) {
            //    var startOctave = new NoteOn(0, parameters.Instrument, this.Frequency * 2, parameters.Volume);
            //    yield return startOctave;
            //    var endOctave = new NoteOff(this.Duration, startOctave);
            //    yield return endOctave;
            //}
        }

        public override string ToString()
        {
            return this.Duration.ToString() + " " + this.Pitch.ToString();
        }
    }

    /// <summary>
    /// Set of music objects that sound subsequently
    /// </summary>
    class Serial : Audible
    {
        public Note[] Content;
        public override double Duration { get { return this.Content.Sum(o => o.Duration); } }

        public Serial(Audible[] content)
        {
            this.Content = Array.ConvertAll(content, item => (Note)item);
        }

        public override IEnumerable<SoundEvent> Render(RenderParameters parameters, double start = 0)
        {
            foreach (var child in this.Content) {
                foreach (var soundEvent in child.Render(parameters, start)) {
                    yield return soundEvent;
                }
                start += child.Duration;
            }
        }

        public IEnumerable<Note> GetRange(double start, double end)
        {
            double time = 0;
            foreach (var note in this.Content) {
                if (time >= start && time <= end)
                    yield return note;
                time += note.Duration;
            }
        }

        public IEnumerable<Note> GetPosition(double position)
        {
            double time = 0;
            foreach (var note in this.Content) {
                if (time == position)
                    yield return note;
                time += note.Duration;
            }
        }

        public override string ToString()
        {
            return string.Join(" ", this.Content.Select(obj => obj.ToString()));
        }
    }

    /// <summary>
    /// Set of music objects that sound simultaneously
    /// </summary>
    class Parallel : Audible
    {
        public Serial[] Content;
        double durationMultiplier;
        double innerDuration;

        public override double Duration { get { return this.innerDuration * this.durationMultiplier; } }

        public Parallel(Audible[] content, double durationMultiplier = 1)
        {
            if (content.Length == 0)
                throw new Exception("No empty content allowed.");

            double duration = content[0].Duration;
            bool allSameDuration = content.All(o => o.Duration == duration);
            if (!allSameDuration)
                throw new Exception("Not every musicobject has the same duration.");

            this.durationMultiplier = durationMultiplier;
            this.innerDuration = duration;
            this.Content = Array.ConvertAll(content, item => (Serial)item);
        }

        public override IEnumerable<SoundEvent> Render(RenderParameters parameters, double start = 0)
        {
            // Number of voices depends on density
            int number = Math.Max(2, (int)Math.Round(this.Content.Length * parameters.MusicParameters.Tension));

            foreach (var child in this.Content.Take(number)) {
                foreach (var soundEvent in child.Render(parameters, start)) {
                    soundEvent.MultiplyOffset(this.durationMultiplier);
                    yield return soundEvent;
                }
            }
        }

        public override string ToString()
        {
            return this.durationMultiplier.ToString() + "{" + string.Join(",", this.Content.Select(obj => obj.ToString())) + "}";
        }
    }
}
