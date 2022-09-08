using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Map_Generator_CSharp.Source.util;
//
//  PerlinNoise.hpp
//  Map Generator
//
class PerlinNoiseGenerator
{
    static private int randomInitialSeed()
    {
        var rand = new Random();
        return rand.Next();
    }
    struct Vec2D <numClass> 
    {
        public numClass x;
        public numClass y;

        public Vec2D(numClass _x, numClass _y)
        {
            x = _x;
            y = _y;
        }
    }
    
    private Vec2D<int> dimensions;
    public PerlinNoiseGenerator(int width, int height) : this(randomInitialSeed(), width, height) {}
    public PerlinNoiseGenerator(int seed, int width, int height) : this(new Random(seed), width, height, true) {}
    public PerlinNoiseGenerator(Random rand, int width, int height) : this(rand, width, height, false) { }
    private PerlinNoiseGenerator(Random rand, int width, int height, bool deleteRandWhenDone) // For constructor from seed
    {
        dimensions = new Vec2D<int> (width, height);
        generateGradients(rand);

        if (deleteRandWhenDone) {//Unsure of how to do this}
    }
}
    public double noise(double x, double y)
    {

        if ((int)x < 0 || x >= dimensions.x || (int)y < 0 || y >= dimensions.y)
        {
            throw new ArgumentOutOfRangeException("Point (" + x + "," + y + ") out of range for noise of size (" + dimensions.x + "," + dimensions.y + ")");
        }

        var point = new Vec2D<double>(x, y);

        // Get grid point coords
        int x0 = (int)x;
        int x1 = x0 + 1;
        int y0 = (int)y;
        int y1 = y0 + 1;

        double xInterp = x - x0;
        double yInterp = y - y0;

        double g0, g1, interp0, interp1, result;

        // Compute dot products

        g0 = gridDotProduct(point, new Vec2D<int> (x0, y0));
        g1 = gridDotProduct(point, new Vec2D<int> (x1, y0));
        interp0 = interpolate(g0, g1, xInterp);

        g0 = gridDotProduct(point, new Vec2D<int> (x0, y1));
        g1 = gridDotProduct(point, new Vec2D<int> (x1, y1));
        interp1 = interpolate(g0, g1, xInterp);

        // Interpolate final result
        result = interpolate(interp0, interp1, yInterp);

        return (result + 1) / 2; // Put in range [0, 1]

    }

    private Vec2D<double>[] gradients;
    private void generateGradients(Random gen)
    {
        // Set up gradient grid (stored in an array of length width*height)
        gradients = new Vec2D<double>[(dimensions.x + 1) * (dimensions.y + 1)];

        // Initialize grid to vectors of length 1
        for (int i = 0; i < (dimensions.x + 1) * (dimensions.y + 1); i++)
        {
            double angle = gen.NextDouble() * 2 * Math.PI;
            gradients[i].x = Math.Cos(angle);
            gradients[i].y = Math.Sin(angle);
        }
}

    private double gridDotProduct(Vec2D<double> point, Vec2D<int> grid)
    {
        // Get gradient vector at grid point
        Vec2D<double> gradient = gradients[grid.y * (dimensions.x + 1) + grid.x];

        // Get offset vector from grid point to target point
        var offset = new Vec2D<double>(point.x -grid.x, point.y - grid.y);

        // Return dot product of offset vector and gradient
        return offset.x * gradient.x + offset.y * gradient.y;
    }

    private double interpolate(double a0, double a1, double w)
    {
        // Use the smoothstep interpolation
        return (a1 - a0) * (3.0 - w * 2.0) * w * w + a0;
    }

};
