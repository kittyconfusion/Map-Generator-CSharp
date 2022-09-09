using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Map_Generator_CSharp.Source.external_util;
using Map_Generator_CSharp.Source.tiles;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Color = SFML.Graphics.Color;

namespace Map_Generator_CSharp.Source;
class DisplayManager
{
    public struct DisplaySettings
    {
        public bool mapStartEnabled;
        public int screenWidth, screenHeight;
        public double initialXOffset, initialYOffset, initialTilesShown;
        public double minTilesShown, maxTilesShown;
        public int baseR, baseG, baseB, outR, outG, outB;
        public int displayMode;

        public DisplaySettings(bool v1, int x, int y, double v2, double v3, double v4, double v5, double v6, int v7, int v8, 
            int v9, int v10, int v11, int v12, int v13) : this()
        {
            mapStartEnabled = v1;screenWidth = x;screenHeight = y;initialXOffset = v2;initialYOffset = v3;initialTilesShown = v4;
            minTilesShown = v5;maxTilesShown = v6;
            baseR = v7;baseG = v8;baseB = v9;outR = v10;outG = v11;outB = v12;displayMode = v13;
        }
    }

    private DisplaySettings displaySettings;
    private double xOffset, yOffset;
    private float tileSize;

    private RenderWindow window;
    private View view;
    private Font font;

    private RenderTexture mapRenderTexture;
    private RenderTexture mapUIRenderTexture;
    private RenderTexture menuRenderTexture;

    private bool activeMap = false;
    private bool mapNeedsUpdate = false;
    private bool activeMapUI = false;
    private bool activeMenu = false;

    public int fps;

    private bool viewingTile = false;
    private Vector2i viewTileCoords;
    private Vector2f viewTileDisplayCoords;

    private double cameraSpeed, effectiveCameraSpeed;

    private TileMap tileMap;

    private string @resourceDir;

    Clock clock;

    //Movement Stuffs
    int recentDragX, recentDragY;

    bool drag = false;

    bool left = false, right = false, up = false, down = false, shift = false;

    double clickTime;
    int clickX, clickY;

    double maxClickLength = 0.2; // How long a click can be to qualify (in seconds)

    public double deltaTime;

    private List<Vertex> rectPoints = new List<Vertex>(40000);

    public void renderMap(bool active) { activeMap = active; }
    public void renderMapUI(bool active) { activeMapUI = active; }
    public void renderMenu(bool active) { activeMenu = active; }
        
    public bool renderingMap() { return activeMap; }
    public bool renderingMapUI() { return activeMapUI; }
    public bool renderingMenu() { return activeMenu; }

    public void SetClock(Clock c) { clock = c; }
    double getTileSize() { return tileSize; }
    double getTilesShown() { return Math.Max(displaySettings.screenWidth, displaySettings.screenHeight) / tileSize; }

    public DisplayManager(DisplaySettings settings, TileMap tm, string rDir) 
    {
        window = new RenderWindow (new VideoMode((uint)settings.screenWidth, (uint)settings.screenHeight), "Map Generator");

        mapRenderTexture = new RenderTexture((uint)settings.screenWidth, (uint)settings.screenHeight);
        menuRenderTexture = new RenderTexture((uint)settings.screenWidth, (uint)settings.screenHeight);

        view = new View();
        view.Center = (new Vector2f(settings.screenWidth / 2, settings.screenHeight / 2));
        view.Size = (new Vector2f(settings.screenWidth, settings.screenHeight));
        
        window.SetView(view);

        if (settings.mapStartEnabled)
        {
            activeMap = true;
            activeMapUI = true;
        }
        else
            activeMenu = true;

        displaySettings = settings;

        tileMap = tm;

        resourceDir = rDir;
        /*
        if (resourceDir == "")
        {
            resourceDir = ExePath::mergePaths(ExePath::getExecutableDir(), "Resources");
        }
        */
        //std::cout << "Using resource directory: " << resourceDir << "\n";

        xOffset = settings.initialXOffset;
        yOffset = settings.initialYOffset;
        tileSize = (float)(Math.Max(settings.screenWidth, settings.screenHeight) / settings.initialTilesShown);

        loadFont();
        loadIcon();

        // How many seconds does it take to move across one screen with the camera?
        double cameraSecondsPerScreen = 2;
        // Camera speed in tiles per second
        cameraSpeed = displaySettings.initialTilesShown / cameraSecondsPerScreen;
        effectiveCameraSpeed = cameraSpeed;
    }
    public RenderWindow GetWindow()
    {
        return window;
    }

