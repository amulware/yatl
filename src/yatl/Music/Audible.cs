using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Cireon.Audio;
using yatl.Environment;

namespace yatl
{
    /// <summary>
    /// Abstract class for objects that can be rendered to sound events
    /// </summary>
    abstract class Audible
    {
        public abstract double Duration { get; }

        public abstract IEnumerable<SoundEvent> Render(MusicParameters parameters, Instrument instrument);
    }

    /// <summary>
    /// Atomic music object
    /// </summary>
    class Note : Audible
    {
        Pitch pitch;
        double duration;

        public override double Duration { get { return this.duration; } }
        public double Frequency { get { return this.pitch.Frequency; } }

        public Note(int duration, Pitch pitch)
        {
            this.duration = duration;
            this.pitch = pitch;
        }

        public override IEnumerable<SoundEvent> Render(MusicParameters parameters, Instrument instrument)
        {
            double volume = Math.Max(0.3, parameters.Tension);
            var start = new NoteOn(0, this, instrument, volume);
            //var start = instrument.Play(volume, this.pitch.Frequency);
            yield return start;

            var end = new NoteOff(this.Duration, start);
            //var end = instrument.Stop(start, duration);
            yield return end;
        }

        public override string ToString()
        {
            return this.Duration.ToString() + " " + this.pitch.ToString();
        }
    }

    /// <summary>
    /// Set of music objects that sound subsequently
    /// </summary>
    class Serial : Audible
    {
        Audible[] content;
        public override double Duration { get { return this.content.Sum(o => o.Duration); } }

        public Serial(Audible[] content)
        {
            this.content = content;
        }

        public override IEnumerable<SoundEvent> Render(MusicParameters parameters, Instrument instrument)
        {
            double time = 0;

            foreach (var child in this.content) {
                foreach (var soundEvent in child.Render(parameters, instrument)) {
                    soundEvent.AddOffset(time);
                    yield return soundEvent;
                }
                time += child.Duration;
            }
        }

        public override string ToString()
        {
            return string.Join(" ", this.content.Select(obj => obj.ToString()));
        }
    }

    /// <summary>
    /// Set of music objects that sound simultaneously
    /// </summary>
    class Parallel : Audible
    {
        Audible[] content;
        double durationMultiplier;
        double innerDuration;

        public override double Duration { get { return this.innerDuration * this.durationMultiplier; } }

        public Parallel(Audible[] content, int durationMultiplier = 1)
        {
            if (content.Length == 0)
                throw new Exception("No empty content allowed.");

            double duration = content[0].Duration;
            bool allSameDuration = content.All(o => o.Duration == duration);
            if (!allSameDuration)
                throw new Exception("Not every musicobject has the same duration.");

            this.durationMultiplier = durationMultiplier;
            this.innerDuration = duration;
            this.content = content;
        }

        public override IEnumerable<SoundEvent> Render(MusicParameters parameters, Instrument instrument)
        {
            int number = this.content.Length;
            //number = Math.Max(1, (int)(number * (1 - parameters.Tension)));

            foreach (var child in this.content.Take(number)) {
                foreach (var soundEvent in child.Render(parameters, instrument)) {
                    soundEvent.MultiplyOffset(this.durationMultiplier);
                    yield return soundEvent;
                }
            }
        }

        public override string ToString()
        {
            return this.durationMultiplier.ToString() + "{" + string.Join(",", this.content.Select(obj => obj.ToString())) + "}";
        }
    }
}
