/* The Computer Language Benchmarks Game
   http://shootout.alioth.debian.org/

   contributed by Isaac Gouy, optimization and use of more C# idioms by Robert F. Tobler
*/

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    // HACK: Scale and truncate before comparing
    const double Scale = 1000000000000000;

    [Export]
    public static double InitialEnergy { get; set; }
    [Export]
    public static double FinalEnergy { get; set; }

    public static void Main () {
        SetHeapSize(8192);

        Invoke("Test");

        const double initial = -169075163828525;
        const double final   = -169016441264431;

        AssertEq(initial, "get_InitialEnergy");
        AssertEq(final,   "get_FinalEnergy");
    }

    [Export]
    public static void Test () {
        const int n = 10000;
        NBodySystem.Initialize();
    
        InitialEnergy = Math.Floor(NBodySystem.Energy() * Scale);
      
        for (int i = 0; i < n; i++)
            NBodySystem.Advance(0.01);
      
        FinalEnergy   = Math.Floor(NBodySystem.Energy() * Scale);
    }
}

public struct Body { 
    public double x, y, z, vx, vy, vz, mass;
}

public unsafe struct Pair { 
    public Body* bi, bj; 
}

public static unsafe class NBodySystem {
    const int BodyCount = 5;
    const int PairCount = BodyCount * (BodyCount - 1) / 2;

    const double Pi = 3.141592653589793;
    const double Solarmass = 4 * Pi * Pi;
    const double DaysPeryear = 365.24;

    public static Body* bodies;
    public static Pair* pairs;

    private static void InitBody (
        int index,
        double x = 0,
        double y = 0,
        double z = 0,
        double vx = 0,
        double vy = 0,
        double vz = 0,
        double mass = 0
    ) {
        var body = &bodies[index];
        body->x  = x;
        body->y  = y;
        body->z  = z;
        body->vx = vx;
        body->vy = vy;
        body->vz = vz;
        body->mass = mass;
    }

    public static void Initialize () {
        bodies = (Body*)(U8.Base);
        pairs  = (Pair*)(&bodies[BodyCount]);

        InitBody(
            0, // Sun
            mass: Solarmass
        );

        InitBody(
            1, // Jupiter
            x:    4.84143144246472090e+00,
            y:   -1.16032004402742839e+00,
            z:   -1.03622044471123109e-01,
            vx:   1.66007664274403694e-03 * DaysPeryear,
            vy:   7.69901118419740425e-03 * DaysPeryear,
            vz:  -6.90460016972063023e-05 * DaysPeryear,
            mass: 9.54791938424326609e-04 * Solarmass
        );

        InitBody(
            2, // Saturn
            x:    8.34336671824457987e+00,
            y:    4.12479856412430479e+00,
            z:   -4.03523417114321381e-01,
            vx:  -2.76742510726862411e-03 * DaysPeryear,
            vy:   4.99852801234917238e-03 * DaysPeryear,
            vz:   2.30417297573763929e-05 * DaysPeryear,
            mass: 2.85885980666130812e-04 * Solarmass
        );

        InitBody(
            3, // Uranus
            x:    1.28943695621391310e+01,
            y:   -1.51111514016986312e+01,
            z:   -2.23307578892655734e-01,
            vx:   2.96460137564761618e-03 * DaysPeryear,
            vy:   2.37847173959480950e-03 * DaysPeryear,
            vz:  -2.96589568540237556e-05 * DaysPeryear,
            mass: 4.36624404335156298e-05 * Solarmass
        );

        InitBody(
            4, // Neptune
            x:    1.53796971148509165e+01,
            y:   -2.59193146099879641e+01,
            z:    1.79258772950371181e-01,
            vx:   2.68067772490389322e-03 * DaysPeryear,
            vy:   1.62824170038242295e-03 * DaysPeryear,
            vz:  -9.51592254519715870e-05 * DaysPeryear,
            mass: 5.15138902046611451e-05 * Solarmass
        );

        int pi = 0;
        for (int i = 0; i < BodyCount - 1; i++) {
            for (int j = i + 1; j < BodyCount; j++) {
                var p = &pairs[pi++];
                p->bi = &bodies[i];
                p->bj = &bodies[j];
            }
        }

        double px = 0.0, py = 0.0, pz = 0.0;
        for (int i = 0; i < BodyCount; i++) {
            var b = &(bodies[i]);
            px += b->vx * b->mass; 
            py += b->vy * b->mass; 
            pz += b->vz * b->mass;
        }

        var sol = &bodies[0];
        sol->vx = -px / Solarmass; 
        sol->vy = -py / Solarmass; 
        sol->vz = -pz / Solarmass;
    }

    public static void Advance (double dt) {
        for (int i = 0; i < PairCount; i++) {
            var p = &pairs[i];
            var bi = p->bi;
            var bj = p->bj;

            double dx = bi->x - bj->x, 
                dy = bi->y - bj->y, 
                dz = bi->z - bj->z;
            double d2 = dx * dx + dy * dy + dz * dz;
            double mag = dt / (d2 * Math.Sqrt(d2));
            bi->vx -= dx * bj->mass * mag; 
            bj->vx += dx * bi->mass * mag;
            bi->vy -= dy * bj->mass * mag; 
            bj->vy += dy * bi->mass * mag;
            bi->vz -= dz * bj->mass * mag; 
            bj->vz += dz * bi->mass * mag;
        }

        for (int i = 0; i < BodyCount; i++) {
            var b = &bodies[i];
            b->x += dt * b->vx; 
            b->y += dt * b->vy; 
            b->z += dt * b->vz;
        }
    }

    public static double Energy () {
        double e = 0.0;
        for (int i = 0; i < BodyCount; i++) {
            var bi = &bodies[i];

            e += 0.5 * bi->mass * 
                (bi->vx * bi->vx + 
                 bi->vy * bi->vy + 
                 bi->vz * bi->vz);

            for (int j = i + 1; j < BodyCount; j++) {
                var bj = &bodies[j];

                double dx = bi->x - bj->x,
                    dy = bi->y - bj->y, 
                    dz = bi->z - bj->z;
                e -= (bi->mass * bj->mass) / 
                    Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }
        return e;
    }
}