    public void setTileMap(TileMap tm)
    {
        tileMap = tm;
        setWhetherViewingTile(false);
    }
    private string mergePaths(string pathA, string pathB)
    {
        return @pathA + @pathB;
    }
    private string getResourcePath(string resource)
    {
        return mergePaths(resourceDir, resource);
    }

    private void loadFont()
    {

        font = new Font(getResourcePath("font.ttf"));

    }

    private void loadIcon()
    {
        var icon = new Image(getResourcePath("icon.png"));

        if (!File.Exists(getResourcePath("icon.png")))
        {
            throw new FileNotFoundException("Resource not found: " + getResourcePath("icon.png"));
        }
        
        window.SetIcon(icon.Size.X, icon.Size.Y, icon.Pixels);
    }
    //Called after all movement keys have been processed
    public void Move()
    {
        effectiveCameraSpeed = cameraSpeed;
        effectiveCameraSpeed *= deltaTime; // Make speed ignore FPS
        if(shift) { effectiveCameraSpeed *= 3; }

        if (!drag)
        {
            if (up && !down) { moveCamera(0, (float)-effectiveCameraSpeed); }
            else if (down && !up) { moveCamera(0, (float)effectiveCameraSpeed); }
            if (left && !right) { moveCamera((float)-effectiveCameraSpeed, 0); }
            else if (right && !left) { moveCamera((float)effectiveCameraSpeed, 0); }
        }
    }
    public void OnKeyRelease(object sender, KeyEventArgs e)
    {
        if (e.Code == Keyboard.Key.W || e.Code == Keyboard.Key.Up) { up = false; }

        if (e.Code == Keyboard.Key.S || e.Code == Keyboard.Key.Down) { down = false; }

        if (e.Code == Keyboard.Key.A || e.Code == Keyboard.Key.Left) { left = false; }

        if (e.Code == Keyboard.Key.D || e.Code == Keyboard.Key.Right) { right = false; }

        if (e.Code == Keyboard.Key.LShift || e.Code == Keyboard.Key.RShift) { shift = false; }
    }
    public void OnKeyPress(object sender, KeyEventArgs e)
    {

        // Move via WASD. Mouse take priority over WASD.
        if (e.Code == Keyboard.Key.W || e.Code == Keyboard.Key.Up) { up = true; }

        else if (e.Code == Keyboard.Key.S || e.Code == Keyboard.Key.Down) { down = true; }
        
        else if (e.Code == Keyboard.Key.A || e.Code == Keyboard.Key.Left) { left = true; }

        else if (e.Code == Keyboard.Key.D || e.Code == Keyboard.Key.Right) { right = true; }

        else if (e.Code == Keyboard.Key.LShift || e.Code == Keyboard.Key.RShift) { shift = true; }

        // changes color mode
        else if (e.Code == Keyboard.Key.C)
        {
            setDisplayMode((getDisplayMode() + 3) % 4);
            tileMap.rerenderTiles(getDisplayMode());
        }
        else if (e.Code == Keyboard.Key.V)
        {
            setDisplayMode((getDisplayMode() + 1) % 4);
            tileMap.rerenderTiles(getDisplayMode());
        }

        // exits tile view
        else if (e.Code == Keyboard.Key.Escape)
        {
            setWhetherViewingTile(false);
        }

        else if (e.Code == Keyboard.Key.F1)
        {
            renderMapUI(!renderingMapUI());
        }
        else if (e.Code == Keyboard.Key.Space)
        {
            Console.WriteLine("\n");
            Random rand = new Random();
            tileMap = new TileMap(tileMap.getSettings());
        }

        else if (e.Code == Keyboard.Key.F3)
        {
            NewMapFromConsole();
        }
    }
    private void NewMapFromConsole()
    {
        int attemptSeed;
        Console.WriteLine("Enter new seed: ");
        try
        {
            attemptSeed = int.Parse(Console.ReadLine());
            tileMap = new TileMap(tileMap.getSettings(), attemptSeed);
        }
        catch (Exception err)
        {
            Console.WriteLine("Invalid seed, " + err.Message);
        }
    }

