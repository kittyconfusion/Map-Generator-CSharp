using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using static Map_Generator_CSharp.Source.tiles.TileMap;

namespace Map_Generator_CSharp.Source.tiles;

//#include "Tile.hpp"
//#include "TileMap.hpp"

//#include <iostream>
//#include <math.h>

//#include <SFML/Graphics.hpp>


class Tile
{


    private Dictionary<string, double> attributes = new Dictionary<string, double>();
    private LinkedList<string> features = new LinkedList<string>();
    private Color colorCache;

    private bool needToRenderColor = true;

    private TileMap tileMap;

    public Tile(TileMap parentMap)
    {
        tileMap = parentMap;

        attributes.Add("elevation", 0);
        attributes.Add("temperature", 0);
        attributes.Add("humidity", 0);
    }

    private double colorCurve(double input, double steepness)
    {
        double seaLevel = tileMap.getSettings().seaLevel;
        return Math.Log(steepness * (input - seaLevel) + 1) / Math.Log(steepness + 1 - steepness * seaLevel);
    }
    public Color getColor(int displayMode)
    {
        if (needToRenderColor) { renderColor(displayMode); needToRenderColor = false; }
        return colorCache;
    }

    public bool isOcean()
    {
        return getAttribute("elevation") < tileMap.getSettings().seaLevel;
    }

    public double getAttribute(string attr)
    {
        return attributes[attr];
    }
    public void setAttribute(string attr, double val)
    {
        attributes[attr] = val;
    }

    public void addFeature(string feat)
    {
        features.AddLast(feat);
    }
    public void removeFeature(string feat)
    {
        features.Remove(feat);
    }
    public bool hasFeature(string feat)
    {
        return features.Find(feat) != null;
    }

    public bool noFeatures()
    {
        return features.Any();
    }
    public void renderColor(int displayMode)
    {
        GenerationSettings gs = tileMap.getSettings();

        double elev = attributes["elevation"]; // Range [0,1]
        double mountainElev = Math.Clamp(elev / (gs.mountainMaxHeight), 0, 1);
        double extraPercent = Math.Clamp((elev - 1) / (gs.mountainMaxHeight - 1), 0, 1);


        double temp = attributes["temperature"]; // Range [0,1]
        double hum = attributes["humidity"]; // Range [0,1]

        double snowPercent = (2 * temp - 0.5) / gs.snowMultiplier; // Range [-0.5,0.5]

        switch (displayMode)
        {

            case 0: // Elevation + mountains
                if (isOcean())
                { // Ocean
                    
                    colorCache.R = (int)(50);
                    colorCache.G = (int)(75);
                    colorCache.B = (byte)(int)(100 + (elev * 155 / gs.seaLevel));
                    
                    //colorCache = new Color(50, 75, (byte)B);
                    if (elev < gs.seaLevel - gs.seafloorThreshold)
                    {
                        colorCache.B = (byte)(int)(90 + (elev * 155 / gs.seaLevel));
                        if (elev < gs.seaLevel - gs.seafloorThreshold * 2)
                            colorCache.B = (byte)(int)(80 + (elev * 155 / gs.seaLevel));
                    }
                }
                else if (hasFeature("beach"))
                { // Beach
                    elev *= 155;
                    colorCache.R = (byte)(int)(elev + 100);
                    colorCache.G = (byte)(int)(elev + 105);
                    colorCache.B = 95;
                }
                else if (hasFeature("forest") && elev <= 1.5)
                { // Forest
                    colorCache.R = 50;
                    colorCache.G = (byte)(int)(175 - 100 * hum);
                    colorCache.B = 50;
                }
                else if (elev > gs.mountainMinHeight + snowPercent * (gs.mountainMaxHeight - gs.mountainMinHeight))
                { // Snow
                    mountainElev *= 75;
                    colorCache = new Color((byte)(140 + mountainElev), (byte)(170 + mountainElev * .6), (byte)(140 + mountainElev));
                }
                else if (elev <= 1)
                { // Normal
                    colorCache.R = (int)(100);
                    colorCache.G = (byte)(int)(150 + 50 * colorCurve(elev, 100));
                    colorCache.B = (int)(100);
                    if (hasFeature("sea_cliff"))
                    {
                        colorCache.R = (byte)(int)(120 + 20 * colorCurve(elev, 100));
                        colorCache.B = (byte)(int)(120 + 35 * colorCurve(elev, 100));
                    }
                }
                else
                { // Foothills
                    colorCache.R = (byte)(int)(100 + extraPercent * 100);
                    colorCache.G = (byte)(int)(Math.Clamp(150 + 50 * Math.Exp(-10 * extraPercent), colorCache.R, 255)); // The Math.Clamp is to prevent purples
                    colorCache.B = (byte)(int)(100 + extraPercent * 100);
                    if (hasFeature("sea_cliff"))
                    {
                        colorCache.R = (byte)(int)(Math.Clamp((30 - 30 * elev) + 80 + 50 * Math.Exp(-10 * extraPercent), colorCache.G, 255));
                        colorCache.B = (byte)(int)(Math.Clamp((10 - 10 * elev) + 80 + 50 * Math.Exp(-10 * extraPercent), colorCache.G, 255));
                    }
                }
                break;

            case 1: // Elevation
                colorCache.R = (byte)(int)(60 + extraPercent * 150);
                colorCache.G = (byte)(int)(Math.Clamp(120 * Math.Clamp(elev, 0, 1) + 60 - 200 * extraPercent, colorCache.R, 255));
                colorCache.B = (byte)(int)(60 + extraPercent * 150);
                break;

            case 2: // Temp
                colorCache.R = (byte)(int)(255 * temp);
                colorCache.G = (int)(0);
                colorCache.B = (byte)(int)(155 - temp * 155);
                break;

            case 3: // Humidity
                colorCache.R = (int)(0);
                colorCache.G = (int)(0);
                colorCache.B = (byte)(int)(255 * hum);
                break;

            default:
                colorCache = new Color(0, 0, 0);
                break;
        }
        colorCache = new Color(colorCache.R, colorCache.G, colorCache.B);
    }

};
