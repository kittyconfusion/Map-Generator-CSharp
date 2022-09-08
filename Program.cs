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

main();

void main()
{
    // Where is the resource folder?
    // USE_WORKING_DIR specifies whether to use the working directory
    // ALT_RESOURCE_FOLDER specifies the location. "" uses the exe's location
    string path = @"C:/Users/natha/source/repos/Map-Generator-CSharp/Resources/";
var initialScreenSize = new Vector2i(1366, 768);
    
    GenerationSettings gs = new GenerationSettings (

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
                                      12, 32, // Longevity of mountain range in number of attempted moountains(min and max)
                                      2.0, 3.5, // Distance between mountains in a range (min and max)
                                      1, //Random offset in each direction that a mountain can form. Lower bounds is 0.
                                      0.05, //Possible angle deviation (radians)
                                      2, // How much smaller are the ends?


                                      // Old mountain generation
                                      30, // Mountain range scatter (distance between ranges)
                                      0.35, // Mountain range spread (size)
                                      0.05, // Mountain range density


                                      // Mountain smoothing
                                      1.05, // Mountain smooth threshold
                                      20,   // Number of smoothing passes
                                      0.45, // How much to smooth (min and max)
                                      0.55, // ^ Lower values = more smoothing

                                      // Oceans
                                      0.5,  // Sea level
                                      false, // Can mountains generate in oceans
                                      0.01, // Beach threashold
                                      1.5, // Ocean humidity multiplier
                                      0.03, // Seafloor threshold

                                      // Humidity smoothing
                                      1.1, // Humidity smooth threshold
                                      15,   // Number of smoothing passes
                                      0.75, // How much to smooth (min and max)
                                      0.8, // ^ Lower values = more smoothing

                                      // Forest generation
                                      50, // Forest scatter (distance between forests)
                                      (float)(Math.Pow(2, 7)), // Forest density
                                      8.5, // How cohesive should the forests be?
                                      4.0 // How much does humidity matter?
                                     );

    TileMap tileMap = new TileMap(gs);

    DisplaySettings ds = new DisplaySettings(
                                              true, // whether to start on the map
                                              initialScreenSize.X, initialScreenSize.Y, // Screen width and height
                                              0.0, 0.0, // Starting camera x and ys
                                              150.0, // Starting tiles shown
                                              4.0, 400.0, // Min and max tiles shown
                                              200, 200, 200, // Base text color
                                              20, 20, 20, // Outline text color
                                              0 // Default display mode
                                             );


    // How many seconds does it take to move across one screen with the camera?
    double cameraSecondsPerScreen = 2;
    // Camera speed in tiles per second
    double cameraSpeed = ds.initialTilesShown / cameraSecondsPerScreen, effectiveCameraSpeed = cameraSpeed;
    double FPSUpdateFreq = 0.2; // How often to update the FPS display (in seconds)
    double maxClickLength = 0.2; // How long a click can be to qualify (in seconds)

    bool left = false, right = false, up = false, down = false;
    bool drag = false;
    int recentDragX, recentDragY;

    double clickTime;
    int clickX, clickY;

    Clock clock = new Clock();
    double currentTime, lastTime = 0, deltaTime, lastFPSTime = 0, deltaFPSTime;
    int frameCounter = 0;

    bool repeatProgram = false;

    do
    {
        DisplayManager dm = new DisplayManager(ds, tileMap, path);
        repeatProgram = false;
        while (dm.isOpen())
        {
            /*
            // Process events
            Event pevent;
            while (dm.pollEvent(pevent))
            {
                // Close the window
                if (pevent.type == sf::Event::Closed) {
                    dm.close();
                }
                
                else if (pevent.type == sf::Event::Resized) {
                    dm.resize(pevent.size.width, pevent.size.height);
                }

                    // Zoom with mouse
                    else if (pevent.Type == MouseWheelScrollEvent) {
                    if (pevent.mouseWheelScroll.wheel == sf::Mouse::VerticalWheel) {
                        dm.changeTileSize(pevent.mouseWheelScroll.delta);
                        cameraSpeed = dm.getTilesShown() / cameraSecondsPerScreen; // Tiles per second
                    }
                }

                    // Drag with mouse
                    else if (pevent.type == sf::Event::MouseButtonPressed) {
                    if (pevent.mouseButton.button == sf::Mouse::Left)
                        {
                        recentDragX = pevent.mouseButton.x;
                        recentDragY = pevent.mouseButton.y;
                        drag = true;

                        clickX = pevent.mouseButton.x;
                        clickY = pevent.mouseButton.y;
                        clickTime = clock.getElapsedTime().asSeconds();
                    }
                }
                    else if (pevent.type == sf::Event::MouseButtonReleased) {
                    if (pevent.mouseButton.button == sf::Mouse::Left)
                        {
                        drag = false;

                        if (pevent.mouseButton.x - clickX == 0 && pevent.mouseButton.y - clickY == 0 && clock.getElapsedTime().asSeconds() - clickTime <= maxClickLength) {
                            dm.onClick(clickX * ((double)(dm.getDisplaySettings().screenWidth) / dm.getWindowWidth()), clickY * ((double)(dm.getDisplaySettings().screenHeight) / dm.getWindowHeight()));
                        }
                    }
                }
                    // Move with mouse. Mouse take priority over WASD.
                    else if (pevent.type == sf::Event::MouseMoved) {
                    if (drag)
                    {
                        int newX = pevent.mouseMove.x;
                        int newY = pevent.mouseMove.y;

                        dm.moveCamera(((recentDragX - newX) / dm.getTileSize()), ((recentDragY - newY) / dm.getTileSize()));

                        recentDragX = newX;
                        recentDragY = newY;
                    }
                }


                    else if (pevent.type == sf::Event::KeyPressed) {

                    // regenerates the tilemap when space is pressed
                    if (pevent.key.code == sf::Keyboard::Space) {
                        unsigned int lastSeed = tileMap->getSeed();
                        delete tileMap;
                        if (sf::Keyboard::isKeyPressed(sf::Keyboard::LControl))
                        {
                            char* input[12];
                            std::cout << "Enter new seed: ";
                            std::cin.getline((char*)input, 12);
                            try
                            {
                                lastSeed = std::stoul(std::string((char*)input).substr(0, 10));
                                std::cout << std::endl;
                                tileMap = new TileMap(gs, lastSeed);
                            }
                            catch (...) {
                                std::cout << "Invalid number\n";
                            }
                            }
                            else
                            {
                                tileMap = new TileMap(gs);
                            }
                            dm.setTileMap(tileMap);

                        }

                        // changes color mode
                        else if (pevent.key.code == sf::Keyboard::C) {
                            dm.setDisplayMode((dm.getDisplayMode() + 3) % 4);
                            tileMap->rerenderTiles(dm.getDisplayMode());
                        }
                        else if (pevent.key.code == sf::Keyboard::V) {
                            dm.setDisplayMode((dm.getDisplayMode() + 1) % 4);
                            tileMap->rerenderTiles(dm.getDisplayMode());
                        }

                        // exits tile view
                        else if (pevent.key.code == sf::Keyboard::Escape) {
                            dm.setWhetherViewingTile(false);
                        }

                        else if (pevent.key.code == sf::Keyboard::F1) {
                            if (dm.renderingMapUI())
                                dm.renderMapUI(false);
                            else
                                dm.renderMapUI(true);
                        }
                    }
                }
                */
            // frame-locked actions

            currentTime = clock.ElapsedTime.AsSeconds();
            deltaTime = currentTime - lastTime;
            lastTime = currentTime;

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

            /*
                // Move via WASD. Mouse take priority over WASD.
                if (sf::Keyboard::isKeyPressed(sf::Keyboard::W) || sf::Keyboard::isKeyPressed(sf::Keyboard::Up))
                {
                    up = true;
                }
                else
                {
                    up = false;
                }
                if (sf::Keyboard::isKeyPressed(sf::Keyboard::S) || sf::Keyboard::isKeyPressed(sf::Keyboard::Down))
                {
                    down = true;
                }
                else
                {
                    down = false;
                }
                if (sf::Keyboard::isKeyPressed(sf::Keyboard::A) || sf::Keyboard::isKeyPressed(sf::Keyboard::Left))
                {
                    left = true;
                }
                else
                {
                    left = false;
                }
                if (sf::Keyboard::isKeyPressed(sf::Keyboard::D) || sf::Keyboard::isKeyPressed(sf::Keyboard::Right))
                {
                    right = true;
                }
                else
                {
                    right = false;
                }
                if (sf::Keyboard::isKeyPressed(sf::Keyboard::LShift) || sf::Keyboard::isKeyPressed(sf::Keyboard::RShift))
                {
                    effectiveCameraSpeed = cameraSpeed * 3;
                }
                else
                {
                    effectiveCameraSpeed = cameraSpeed;
                }
                effectiveCameraSpeed *= deltaTime; // Make speed ignore FPS

                if (!drag)
                {
                    if (up && !down) { dm.moveCamera(0, -effectiveCameraSpeed); }
                    else if (down && !up) { dm.moveCamera(0, effectiveCameraSpeed); }
                    if (left && !right) { dm.moveCamera(-effectiveCameraSpeed, 0); }
                    else if (right && !left) { dm.moveCamera(effectiveCameraSpeed, 0); }
                }
                */
                dm.display();
            }

        } while (repeatProgram) ;
    }