    public void OnMouseMove(object sender, MouseMoveEventArgs e)
    {
        if (drag)
        {
            int newX = e.X;
            int newY = e.Y;

            moveCamera((float)((recentDragX - newX) / getTileSize()), (float)((recentDragY - newY) / getTileSize()));

            recentDragX = newX;
            recentDragY = newY;
        }
    }
    public void OnMousePress(object sender, MouseButtonEventArgs e)
    {
        if (e.Button == Mouse.Button.Left)
        {
            recentDragX = e.X;
            recentDragY = e.Y;
            drag = true;

            clickX = e.X;
            clickY = e.Y;
            clickTime = clock.ElapsedTime.AsSeconds();
        }
    }
    public void OnMouseRelease(object sender, MouseButtonEventArgs e)
    {
        if (e.Button == Mouse.Button.Left)
        {
            drag = false;

            if (e.X - clickX == 0 && e.Y - clickY == 0 && clock.ElapsedTime.AsSeconds() - clickTime <= maxClickLength)
            {
                onClick((int)(clickX * ((double)(displaySettings.screenWidth) / getWindowWidth())), (int)(clickY * ((double)(displaySettings.screenHeight) / getWindowHeight())));
            }
        }
    }
    public void OnMouseScroll(object sender, MouseWheelScrollEventArgs e)
    {
        changeTileSize(e.Delta);

        double cameraSecondsPerScreen = 2;

        cameraSpeed = getTilesShown() / cameraSecondsPerScreen; // Tiles per second
    }
    public void resize(object sender, SizeEventArgs e)
    {
        displaySettings.screenWidth = (int)e.Width;
        displaySettings.screenHeight = (int)e.Height;
        changeTileSize(0); // Make sure tilesize is within bounds

        view.Center = (new Vector2f(displaySettings.screenWidth / 2, displaySettings.screenHeight / 2));
        view.Size = (new Vector2f(displaySettings.screenWidth, displaySettings.screenHeight));

        window.SetView(view);
        
        mapRenderTexture = new RenderTexture((uint)displaySettings.screenWidth, (uint)displaySettings.screenHeight);
        menuRenderTexture = new RenderTexture((uint)displaySettings.screenWidth, (uint)displaySettings.screenHeight);
    }
    public void display()
    {
        window.Clear();
        draw();
        window.Display();
    }
    private void draw()
    {
        if (activeMap)
        {
            // clear map rendertexture
            mapRenderTexture.Clear(new Color(63, 63, 55));

            // map drawing
            drawTiles();
            if (activeMapUI)
            {
                if (viewingTile) { drawTileStats(); }
                drawCoords();
                drawControls();
                drawDebug();
                drawColorScheme();
            }

            // update map rendertexture
            mapRenderTexture.Display();

            // pass rendertexture to window
            Texture texture = mapRenderTexture.Texture;
            Sprite renderTextureSprite = new Sprite(texture);
            window.Draw(renderTextureSprite);
        }
        if (activeMenu)
        {
            menuRenderTexture.Clear();

            menuRenderTexture.Display();

            Texture texture3 = menuRenderTexture.Texture;
            Sprite renderTextureSprite = new Sprite(texture3);
            window.Draw(renderTextureSprite);
        }
    }
    private void drawTiles()
    {

        int tileDisplayWidth = (int)(displaySettings.screenWidth / tileSize) + 2;
        int tileDisplayHeight = (int)(displaySettings.screenHeight / tileSize) + 2;

        rectPoints.Clear();

        for (int y = (int)yOffset; y < tileDisplayHeight + (int)yOffset; y++)
        {
            for (int x = (int)xOffset; x < tileDisplayWidth + (int)xOffset; x++)
            {

                if (x < 0 || x >= tileMap.getWidth() || y < 0 || y >= tileMap.getHeight())
                {
                    continue;
                }

                Tile t = tileMap.getTile(x, y);
                Vector2f screenPos = new Vector2f((float)((x - xOffset) * tileSize), (float)((y - yOffset) * tileSize));
                Color col;
                
                if (viewingTile == true && viewTileCoords.X == x && viewTileCoords.Y == y && activeMapUI)
                {
                    col = new Color(255, 100, 100);
                }
                else
                {
                    col = t.getColor(getDisplayMode());
                }
                
                Vertex v = new Vertex(screenPos + new Vector2f(0, 0), col);
                rectPoints.Add(v);

                v = new Vertex(screenPos + new Vector2f(tileSize, 0), col);
                rectPoints.Add(v);

                v = new Vertex(screenPos + new Vector2f(tileSize, tileSize), col);
                rectPoints.Add(v);

                v = new Vertex(screenPos + new Vector2f(0, tileSize), col);
                rectPoints.Add(v);
            }
            if (rectPoints.Count >= 40000)
            {
                mapRenderTexture.Draw(rectPoints.ToArray(), PrimitiveType.Quads);
                rectPoints.Clear();
                //rectPoints = new List<Vertex>(10000);
            }
        }
        //mapRenderTexture.Draw()
        mapRenderTexture.Draw(rectPoints.ToArray(),PrimitiveType.Quads);
    }
    /*
    private RectangleShape drawTile(Tile t, Vector2f screenPos)
    {
        return drawTile(t.getColor(getDisplayMode()), screenPos);
    }
    private RectangleShape drawTile(Color highlight, Vector2f screenPos)
    {
        RectangleShape rect = new RectangleShape();
        rect.Position = screenPos;
        rect.Size = new Vector2f((float)tileSize, (float)tileSize);
        rect.FillColor = highlight;
        return rect;
        //mapRenderTexture.Draw(rect);
    }
    */
    private void drawTileStats()
    {
        Tile viewTile = tileMap.getTile(viewTileCoords.X, viewTileCoords.Y);

        string featureText;
        if (viewTile.hasFeature("sea_cliff"))
        {
            featureText = "Cliffs";
        }
        else if (viewTile.isOcean())
        {
            featureText = "Ocean";
        }
        else if (viewTile.hasFeature("mountain"))
        {
            featureText = "Mountain";
        }
        else if (viewTile.hasFeature("foothill"))
        {
            featureText = "Foothills";
        }
        else if (viewTile.hasFeature("beach"))
        {
            featureText = "Beach";
        }
        else if (viewTile.hasFeature("forest"))
        {
            featureText = "Forest";
        }
        else
        {
            featureText = "Plains";
        }

        int offset = 5;
        int fontSize = 30; // Pixels

        int xSize = (int)(13.5 * fontSize);
        int ySize = 4 * fontSize + 6 * offset;

        Vector2f size = new Vector2f(xSize, ySize);
        RoundedRectangleShape rect = new RoundedRectangleShape(size, 5, 5); // A class I found off of GitHub (make sure to add the files to your IDE in order to see them)
        rect.FillColor = (new Color(100, 100, 100));
        rect.Position = (viewTileDisplayCoords);

        mapRenderTexture.Draw(rect);

        Text text = new Text();
        text.Font = font;
        text.CharacterSize = (uint)fontSize;
        text.FillColor = new Color((byte)displaySettings.baseR, (byte)displaySettings.baseG, (byte)displaySettings.baseB);
        text.OutlineThickness = 2;
        text.OutlineColor = new Color((byte)displaySettings.outR, (byte)displaySettings.outG, (byte)displaySettings.outB);

        text.Style = Text.Styles.Bold | Text.Styles.Underlined;
        text.Position = new Vector2f(viewTileDisplayCoords.X + offset, viewTileDisplayCoords.Y + offset);
        text.DisplayedString = ("Tile (" + viewTileCoords.X + "," + viewTileCoords.Y + ")" + ((featureText != "") ? " - " + featureText : ""));
        mapRenderTexture.Draw(text);
        text.Style = (Text.Styles.Regular);

        text.Position = new Vector2f(text.Position.X, text.Position.Y + fontSize + offset);
        text.DisplayedString = ("Elevation: " + ((int)(100 * viewTile.getAttribute("elevation"))));
        mapRenderTexture.Draw(text);

        text.Position = new Vector2f(text.Position.X, text.Position.Y + fontSize + offset);
        text.DisplayedString = ("Temperature: " + ((int)(100 * viewTile.getAttribute("temperature"))));
        mapRenderTexture.Draw(text);

        text.Position = new Vector2f(text.Position.X, text.Position.Y + fontSize + offset);
        text.DisplayedString = ("Humidity: " + ((int)(100 * viewTile.getAttribute("humidity"))));
        mapRenderTexture.Draw(text);

    }

