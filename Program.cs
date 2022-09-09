//
//  main.cpp
//  Map Generator
//

using Map_Generator_CSharp.Source.tiles;
using Map_Generator_CSharp.Source;
using static SFML.Window.Keyboard;
using System.Drawing;
using System.IO;
using SFML.System;
using static Map_Generator_CSharp.Source.tiles.TileMap;
using static Map_Generator_CSharp.Source.DisplayManager;
using SFML.Window;
using System.Reflection.Emit;
using System.Resources;
using System;
using SFML.Graphics;
using System.ComponentModel;

main();

void main()
{
    // Where is the resource folder?
    string RunningPath = AppDomain.CurrentDomain.BaseDirectory;
    string path = string.Format("{0}Resources\\", Path.GetFullPath(Path.Combine(RunningPath, @"..\..\..\")));
    
    var initialScreenSize = new Vector2i(1366, 768);

    GenerationSettings gs = new GenerationSettings(

                                      // Tile generation
                                      200, 200, // Map size
                                      75.0, // Noise size
                                      3, // Levels of noise detail
                                      3, // Detail size reduction per level
                                      true, // Use new mountains


                                      // Universal mountain generation settings
                                      1.2, // Mountain min height
                                      3.3, // Mountain max height
                                      1.0, // Snow multiplier

                                      // New mountain generation
                                      0.000070,  // Chance of new range per tile
                                      12, 34, // Longevity of mountain range in number of attempted moountains(min and max)
                                      1.5, 3.0, // Distance between mountains in a range (min and max)
                                      2, //Random offset in each direction that a mountain can form. Lower bounds is 0.
                                      0.055, //Possible angle deviation (radians)
                                      2, // How much smaller are the ends?


                                      // Old mountain generation
                                      30, // Mountain range scatter (distance between ranges)
                                      0.35, // Mountain range spread (size)
                                      0.05, // Mountain range density


                                      // Mountain smoothing
                                      1.05, // Mountain smooth threshold
                                      20,   // Number of smoothing passes
                                      0.45, // How much to smooth (min and max)
                                      0.60, // ^ Lower values = more smoothing

                                      // Oceans
                                      0.5,  // Sea level
                                      false, // Can mountains generate in oceans
                                      0.01, // Beach threashold
                                      1.5, // Ocean humidity multiplier
                                      0.03, // Seafloor threshold

                                      // Humidity smoothing
                                      0.9, // Humidity smooth threshold
                                      15,   // Number of smoothing passes
                                      0.75, // How much to smooth (min and max)
                                      0.8, // ^ Lower values = more smoothing

                                      // Forest generation
                                      40, // Forest scatter (distance between forests)
                                      (float)(Math.Pow(2, 7)), // Forest density
                                      8.5, // How cohesive should the forests be?
                                      3.0 // How much does humidity matter?
                                     );

    TileMap tileMap = new TileMap(gs);

    DisplaySettings ds = new DisplaySettings(
                                              true, // whether to start on the map
                                              initialScreenSize.X, initialScreenSize.Y, // Screen width and height
                                              0.0, 0.0, // Starting camera x and ys
                                              150.0, // Starting tiles shown
                                              24.0, 400.0, // Min and max tiles shown
                                              200, 200, 200, // Base text color
                                              20, 20, 20, // Outline text color
                                              0 // Default display mode
                                             );




    double FPSUpdateFreq = 0.1; // How often to update the FPS display (in seconds)

    Clock clock = new Clock();
    double currentTime, lastTime = 0, deltaTime, lastFPSTime = 0, deltaFPSTime;
    int frameCounter = 0;

    bool repeatProgram = false;



    do
    {
        DisplayManager dm = new DisplayManager(ds, tileMap, path);
        dm.GetWindow().SetFramerateLimit(30);
        //dm.GetWindow().SetVerticalSyncEnabled(true);
        dm.SetClock(clock);
        repeatProgram = false;
        var window = dm.GetWindow();

        window.Closed += OnClosed;
        window.Resized += dm.resize;
        window.MouseWheelScrolled += dm.OnMouseScroll;
        window.MouseButtonPressed += dm.OnMousePress;
        window.MouseButtonReleased += dm.OnMouseRelease;
        window.MouseMoved += dm.OnMouseMove;
        window.KeyPressed += dm.OnKeyPress;
        window.KeyReleased += dm.OnKeyRelease;


        while (dm.isOpen())
        {
            window.DispatchEvents();
            dm.Move();

            // frame-locked actions

            currentTime = clock.ElapsedTime.AsSeconds();
            deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            dm.deltaTime = deltaTime;

            // FPS calculation
            deltaFPSTime = currentTime - lastFPSTime; // In seconds
            frameCounter++;
            if (deltaFPSTime >= FPSUpdateFreq)
            {
                double fps = frameCounter / deltaFPSTime;
                dm.fps = ((int)(fps));
                lastFPSTime = currentTime;
                frameCounter = 0;
            }
            dm.display();
            //window.WaitAndDispatchEvents();
        }
    } while (repeatProgram);
}


static void OnClosed(object sender, EventArgs e)
{
    var window = (RenderWindow)sender;
    window.Close();
}

