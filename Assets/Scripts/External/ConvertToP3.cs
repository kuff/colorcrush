using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvertToP3 : MonoBehaviour
{
    Color P3Colour;

    float[,] XYZTosRGBD65Matrix = { { 3.2404542f, -1.5371385f, -0.4985314f }, 
                                    { -0.9692660f, 1.8760108f, 0.0415560f }, 
                                    { 0.0556434f, -0.2040259f, 1.0572252f } };//bruce
    float[,] sRGBToP3D65Matrix = { { 0.8225f, 0.1774f, 0.0000f },
                                   { 0.0332f, 0.9669f, 0.0000f },
                                   { 0.0171f, 0.0724f, 0.9108f } };//endavid


    public Color Convert(Vector3 xyY)
    {
        //Vector3 XYZ = ConvertxyYToXYZ(coordinate, luminance);
        Vector3 sRGB = ConvertXYZTosRGB(xyY, XYZTosRGBD65Matrix);
        Vector3 gammaMinus = GammaCorrectionMinus(sRGB);
        Vector3 P3 = ConvertsRGBToP3(gammaMinus, sRGBToP3D65Matrix);
        Vector3 P3GammaAdded = GammaCorrectionPlus(P3);
        Color P3Colour = new Color(P3GammaAdded[0], P3GammaAdded[1], P3GammaAdded[2]);

        return P3Colour;
    }

    Vector3 ConvertxyYToXYZ(Vector2 xy, float Y)
    {
        if (xy[1] == 0)
        {
            return new Vector3(0, 0, 0);
        }
        float X = (xy[0] * Y) / xy[1];
        float Z = ((1 - xy[0] - xy[1]) * Y) / xy[1];
        Vector3 XYZ = new Vector3(X, Y, Z);
        return XYZ;
    }
    Vector3 ConvertXYZTosRGB(Vector3 XYZ, float[,] matrix)
    {
        float r = XYZ[0] * matrix[0, 0] + XYZ[1] * matrix[0, 1] + XYZ[2] * matrix[0, 2];
        float g = XYZ[0] * matrix[1, 0] + XYZ[1] * matrix[1, 1] + XYZ[2] * matrix[1, 2];
        float b = XYZ[0] * matrix[2, 0] + XYZ[1] * matrix[2, 1] + XYZ[2] * matrix[2, 2];
        Vector3 sRGB = new Vector3(r, g, b);
        
        return sRGB;
    }
    Vector3 GammaCorrectionMinus(Vector3 sRGB)
    {
        for(int i = 0; i < 3; i++)
        {
            if (sRGB[i] < 0.040045f)
            {
                sRGB[i] = sRGB[i] / 12.92f;
            }
            else
            {
                sRGB[i] = Mathf.Pow((sRGB[i]+0.055f)/1.055f, 2.4f);
            }
        }

        return sRGB;
    }
    Vector3 GammaCorrectionPlus(Vector3 sRGB)
    {
        for (int i = 0; i < 3; i++)
        {
            if (sRGB[i] < 0.0031308f)
            {
                sRGB[i] = sRGB[i] * 12.92f;
            }
            else
            {
                sRGB[i] = (Mathf.Pow(sRGB[i], (1f / 2.4f)) * 1.055f) - 0.055f;
            }
        }

        return sRGB;
    }
    Vector3 ConvertsRGBToP3(Vector3 XYZ, float[,] matrix)
    {
        float r = XYZ[0] * matrix[0, 0] + XYZ[1] * matrix[0, 1] + XYZ[2] * matrix[0, 2];
        float g = XYZ[0] * matrix[1, 0] + XYZ[1] * matrix[1, 1] + XYZ[2] * matrix[1, 2];
        float b = XYZ[0] * matrix[2, 0] + XYZ[1] * matrix[2, 1] + XYZ[2] * matrix[2, 2];
        Vector3 RGB = new Vector3(r, g, b);

        return RGB;
    }
}
