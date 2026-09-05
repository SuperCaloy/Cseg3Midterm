using UnityEngine;
using UnityEditor;
using System.IO;

public static class TextureGenerator
{
    public static Texture2D GenerateFractalNoise(string name, int width, int height, float scale, int octaves, float persistence, float lacunarity, Color color1, Color color2)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");

        string path = $"Assets/Textures/{name}.png";
        
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        float xOffset = Random.Range(0f, 9999f);
        float yOffset = Random.Range(0f, 9999f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = x / scale * frequency + xOffset;
                    float sampleY = y / scale * frequency + yOffset;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                
                // Normalize to 0-1
                float normalizedHeight = Mathf.InverseLerp(-1.5f, 1.5f, noiseHeight);
                Color pixelColor = Color.Lerp(color1, color2, normalizedHeight);
                
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        
        AssetDatabase.ImportAsset(path);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