    private void drawCoords()
    {
        Text coordText = new Text();
        coordText.Font = font;

        var coords = new Vector2f();
        coords = getCameraCenter();

        coordText.DisplayedString = ("(" + ((int)(coords.X)) + ", " + ((int)(coords.Y)) + ")");


        coordText.CharacterSize = 70; // Pixels, not normal font size
        coordText.FillColor = (new Color((byte)displaySettings.baseR, (byte)displaySettings.baseG, (byte)displaySettings.baseB)); // Color

        coordText.OutlineThickness = 2;
        coordText.OutlineColor = (new Color((byte)displaySettings.outR, (byte)displaySettings.outG, (byte)displaySettings.outB));

        coordText.Style = Text.Styles.Bold;


        coordText.Position = new Vector2f(10, 10);
        mapRenderTexture.Draw(coordText);
    }
    private void drawControls()
    {
        Text controlText = new Text();
        controlText.Font = font;
        if (Keyboard.IsKeyPressed(Keyboard.Key.H))
        {
            controlText.DisplayedString = ("WASD/arrows/click-and-drag to move. Shift to go faster.\nSpace to regenerate terrain. F3 to enter seed in console.\nC/V to change display mode.\nClick on tile to view, ESC to stop viewing.\nF1 to toggle UI.");
        }
        else
        {
            controlText.DisplayedString = ("H for controls.");
        }

        controlText.CharacterSize = 30; // Pixels, not normal font size
        controlText.FillColor = (new Color((byte)displaySettings.baseR, (byte)displaySettings.baseG, (byte)displaySettings.baseB)); // Color

        controlText.OutlineThickness = 2;
        controlText.OutlineColor = (new Color((byte)displaySettings.outR, (byte)displaySettings.outG, (byte)displaySettings.outB));

        controlText.Style = Text.Styles.Bold;


        controlText.Position = new Vector2f(10, 110);
        mapRenderTexture.Draw(controlText);
    }
    private void drawDebug()
    {
        Text debugText = new Text();
        debugText.Font = font;

        debugText.DisplayedString = fps.ToString();


        debugText.CharacterSize = 40; // Pixels, not normal font size
        debugText.FillColor = (new Color(0, 255, 0)); // Color

        debugText.OutlineThickness = 2;
        debugText.OutlineColor = (new Color(0, 0, 0));

        debugText.Style = Text.Styles.Bold;


        debugText.Position = new Vector2f(displaySettings.screenWidth - debugText.GetGlobalBounds().Width - 10, 10);
        mapRenderTexture.Draw(debugText);

        debugText.DisplayedString = tileMap.getSeed().ToString();

        debugText.FillColor = (new Color(255, 255, 255));

        debugText.CharacterSize = 15;

        debugText.Position = new Vector2f(displaySettings.screenWidth - debugText.GetGlobalBounds().Width - 10, 70);
        mapRenderTexture.Draw(debugText);

        debugText.DisplayedString = ((tileMap.getSettings().width) + " x " + (tileMap.getSettings().height));

        debugText.Position = new Vector2f(displaySettings.screenWidth - debugText.GetGlobalBounds().Width - 10, 100);
        mapRenderTexture.Draw(debugText);
    }
    private void drawColorScheme()
    {
        Text colorSchemeText = new Text();
        colorSchemeText.Font = font;

        switch (displaySettings.displayMode)
        {
            case 0:
                colorSchemeText.DisplayedString = ("Elevation + Features");
                break;
            case 1:
                colorSchemeText.DisplayedString = ("Elevation");
                break;
            case 2:
                colorSchemeText.DisplayedString = ("Temperature");
                break;
            case 3:
                colorSchemeText.DisplayedString = ("Humidity");
                break;
            default:
                colorSchemeText.DisplayedString = ("Invalid display setting!");
                break;
        }


        colorSchemeText.CharacterSize = 40; // Pixels, not normal font size
        colorSchemeText.FillColor = (new Color((byte)displaySettings.baseR, (byte)displaySettings.baseG, (byte)displaySettings.baseB)); // Color

        colorSchemeText.OutlineThickness = 2;
        colorSchemeText.OutlineColor = (new Color((byte)displaySettings.outR, (byte)displaySettings.outG, (byte)displaySettings.outB));

        colorSchemeText.Style = Text.Styles.Bold;


        colorSchemeText.Position = new Vector2f(10, displaySettings.screenHeight - colorSchemeText.GetGlobalBounds().Height - 20);
        mapRenderTexture.Draw(colorSchemeText);
    }
    // Functions relayed from sf::RenderWindow

