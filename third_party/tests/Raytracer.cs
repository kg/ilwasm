// ncbray/wassembler demos/raytrace.wasm

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public struct Vec3f {
  public float x, y, z;
}

public static unsafe class Raytrace {
  public const int TargaHeaderSize = 18;
  public const int BytesPerPixel = 3;

  public static int  width, height;
  public static int* frame_buffer;
  public static int  phase;

  // TODO: Struct fields
  public static Vec3f* 
    pos, 
    dir, 
    light, 
    half, 
    color;
  // TODO: Single Intersection { Vec3f, Vec3f } struct
  public static Vec3f* 
    intersection_pos,
    intersection_normal;

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

  public static float fsqrt (float v) {
    return (float)Math.Sqrt(v);
  }

  // Convert a linear color value to a gamma-space byte.
  // Square root approximates gamma-correct rendering.
  public static int l2g (float v) {
    return f2b(fsqrt(v));
  }

  public static int packColor (float r, float g, float b) {
    return l2g(b) << 16 | l2g(g) << 8 | l2g(r);
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

  [Export]
  private static void emitTargaHeader (int offset) {
      var ptr = &U8.Base[offset];

      // ID length
      *ptr++ = 0;

      // Colormap type - none
      *ptr++ = 0;

      // Image type - uncompressed truecolor
      *ptr++ = 2;

      // Colormap specification
      for (var j = 0; j < 5; j++)
        *ptr++ = 0;

      // Image specification

      // X origin
      *ptr++ = 0;
      *ptr++ = 0;

      // Y origin
      *ptr++ = 0;
      *ptr++ = 0;

      // Width
      *ptr++ = (byte)width;
      width = width >> 8;
      *ptr++ = (byte)width;

      // Height
      *ptr++ = (byte)height;
      height = height >> 8;
      *ptr++ = (byte)height;

      // Bits per pixel
      *ptr++ = BytesPerPixel * 8;

      // Image descriptor [0..3] = alpha depth, [4..5] direction
      *ptr++ = 0;
  }

  [Export]
  public static int checksum (int pPixels, int numBytes) {
    var pixels = (int*)&(U8.Base[pPixels]);
    var numPixels = numBytes / BytesPerPixel;
    var lastPixel = &pixels[numPixels];
    int sum = 0;

    while (pixels < lastPixel) {
      sum += *pixels;
      pixels++;
    }

    return sum;
  }

  [Export]
  public static void init (int w, int h, int pFrameBuffer) {
    width = w;
    height = h;
    frame_buffer = (int*)&(U8.Base[pFrameBuffer]);
  }
}

public static class Program {
  public static void Main () {
    SetHeapSize(128 * 1024);

    const int width = 32;
    const int height = 32;
    const int heapOffset = 0;

    const int expectedSize = width * height * Raytrace.BytesPerPixel;
    // FIXME
    const int expectedChecksum = 1234567;

    Invoke("init", width, height, heapOffset + Raytrace.TargaHeaderSize);
    Invoke("emitTargaHeader", heapOffset);
    // Invoke("renderFrame");
    AssertEq(expectedChecksum, "checksum", heapOffset + Raytrace.TargaHeaderSize, expectedSize);
    AssertHeapEqFile(heapOffset, Raytrace.TargaHeaderSize + expectedSize, "raytraced.tga");
  }
}

/*
func intersect(pos i32, dir i32, intersection i32) i32 {
  var px f32 = loadF32(pos);
  var py f32 = loadF32(pos+4);
  var pz f32 = loadF32(pos+8);

  var vx f32 = loadF32(dir);
  var vy f32 = loadF32(dir + 4);
  var vz f32 = loadF32(dir + 8);


  // The sphere.
  var radius f32 = 4.0f;
  var cx f32 = 0.0f;
  var cy f32 = sinF32(loadF32(phase));
  var cz f32 = -6.0f;

  // Calculate the position relative to the center of the sphere.
  var ox f32 = px - cx;
  var oy f32 = py - cy;
  var oz f32 = pz - cz;

  var dot f32 = vx * ox + vy * oy + vz * oz;

  var partial f32 = dot * dot + radius * radius - (ox * ox + oy * oy + oz * oz);
  if (partial >= 0.0f) {
    var d f32 = -dot - sqrtF32(partial);
    if (d >= 0.0f) {
      var normal i32 = intersection + 12;
      vecStore(px + vx * d - cx, py + vy * d - cy, pz + vz * d - cz, normal);
      vecNormalize(normal);
      return 1;
    }
  }
  return 0;
}

func renderFrame() i32 {
  var w i32 = loadI32(width);
  var h i32 = loadI32(height);
  var buffer i32 = loadI32(frame_buffer);

  vecStore(20.0f, 20.0f, 15.0f, light);
  vecNormalize(light);

  var j i32 = 0;
  while (j < h) {
    var y f32 = 0.5f - f32(j) / f32(h);
    var i i32 = 0;
    while (i < w) {
      var x f32 = f32(i) / f32(w) - 0.5f;
      vecStore(x, y, 0.0f, pos);
      vecStore(x, y, -0.5f, dir);
      vecNormalize(dir);

      // Compute the half vector;
      vecScale(dir, -1.0f, half);
      vecAdd(half, light, half);
      vecNormalize(half);

      // Light accumulation
      var r f32 = 0.0f;
      var g f32 = 0.0f;
      var b f32 = 0.0f;

      // Surface diffuse.
      var dr f32 = 0.7f;
      var dg f32 = 0.7f;
      var db f32 = 0.7f;

      if (intersect(pos, dir, intersection)) {
        sampleEnv(intersection + 12, color);
        var ambientScale f32 = 0.2f;
        r = r + dr * loadF32(color) * ambientScale;
        g = g + dg * loadF32(color + 4) * ambientScale;
        b = b + db * loadF32(color + 8) * ambientScale;

        var diffuse f32 = vecNLDot(intersection + 12, light);
        var specular f32 = vecNLDot(intersection + 12, half);
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
        r = loadF32(color);
        g = loadF32(color + 4);
        b = loadF32(color + 8);
      }
      storeI32(buffer + (j * w + i) * 4, packColor(r, g, b, 1.0f));
      i = i + 1;
    }
    j = j + 1;
  }
  return buffer;
}

*/