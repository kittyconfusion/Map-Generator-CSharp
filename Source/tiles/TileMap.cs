using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Map_Generator_CSharp.Source.tiles;
using Map_Generator_CSharp.Source.util;

namespace Map_Generator_CSharp.Source.tiles;

class TileMap
{
    public struct GenerationSettings
    {

        // Tile generation
        public int width;
        public int height;
        public float largestOctave; // How big one perlin cell is in the biggest octave
        public int perlinOctaves;
        public float octaveScale;
        public bool mountainType;


        // Mountain generation
        public float mountainMinHeight;
        public float mountainMaxHeight;
        public float snowMultiplier;

        //New Settings
        public float mountainRangeChancePerTile;
        public int mountainRangeLengthLow;
        public int mountainRangeLengthHigh;
        public float mountainRangeDistanceLow;
        public float mountainRangeDistanceHigh;
        public float mountainRangeRandomOffset;
        public float mountainRangeMaxAngleChange;
        public float mountainRangeShrink;

        //Old Settings
        public float mountainPerlinScale; // How big one perlin cell is for mountains
        public float mountainThreshold; // What's the perlin threshold for generating mountains? (smaller value = rarer)
        public float mountainChance; // What's the chance of a mountain in a tile within that threshold (smaller value = rarer)


        // Mountain smoothing
        public float mountainSmoothThreshold;
        public int mountainSmoothPasses;
        public float mountainDistributionLow; // How much to smooth (min and max)
        public float mountainDistributionHigh;

        // Ocean settings
        public float seaLevel;
        public bool canMountainsFormInOcean;
        public float beachThreashold;
        public float oceanHumidityMultiplier;
        public float seafloorThreshold;

        // Humidity smoothing
        public float humiditySmoothThreshold;
        public int humiditySmoothPasses;
        public float humidityDistributionLow; // How much to smooth (min and max)
        public float humidityDistributionHigh;

        // Forest generation
        public float forestPerlinScale; // How big one perlin cell is for forests
        public float forestChance; // What's the chance of a forest in a tile within that threshold (smaller value = rarer)
        public float forestPerlinWeight; // How much does the perlin noise matter?
        public float forestHumidityWeight; // How much does it value humidity?

        public GenerationSettings(int v1, int v2, double v3, int v4, int v5, bool v6, double v7, double v8, double v9, double v10, int v11, int v12, 
            double v13, double v14, int v15, double v16, int v17, int v18, double v19, double v20, double v21, int v22, double v23, double v24, double v25, 
            bool v26, double v27, double v28, double v29, double v30, int v31, double v32, double v33, int v34, float v35, double v36, double v37) : this()
        {
            width = v1; height = v2; largestOctave = (float)v3; perlinOctaves = v4; octaveScale = v5; mountainType = v6;
            mountainMinHeight = (float)v7; mountainMaxHeight = (float)v8; snowMultiplier = (float)v9;
            mountainRangeChancePerTile = (float)v10; mountainRangeLengthLow = v11; mountainRangeLengthHigh = v12;
            mountainRangeDistanceLow = (float)v13; mountainRangeDistanceHigh = (float)v14; mountainRangeRandomOffset = v15;
            mountainRangeMaxAngleChange = (float)v16; mountainRangeShrink = v17;
            mountainPerlinScale = v18; mountainThreshold = (float)v19; mountainChance = (float)v20;
            mountainSmoothThreshold = (float)v21; mountainSmoothPasses = v22; mountainDistributionLow = (float)v23; mountainDistributionHigh = (float)v24;
            seaLevel = (float)v25; canMountainsFormInOcean = v26; beachThreashold = (float)v27; oceanHumidityMultiplier = (float)v28; seafloorThreshold = (float)v29;
            humiditySmoothThreshold = (float)v30; humiditySmoothPasses = v31; humidityDistributionLow = (float)v32; humidityDistributionHigh = (float)v33;
            forestPerlinScale = (float)v34; forestChance = v35; forestPerlinWeight = (float)v36; forestHumidityWeight = (float)v37;
        }
    }
    private Tile[] tileMap;
    private GenerationSettings settings;