    public bool isOpen()
    {
        return window.IsOpen;
    }

    public void close()
    {
        window.Close();
    }

    //https://en.sfml-dev.org/forums/index.php?topic=27155.0 <- Do this
    /*
    public bool pollEvent(Event @event) {
        return window.pollEvent(@event);
    }
    */
    public uint getWindowWidth()
    {
        return window.Size.X;
    }
    public uint getWindowHeight()
    {
        return window.Size.Y;
    }

    public Vector2f getCameraCenter()
    {
        double centerX = xOffset + displaySettings.screenWidth / (2.0* tileSize);
        double centerY = yOffset + displaySettings.screenHeight / (2.0* tileSize);
        return new Vector2f((float)centerX, (float)centerY);
    }

    public double getMaxTileSize()
    {
        return Math.Max(displaySettings.screenWidth, displaySettings.screenHeight) / displaySettings.minTilesShown;
    }

    public double getMinTileSize()
    {
        return Math.Max(displaySettings.screenWidth, displaySettings.screenHeight) / displaySettings.maxTilesShown;
    }

    public void changeTileSize(double delta)
    {
        Vector2f center = getCameraCenter();

        tileSize = (float)Math.Max(Math.Min(tileSize + delta, getMaxTileSize()), getMinTileSize());

        xOffset = center.X - displaySettings.screenWidth / (2 * tileSize);
        yOffset = center.Y - displaySettings.screenHeight / (2 * tileSize);

        setWhetherViewingTile(false); // Leads to general problems if this isn't here
    }
    public void moveCamera(float x, float y)
    {
        xOffset += x;
        yOffset += y;

        viewTileDisplayCoords.X -= x * (displaySettings.screenWidth / getWindowWidth()) * (float)tileSize;
        viewTileDisplayCoords.Y -= y * (displaySettings.screenHeight / getWindowHeight()) * (float)tileSize;
    }
    public void setViewTile(Vector2i tileCoords, Vector2f screenCoords)
    {
        viewTileCoords = tileCoords;
        viewTileDisplayCoords = screenCoords;

        setWhetherViewingTile(true);
    }

    public void setWhetherViewingTile(bool view)
    {
        viewingTile = view;
    }

    public Vector2i getTileCoordsFromScreenCoords(int screenX, int screenY)
    {
        return new Vector2i((int)(xOffset + (double)screenX / tileSize), (int)(yOffset + (double)screenY / tileSize));
    }

    public void onClick(int clickX, int clickY)
    {
        Vector2i tileCoords = getTileCoordsFromScreenCoords(clickX, clickY);

        if ((!viewingTile || tileCoords != viewTileCoords) && tileCoords.X >= 0 && tileCoords.X < tileMap.getWidth() && tileCoords.Y >= 0 && tileCoords.Y < tileMap.getHeight())
        {
            setViewTile(tileCoords, new Vector2f(clickX + displaySettings.screenWidth / 20, clickY + displaySettings.screenHeight / 20));
        }
        else
        {
            setWhetherViewingTile(false);
        }
    }

    public int getDisplayMode()
    {
        return displaySettings.displayMode;
    }
    public void setDisplayMode(int mode)
    {
        displaySettings.displayMode = mode;
    }
}
