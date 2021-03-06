using System;
using System.Linq;
using yatl.Environment.Tilemap.Hexagon;
using yatl.Utilities;
using Extensions = yatl.Environment.Tilemap.Hexagon.Extensions;
using GeneratingTile = yatl.Environment.Tilemap.Hexagon.Tile<yatl.Environment.Level.Generation.GeneratingTileInfo>;

namespace yatl.Environment.Level.Generation
{
    sealed class LevelGenerator
    {

        public static LevelGenerator NewDefault { get { return new LevelGenerator().WithDefaultSettings; } }

        public int Radius { get; set; }
        public float OpennessCore { get; set; }
        public float OpennessRim { get; set; }

        public float MinCorridorWidth { get; set; }
        public float MaxCorridorWidth { get; set; }

        public bool LogGenerationDetails { get; set; }
        public bool MuteAllOutput { get; set; }

        public LevelGenerator WithDefaultSettings
        {
            get
            {
                this.Radius = Settings.Game.Level.Radius;
                this.OpennessRim = 0f;
                this.OpennessCore = 0.5f;

                this.MinCorridorWidth = 0.2f;
                this.MaxCorridorWidth = 0.75f;

                return this;
            }
        }

        public LevelGenerator Verbose
        {
            get { this.LogGenerationDetails = true; return this; }
        }
        public LevelGenerator Silent
        {
            get { this.MuteAllOutput = true; return this; }
        }

        public Tilemap<TileInfo> Generate()
        {
            if (!this.MuteAllOutput)
            {
                Console.WriteLine("------------------------------");
                Console.WriteLine("Generating level with radius {0} ({1} tiles)...", this.Radius, Extensions.TileCountForRadius(this.Radius));
            }

            var timer = SteppedStopwatch.StartNew();
            timer.Mute = this.MuteAllOutput || !this.LogGenerationDetails;

            #region initialise

            var tempMap = new Tilemap<GeneratingTileInfo>(this.Radius);

            timer.WriteStepToConsole("Made mutable tilemap ......... {0}");

            var tiles = tempMap.ToList();

            timer.WriteStepToConsole("Enumerated tiles ............. {0}");

            //var tilesSpiral = tempMap.TilesSpiralOutward.ToList();
            //var tilesDistance = tiles.OrderBy(
            //    t => (Settings.Game.Level.TileToPosition * t.Xy).LengthSquared
            //    ).ToList();

            //var infos = new List<GeneratingTileInfo>(tiles.Count);

            foreach (var tile in tiles)
            {
                var info = new GeneratingTileInfo();
                //infos.Add(info);
                tempMap[tile] = info;
            }
            timer.WriteStepToConsole("Filled mutable tiles ......... {0}");

            #endregion

            var centerTile = new GeneratingTile(tempMap, 0, 0);

            #region open walls

            centerTile.OpenRandomSpanningTree(this.MinCorridorWidth, this.MaxCorridorWidth);

            timer.WriteStepToConsole("Opened random spanning tree .. {0}");

            if (this.OpennessCore > 0 || this.OpennessRim > 0)
            {
                tiles.OpenRandomWalls(t => GameMath.Lerp(
                    this.OpennessCore, this.OpennessRim, (float)t.Radius / this.Radius),
                    this.MinCorridorWidth, this.MaxCorridorWidth);
                timer.WriteStepToConsole(
                    string.Format("Opened {0:0}%-{1:0}% random walls ", this.OpennessCore * 100, this.OpennessRim * 100)
                    .PadRight(30, '.') + " {0}");
            }

            #endregion

            #region make level geometry

            tiles.MakeWalls();

            timer.WriteStepToConsole("Generated level walls ........ {0}");

            tiles.MakeFloors();

            timer.WriteStepToConsole("Generated level floors ....... {0}");

            #endregion

            #region light level

            var lightBuffer1 = new Tilemap<float>(this.Radius);
            var lightBuffer2 = new Tilemap<float>(this.Radius);

            lightBuffer1.SetRandom();
            lightBuffer1[0, 0] = 0.5f;
            lightBuffer1.DilateTo(tempMap, lightBuffer2);
            foreach (var tile in lightBuffer2.Where(t => t.Radius == this.Radius))
                lightBuffer2[tile] = 1f;
            lightBuffer2.SmoothTo(tempMap, lightBuffer1);
            //lightBuffer1.SmoothTo(tempMap, lightBuffer2);

            lightBuffer1.SetBrightness(tempMap);

            timer.WriteStepToConsole("Generated level lightness .... {0}");

            #endregion

            #region build

            var result = tempMap.Build();

            timer.WriteStepToConsole("Build immutable tilemap ...... {0}");

            #endregion

            if (!this.MuteAllOutput)
            {
                timer.Mute = false;
                timer.WriteTotalToConsole("Generated level in {0}");
                Console.WriteLine("------------------------------");
            }

            return result;
        }

    }
}