    private int seed;
    public int getSeed() { return seed; }
    private static int randomInitialSeed()
    {
        Random rand = new Random();
        return rand.Next();
    }
    public TileMap(GenerationSettings settings) : this(settings, randomInitialSeed()) {}

    public TileMap(GenerationSettings gs, int seed) {
        settings = gs;
        tileMap = new Tile[settings.width * settings.height];
    
        for (int i = 0; i<settings.width*settings.height; i++) {
            tileMap[i] = new Tile(this);
        }

        this.seed = seed;

    generateMap(seed);

    }
public void generateMap(int seed)
    {
        Console.WriteLine("Generating new map with seed " + seed);
        Random rand = new Random(seed);

        Console.WriteLine("Creating the earth");
        generateTileAttributes(rand);

        Console.WriteLine("Raising mountains");
        if (settings.mountainType)
        {
            generateMountains(rand);
        }
        else
        {
            generateMountainsOld(rand);
        }

        Console.WriteLine("Flattening mountains");
        for (int i = 0; i < settings.mountainSmoothPasses; i++)
        {
            smoothMountains(rand);
            Console.WriteLine("> " + (i + 1) + " smooth passes completed");
        }
        Console.WriteLine("Smoothing complete");

        if (!settings.canMountainsFormInOcean)
        {
            Console.WriteLine("Shearing cliffs");
            makeSeaCliffs(rand);
        }

        Console.WriteLine("Designating beaches");
        designateBeaches();

        // Gives ocean tiles humidity
        Console.WriteLine("Wetting ocean");
        makeSeaWet();

        // Essentially smooths the humidity. Uses the same settings as foothills
        Console.WriteLine("Dispersing humidity");
        for (int i = 0; i < settings.humiditySmoothPasses; i++)
        {
            smoothHumidity(rand);
            Console.WriteLine("> " + (i + 1) + " dispersion passes completed");
        }

        Console.WriteLine("Growing forests");
        generateForests(rand);
    }
    public void rerenderTiles(int displayMode)
    {
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                getTile(x, y).renderColor(displayMode);
            }
        }
    }
    private void generateTileAttributes(Random rand)
    {
        // Create generators
        PerlinNoiseGenerator[] elevationGenerators = getGeneratorList(rand),
                               temperatureGenerators = getGeneratorList(rand),
                               humidityGenerators = getGeneratorList(rand);
        

        double attributeScale = (1 - 1 / settings.octaveScale) / (1 - Math.Pow(settings.octaveScale, -1 * settings.perlinOctaves));

        // Assign to tiles
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                updateTileAttributes(x, y, "elevation", elevationGenerators, attributeScale);
                updateTileAttributes(x, y, "temperature", temperatureGenerators, attributeScale);
                updateTileAttributes(x, y, "humidity", humidityGenerators, attributeScale);
            }
        }
    }
    private PerlinNoiseGenerator[] getGeneratorList(Random rand)
    {
        var generators = new PerlinNoiseGenerator[settings.perlinOctaves];

        double size = 1.0 / settings.largestOctave;
        for (int i = 0; i < settings.perlinOctaves; i++)
        {
            generators[i] = new PerlinNoiseGenerator(rand, (int)Math.Ceiling(settings.width * size), (int)Math.Ceiling(settings.height * size));
            size *= settings.octaveScale;
        }

        return generators;

    }
    private void updateTileAttributes(int x, int y, string attr, PerlinNoiseGenerator[] gens, double attrScale)
    {
        Tile t = getTile(x, y);

        int octaveScale = 1;
        for (int i = 0; i < settings.perlinOctaves; i++)
        {
            t.setAttribute(attr,
                t.getAttribute(attr) +
                    gens[i].noise(
                        (double)(x * octaveScale) / settings.largestOctave,
                        (double)(y * octaveScale) / settings.largestOctave
                    ) / octaveScale
            );

            octaveScale = (int)(octaveScale * settings.octaveScale);
        }

        t.setAttribute(attr, t.getAttribute(attr) * attrScale); // Range [0,1]
    }
    private void generateMountains(Random rand)
    {
        for (int y = 0; y < settings.height; y++)
        {
            for (int x = 0; x < settings.width; x++)
            {
                if (rand.NextDouble() < settings.mountainRangeChancePerTile)
                {
                    generateMountainRange(rand, x, y); //Create a new mountain range at that tile
                }
            }
        }
    }

    //https://gist.github.com/tansey/1444070 
    private static double normalDistribution(Random random, double minn, double maxx)
    {
        double min = Math.Min(minn, maxx);
        double max = Math.Max(minn, maxx);
        // The method requires sampling from a uniform random of (0,1]
        // but Random.NextDouble() returns a sample of [0,1).
        double x1 = 1 - random.NextDouble();
        double x2 = 1 - random.NextDouble();

        double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
     
        return Math.Clamp((double)(y1 * ((max - min) / 6.0) + (min + (0.5 * (max - min)))), min, max);
    }
    private void generateMountainRange(Random rand, int xStart, int yStart)
    {
        //std::uniform_real_distribution<double> uniformDistribution(0, 1);
        //std::normal_distribution<double> normalDistribution(0, 1);
        //std::normal_distribution<double> lengthDistribution(settings.mountainRangeLengthLow + ((double)(settings.mountainRangeLengthHigh -
        //settings.mountainRangeLengthLow) / 2), ((double)(settings.mountainRangeLengthHigh - settings.mountainRangeLengthLow) / 2));

        double lenLow = settings.mountainRangeLengthLow + ((double)(settings.mountainRangeLengthHigh - settings.mountainRangeLengthLow) / 2);
        double lenHigh = ((double)(settings.mountainRangeLengthHigh - settings.mountainRangeLengthLow) / 2);

        double rangeX = xStart;
        double rangeY = yStart;
        int startingLength = (int)Math.Max(Math.Round(normalDistribution(rand, lenLow, lenHigh)), 0);
        int lengthLeft = startingLength;
        double angle = rand.NextDouble() * 2 * Math.PI;
        double angleChange = normalDistribution(rand, 0, 1) * settings.mountainRangeMaxAngleChange;
        double speed = settings.mountainRangeDistanceLow + (rand.NextDouble() * (settings.mountainRangeDistanceHigh - settings.mountainRangeDistanceLow));
        double xVelocity = Math.Cos(angle) * speed;
        double yVelocity = Math.Sin(angle) * speed;

        //std::uniform_real_distribution<double> mtnHeight(settings.mountainMinHeight, settings.mountainMaxHeight -1); //I was unsure how to recreate this with a 0-1 distribution.
        double height, heightDivisor;

        while (lengthLeft > 0)
        {
            //Determine a location to place the next mountain. 
            double tryX = rangeX + (normalDistribution(rand, 0, 1) * settings.mountainRangeRandomOffset);
            double tryY = rangeY + (normalDistribution(rand, 0, 1) * settings.mountainRangeRandomOffset);

            //Check if able to place a mountain at location
            if (inBounds((int)(tryX + 0.5), (int)(tryY + 0.5)))
            {
                Tile t = getTile((int)(tryX + 0.5), (int)(tryY + 0.5));
                if (!(t.hasFeature("mountain")) && (!(t.isOcean()) || settings.canMountainsFormInOcean))
                {
                    //Place a mountain
                    t.addFeature("mountain");
                    //This is messy code. But at least it technicallly is functional. :)
                    heightDivisor = settings.mountainRangeShrink * 2 * Math.Abs(((double)lengthLeft / startingLength) - 0.5); // Range [0, mountainRangeShrink]
                    height = (settings.mountainMinHeight + (rand.NextDouble() * (settings.mountainMaxHeight - settings.mountainMinHeight - 1))) / Math.Max(heightDivisor, 1);
                    t.setAttribute("elevation", t.getAttribute("elevation") + height);
                }
            }
            //Update current range location and angle
            lengthLeft--;
            angle += angleChange;
            xVelocity = Math.Cos(angle) * speed;
            yVelocity = Math.Sin(angle) * speed;
            rangeX += xVelocity;
            rangeY += yVelocity;
        }

    }
    private bool inBounds(int x, int y)
    {
        return x >= 0 && x < settings.width && y >= 0 && y < settings.height;
    }

    [Obsolete]
    private void generateMountainsOld(Random rand)
    {
    }

    private void getTileSurroundingMaxAndMin(double[] buffer, int x, int y, bool includeSeaTiles, ref float max, ref float min)
    {
        for (int xmod = -1; xmod <= 1; xmod++)
        {
            for (int ymod = -1; ymod <= 1; ymod++)
            {
                int currentx = xmod + x;
                int currenty = ymod + y;
                if (currentx >= 0 && currenty >= 0 && currentx < settings.width && currenty < settings.height)
                { //If tile in bounds of map
                    double tempValue = buffer[currenty * settings.width + currentx];

                    if (includeSeaTiles || !(getTile(currentx, currenty).isOcean()))
                    {
                        if (tempValue > max)
                        {
                            max = (float)tempValue;
                        }
                        if (tempValue < min)
                        {
                            min = (float)tempValue;
                        }
                    }
                }
            }
        }
    }

    private void smoothMountains(Random rand)
    {
        double[] heightBuffer = new double[settings.width * settings.height]; //Needed so that modified data does not interfere with currently generating data
        for (int y = 0; y < settings.height; y++)
        {
            for (int x = 0; x < settings.width; x++)
            {
                heightBuffer[y * settings.width + x] = getTile(x, y).getAttribute("elevation");
            }
        }
        //std::uniform_real_distribution<double> foothillDistribution(settings.mountainDistributionLow, settings.mountainDistributionHigh);
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                Tile currentTile = getTile(x, y);
                float min = settings.mountainMaxHeight, max = 0;

                // Do not perform modifications on tiles with the mountain attribute
                if (!(currentTile.hasFeature("mountain")))
                {// && !(currentTile.hasFeature("foothill"))) {

                    // Finds the minimum and maximum heights of the adjacent 8 tiles and determines the lowest and greatest elevation
                    getTileSurroundingMaxAndMin(heightBuffer, x, y, settings.canMountainsFormInOcean, ref max, ref min);

                    // If a modification is necessary smooth out the tile
                    if (max / min >= settings.mountainSmoothThreshold && (settings.canMountainsFormInOcean || !(currentTile.isOcean())))
                    {
                        currentTile.addFeature("foothill");
                        currentTile.setAttribute("elevation", ((rand.NextDouble() * (settings.mountainDistributionHigh - settings.mountainDistributionLow)
                            + settings.mountainDistributionLow) - 0.05) * (max - min) + min);
                    }

                }
            }
        }
    }
    private void makeSeaCliffs(Random rand)
    {
        double[] heightBuffer = new double[settings.width * settings.height]; //Needed so that modified data does not interfere with currently generating data
        for (int y = 0; y < settings.height; y++)
        {
            for (int x = 0; x < settings.width; x++)
            {
                heightBuffer[y * settings.width + x] = getTile(x, y).getAttribute("elevation");
            }
        }
        //std::uniform_real_distribution<double> foothillDistribution(settings.mountainDistributionLow, settings.mountainDistributionHigh);
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                Tile currentTile = getTile(x, y);
                float min = settings.mountainMaxHeight, max = 0;

                if (currentTile.isOcean())
                {

                    // Finds the minimum and maximum heights of the adjacent 8 tiles and determines the lowest and greatest elevation
                    getTileSurroundingMaxAndMin(heightBuffer, x, y, true, ref max, ref min);

                    // If a it is a valid sea tile, and near mountains
                    if (max >= 0.95)
                    {
                        currentTile.addFeature("sea_cliff");
                        currentTile.setAttribute("elevation", ((rand.NextDouble() * (settings.mountainDistributionHigh - settings.mountainDistributionLow)
                            + settings.mountainDistributionLow) - 0.05) * (max - min) + min);
                    }
                }
            }
        }
    }
    private void designateBeaches()
    {
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                Tile currentTile = getTile(x, y);

                if (currentTile.getAttribute("elevation") >= settings.seaLevel && settings.seaLevel + settings.beachThreashold > currentTile.getAttribute("elevation"))
                {
                    currentTile.addFeature("beach");
                }
            }
        }
    }
    private void makeSeaWet()
    {
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                Tile currentTile = getTile(x, y);

                if (currentTile.getAttribute("elevation") < settings.seaLevel)
                {
                    currentTile.addFeature("sea");
                    currentTile.setAttribute("humidity", Math.Min(1, settings.oceanHumidityMultiplier * currentTile.getAttribute("humidity")));
                }
            }
        }
    }
    private void smoothHumidity(Random rand)
    {
        double[] humidityBuffer = new double[settings.width * settings.height]; //Needed so that modified data does not interfere with currently generating data
        for (int y = 0; y < settings.height; y++)
        {
            for (int x = 0; x < settings.width; x++)
            {
                humidityBuffer[y * settings.width + x] = getTile(x, y).getAttribute("humidity");
            }
        }
        //std::uniform_real_distribution<double> distribution(settings.humidityDistributionLow, settings.humidityDistributionHigh);
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {
                Tile currentTile = getTile(x, y);
                float min = 1, max = 0;

                if (!(currentTile.isOcean()) && !(currentTile.hasFeature("mountain") || currentTile.hasFeature("foothill")))
                {

                    // Finds the minimum and maximum heights of the adjacent 8 tiles and determines the lowest and greatest elevation
                    getTileSurroundingMaxAndMin(humidityBuffer, x, y, currentTile.hasFeature("beach"), ref max, ref min);

                    // If a modification is necessary smooth out the tile
                    if (max / min >= settings.humiditySmoothThreshold)
                    {
                        currentTile.setAttribute("humidity", (rand.NextDouble() * (settings.mountainDistributionHigh - settings.mountainDistributionLow)
                            + settings.mountainDistributionLow) * (max - min) + min);
                    }
                }
            }
        }
    }
    private void generateForests(Random rand)
    {
        var perlin = new PerlinNoiseGenerator(rand, (int)Math.Ceiling(settings.width/ settings.forestPerlinScale), (int)Math.Ceiling(settings.height / settings.forestPerlinScale));

        //std::uniform_real_distribution<double> forestDist(0,1);

        Tile t;
        double perlinVal, randVal, chance, humidityChance;
        for (int x = 0; x < settings.width; x++)
        {
            for (int y = 0; y < settings.height; y++)
            {

                t = getTile(x, y);
                if (!(t.isOcean()) && t.noFeatures())
                {

                    perlinVal = perlin.noise((double)x / settings.forestPerlinScale, (double)y / settings.forestPerlinScale);

                    //if(perlinVal < settings.forestThreshold) {

                    chance = Math.Pow(perlinVal, settings.forestPerlinWeight) * settings.forestChance * Math.Pow(t.getAttribute("humidity"), settings.forestHumidityWeight);
                    randVal = rand.NextDouble();
                    if (randVal < chance)
                    {
                        t.addFeature("forest");
                    }
                    //}
                }
            }
        }
    }
    public Tile getTile(int x, int y) 
    {
        return tileMap[y * settings.width + x] ;
    }

    public int getWidth() {
        return settings.width;
    }

    public int getHeight() {
        return settings.height;
    }

    public GenerationSettings getSettings()
    {
        return settings;
    }
}

/*
TileMap::~TileMap() {
    for (int i = 0; i < settings.width * settings.height; i++)
    {
        delete tileMap[i];
    }
    delete[] tileMap;
}
*/