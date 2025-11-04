using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Memorizer.IntegrationTests.Services;

public class MultiModalServiceTests
{
    private readonly MultiModalSettings _settings;
    private readonly Mock<ILogger<MultiModalService>> _mockLogger;
    private readonly ITestOutputHelper _output;

    public MultiModalServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _settings = new MultiModalSettings
        {
            ApiUrl = new Uri("http://192.168.0.50:1234"),
            Model = "qwen/qwen3-vl-8b",
            Temperature = 0.2,
            MaxTokens = 1000,
            Timeout = TimeSpan.FromMinutes(2)
        };

        _mockLogger = new Mock<ILogger<MultiModalService>>();
    }

    [Fact]
    public async Task AnalyzeImage_WithTriangle_ShouldRecognizeShape()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new MultiModalService(httpClient, _settings, _mockLogger.Object);

        // Create a simple triangle image
        var imageData = CreateTriangleImage();

        // Act
        var result = await service.AnalyzeImageAsync(
            imageData,
            "이 이미지에 뭐가 보이나요?",
            "png"
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        _output.WriteLine("Response from MultiModalService:");
        _output.WriteLine(result);

        // Check if the response mentions triangle in any form
        var lowerResult = result.ToLower();
        Assert.True(
            lowerResult.Contains("삼각형") ||
            lowerResult.Contains("triangle") ||
            lowerResult.Contains("세모"),
            $"Expected response to mention triangle/삼각형, but got: {result}"
        );
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthStatus()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new MultiModalService(httpClient, _settings, _mockLogger.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Message);
        Assert.NotNull(result.ModelName);
        Assert.Equal(_settings.Model, result.ModelName);
    }

    /// <summary>
    /// Creates a simple triangle image using basic drawing with System.Drawing primitives
    /// This creates a minimal PNG with a blue triangle
    /// </summary>
    private static byte[] CreateTriangleImage()
    {
        // Simple 100x100 PNG with a blue triangle
        // This is a minimal hand-crafted PNG for testing purposes
        // Using a simple base64 encoded triangle PNG (created externally)

        // Create a simple triangle using raw bitmap data
        const int width = 100;
        const int height = 100;

        // Create RGBA bitmap data (white background with blue triangle)
        var pixels = new byte[width * height * 4];

        // Fill with white background (255, 255, 255, 255)
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;     // R
            pixels[i + 1] = 255; // G
            pixels[i + 2] = 255; // B
            pixels[i + 3] = 255; // A
        }

        // Draw a simple triangle (blue color: 0, 0, 255)
        // Triangle points: top (50, 20), bottom-left (20, 80), bottom-right (80, 80)
        for (int y = 20; y <= 80; y++)
        {
            // Calculate the left and right bounds of the triangle at this y coordinate
            int leftX, rightX;

            if (y <= 50)
            {
                // Upper half of triangle
                leftX = 50 - (y - 20) * 30 / 30;
                rightX = 50 + (y - 20) * 30 / 30;
            }
            else
            {
                // Lower half of triangle (maintaining constant width)
                leftX = 20;
                rightX = 80;
            }

            // Fill the row with blue pixels
            for (int x = Math.Max(0, leftX); x <= Math.Min(width - 1, rightX); x++)
            {
                int index = (y * width + x) * 4;
                if (index >= 0 && index < pixels.Length - 3)
                {
                    pixels[index] = 0;       // R
                    pixels[index + 1] = 0;   // G
                    pixels[index + 2] = 255; // B (blue)
                    pixels[index + 3] = 255; // A
                }
            }
        }

        // Convert to PNG format using a simple PNG encoder
        return EncodeToPng(pixels, width, height);
    }

    /// <summary>
    /// Simple PNG encoder for testing purposes
    /// </summary>
    private static byte[] EncodeToPng(byte[] rgbaData, int width, int height)
    {
        using var ms = new MemoryStream();

        // PNG signature
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);

        // IHDR chunk
        WriteChunk(ms, "IHDR", writer =>
        {
            writer.Write(ToBigEndian(width));
            writer.Write(ToBigEndian(height));
            writer.Write((byte)8);  // bit depth
            writer.Write((byte)6);  // color type (RGBA)
            writer.Write((byte)0);  // compression
            writer.Write((byte)0);  // filter
            writer.Write((byte)0);  // interlace
        });

        // IDAT chunk (image data)
        var imageData = new List<byte>();
        for (int y = 0; y < height; y++)
        {
            imageData.Add(0); // filter type for this scanline
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                imageData.Add(rgbaData[idx]);     // R
                imageData.Add(rgbaData[idx + 1]); // G
                imageData.Add(rgbaData[idx + 2]); // B
                imageData.Add(rgbaData[idx + 3]); // A
            }
        }

        // Compress with zlib
        var compressed = Compress(imageData.ToArray());
        WriteChunk(ms, "IDAT", writer => writer.Write(compressed));

        // IEND chunk
        WriteChunk(ms, "IEND", writer => { });

        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, Action<BinaryWriter> writeData)
    {
        using var dataStream = new MemoryStream();
        using var dataWriter = new BinaryWriter(dataStream);

        // Write chunk data
        writeData(dataWriter);
        var data = dataStream.ToArray();

        // Write length
        var lengthBytes = ToBigEndian(data.Length);
        stream.Write(lengthBytes, 0, 4);

        // Write type
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes, 0, 4);

        // Write data
        stream.Write(data, 0, data.Length);

        // Write CRC
        var crc = CalculateCrc(typeBytes, data);
        var crcBytes = ToBigEndian((int)crc);
        stream.Write(crcBytes, 0, 4);
    }

    private static byte[] ToBigEndian(int value)
    {
        return new[]
        {
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        };
    }

    private static uint CalculateCrc(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in type.Concat(data))
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            deflate.Write(data, 0, data.Length);
        }

        var compressed = output.ToArray();

        // Add zlib header and checksum
        using var result = new MemoryStream();
        result.WriteByte(0x78); // CMF
        result.WriteByte(0x9C); // FLG
        result.Write(compressed, 0, compressed.Length);

        // Adler32 checksum
        uint adler = Adler32(data);
        result.Write(ToBigEndian((int)adler), 0, 4);

        return result.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var by in data)
        {
            a = (a + by) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }
}
