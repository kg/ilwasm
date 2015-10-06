//#use lib/Targa.cs

// ncbray/wassembler demos/raytrace.wasm

using System;
using System.Runtime.InteropServices;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

[StructLayout(LayoutKind.Sequential, Pack=1)]
public struct Vec3f {
  public float x, y, z;
}

public static unsafe class Raytrace {
  public const int TargaHeaderSize = 18;
  public const int BytesPerPixel = 3;

  private static int   width, height;
  private static byte* frame_buffer;

  // TODO: Implement ref/out and turn these into struct fields
  private static Vec3f*
    pos, dir,
    light, half,
    color, intersection_normal;

  // Convert [0.0, 1.0] to [0, 255].
  public static int f2b (float v) {
    var vi = (int)(v * 255.0f);
    if (vi < 0)
      return 0;
    else if (vi > 255)
      return 255;
    else
      return vi;
  }

  public static float fsin (float v) {
    if (v < 0.5f)
      return -16.0f * v * v + 8.0f * v;
    else
      return 16.0f * v * v - 24.0f * v + 8.0f;
  }

  public static float fsqrt (float v) {
    return (float)Math.Sqrt(v);
  }

  // Convert a linear color value to a gamma-space byte.
  // Square root approximates gamma-correct rendering.
  public static int l2g (float v) {
    return f2b(fsqrt(v));
  }

  public static void storeColor (int x, int y, float r, float g, float b) {
    // Invert y
    y = height - y - 1;

    var index = (x + (y * width)) * BytesPerPixel;
    var ptr = &frame_buffer[index];

    // Reversed r/g/b order
    ptr[2] = (byte)l2g(r);
    ptr[1] = (byte)l2g(g);
    ptr[0] = (byte)l2g(b);
  }

  public static void vecStore (float x, float y, float z, Vec3f* ptr) {
    ptr->x = x;
    ptr->y = y;
    ptr->z = z;
  }

  public static void vecAdd (Vec3f* a, Vec3f* b, Vec3f* ptr) {
    vecStore(
      a->x + b->x, 
      a->y + b->y, 
      a->z + b->z, 
      ptr
    );    
  }

  public static void vecScale (Vec3f* a, float scale, Vec3f* ptr) {
    vecStore(
      a->x * scale, 
      a->y * scale, 
      a->z * scale, 
      ptr
    );    
  }

  public static void vecNormalize (Vec3f* ptr) {
    var x = ptr->x;
    var y = ptr->y;
    var z = ptr->z;

    float invLen = 1.0f / fsqrt((x * x) + (y * y) + (z * z));
    vecStore(x * invLen, y * invLen, z * invLen, ptr);
  }

  public static float vecLen (Vec3f* ptr) {
    var x = ptr->x;
    var y = ptr->y;
    var z = ptr->z;

    return fsqrt((x * x) + (y * y) + (z * z));
  }

  public static float vecDot (Vec3f* a, Vec3f* b) {
    return (a->x * b->x) + (a->y * b->y) + (a->z * b->z);
  }

  public static float vecNLDot (Vec3f* a, Vec3f* b) {
    var value = vecDot(a, b);
    if (value < 0)
      return 0;
    else
      return value;
  }

  public static void sampleEnv (Vec3f* dir, Vec3f* ptr) {
    var y = dir->y;
    var amt = y * 0.5f + 0.5f;
    var keep = 1.0f - amt;
    vecStore(
      keep * 0.1f + amt * 0.1f, 
      keep * 1.0f + amt * 0.1f, 
      keep * 0.1f + amt * 1.0f, 
      ptr
    );
  }

  public static bool intersect () {
    var px = pos->x;
    var py = pos->y;
    var pz = pos->z;

    var vx = dir->x;
    var vy = dir->y;
    var vz = dir->z;

    // The sphere.
    var radius = 4.0f;
    var cx = 0.0f;
    var cy = 0; // fsin(phase);
    var cz = -6.0f;

    // Calculate the position relative to the center of the sphere.
    var ox = px - cx;
    var oy = py - cy;
    var oz = pz - cz;

    var dot = vx * ox + vy * oy + vz * oz;

    var partial = dot * dot + radius * radius - (ox * ox + oy * oy + oz * oz);
    if (partial >= 0.0f) {
      var d = -dot - fsqrt(partial);

      if (d >= 0.0f) {
        vecStore(px + vx * d - cx, py + vy * d - cy, pz + vz * d - cz, intersection_normal);
        vecNormalize(intersection_normal);
        return true;
      }
    }

    return false;
  }

  [Export]
  public static void renderFrame () {
    var w = width;
    var h = height;

    vecStore(20.0f, 20.0f, 15.0f, light);
    vecNormalize(light);

    var j = 0;
    while (j < h) {
      var y = 0.5f - (float)(j) / (float)(h);

      var i = 0;
      while (i < w) {
        var x = (float)(i) / (float)(w) - 0.5f;
        vecStore(x, y, 0.0f, pos);
        vecStore(x, y, -0.5f, dir);
        vecNormalize(dir);

        // Compute the half vector;
        vecScale(dir, -1.0f, half);
        vecAdd(half, light, half);
        vecNormalize(half);

        // Light accumulation
        var r = 0.0f;
        var g = 0.0f;
        var b = 0.0f;

        // Surface diffuse.
        var dr = 0.7f;
        var dg = 0.7f;
        var db = 0.7f;

        if (intersect()) {
          sampleEnv(intersection_normal, color);

          const float ambientScale = 0.2f;
          r = r + dr * color->x * ambientScale;
          g = g + dg * color->y * ambientScale;
          b = b + db * color->z * ambientScale;

          var diffuse = vecNLDot(intersection_normal, light);
          var specular = vecNLDot(intersection_normal, half);

          // Take it to the 64th power, manually.
          specular = specular * specular;
          specular = specular * specular;
          specular = specular * specular;
          specular = specular * specular;
          specular = specular * specular;
          specular = specular * specular;

          specular = specular * 0.6f;

          r = r + dr * diffuse + specular;
          g = g + dg * diffuse + specular;
          b = b + db * diffuse + specular;
        } else {
          sampleEnv(dir, color);
          r = color->x;
          g = color->y;
          b = color->z;
        }

        storeColor(i, j, r, g, b);

        i = i + 1;
      }

      j = j + 1;
    }
  }

  [Export]
  public static void init (int w, int h, int pFrameBuffer) {
    width = w;
    height = h;
    frame_buffer = &U8.Base[pFrameBuffer];

    var pScratch = pFrameBuffer + (width * height * BytesPerPixel) + 1024;
    var scratch = (Vec3f*)&U8.Base[pScratch];

    // FIXME
    pos   = &scratch[0];
    dir   = &scratch[1];
    light = &scratch[2];
    half  = &scratch[3];
    color = &scratch[4];
    intersection_normal = &scratch[5];
  }
}

public static class Program {
  public static void Main () {
    SetHeapSize(64 * 1024);

    const int width = 16;
    const int height = 16;
    const int heapOffset = 0;

    const int expectedSize = width * height * Raytrace.BytesPerPixel;

    Invoke("init", width, height, heapOffset + Raytrace.TargaHeaderSize);
    AssertEq(18, "emitTargaHeader", heapOffset, width, height);
    Invoke("renderFrame");
    AssertHeapEqFile(heapOffset, Raytrace.TargaHeaderSize + expectedSize, "raytraced.tga");
  }
}