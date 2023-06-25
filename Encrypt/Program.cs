using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        // Đường dẫn tệp tin .exe đầu vào
        string inputFile = "Game Co Caro.exe";

        // Tạo khóa ngẫu nhiên
        byte[] key = GenerateRandomBytes(32); // 32 byte = 256 bit

        // Tạo một nonce (số duy nhất một lần)
        byte[] nonce = GenerateRandomBytes(12); // 12 byte = 96 bit 

        // Mở tệp tin đầu vào
        byte[] plaintext = File.ReadAllBytes(inputFile);

        // Mã hóa với AES-256-GCM
        byte[] ciphertext, tag;
        using (AesGcm aes = new AesGcm(key))
        {
            ciphertext = new byte[plaintext.Length];
            tag = new byte[16]; // 16 byte = 128 bit (độ dài mặc định của tag trong AES-GCM)
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // Ghi tệp tin mã hóa và tag vào tệp tin đầu ra
        File.WriteAllBytes("bin", ciphertext);
        // Chuyển đổi key, nonce và tag sang dạng Base64
        string keyBase64 = Convert.ToBase64String(key);
        string nonceBase64 = Convert.ToBase64String(nonce);
        string tagBase64 = Convert.ToBase64String(tag);

        // Dấu các giá trị key, nonce và tag vào icon của tệp tin đầu vào
        string outputIconFile = "output.png";
        EmbedDataInIcon(inputFile, outputIconFile, keyBase64, nonceBase64, tagBase64);

        Console.WriteLine("Encryption done!");
    }

    static byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return randomBytes;
    }
    static void EmbedDataInIcon(string inputFile, string outputIconFile, string keyBase64, string nonceBase64, string tagBase64)
    {
        // Trích xuất icon từ tệp tin .exe
        Icon icon = Icon.ExtractAssociatedIcon(inputFile);

        // Ghép các chuỗi Base64 thành một chuỗi duy nhất
        string message = $"{keyBase64}|{nonceBase64}|{tagBase64}\0";
        // Kiểm tra kích thước dữ liệu cần dấu có vượt quá giới hạn của icon hay không
        int maxDataLength = icon.Width * icon.Height * 3 / 8; // Giới hạn dữ liệu dấu là 3/8 kích thước icon
        if (message.Length > maxDataLength)
        {
            throw new Exception("Message lagger than icon!!");
        }

        // Chuyển icon thành bitmap để thực hiện các thao tác pixel
        Bitmap bitmap = icon.ToBitmap();

        // Dấu dữ liệu vào bitmap
        int bitCount = 0;
        int dataIndex = 0;
        for (int j = 0; j < bitmap.Height; j++)
        {
            for (int i = 0; i < bitmap.Width; i++)
            {
                Color pixelColor = bitmap.GetPixel(i, j);
                for (int color = 0; color < 3; color++)
                {
                    if (dataIndex < message.Length)
                    {
                        byte value = (byte)message[dataIndex];

                        if (IsBitSet(value, 7 - bitCount))
                        {
                            pixelColor = SetLSB(pixelColor, true, color);
                        }
                        else
                        {
                            pixelColor = SetLSB(pixelColor, false, color);
                        }

                        bitCount++;

                        if (bitCount == 8)
                        {
                            bitCount = 0;
                            dataIndex++;
                        }
                    }
                    bitmap.SetPixel(i, j, pixelColor);
                }
            }
        }
        bitmap.Save(outputIconFile);
    }

    static bool IsBitSet(byte value, int pos)
    {
        value = (byte)(value >> pos);
        return (value & 1) == 1;
    }

    static Color SetLSB(Color pixel, bool bitValue, int color)
    {
        int lsb = bitValue ? 1 : 0;
        int r = pixel.R;
        int g = pixel.G;
        int b = pixel.B;
        switch (color)
        {
            case 0:
                r = (pixel.R & ~1) | lsb;
                break;
            case 1:
                g = (pixel.G & ~1) | lsb;
                break;
            case 2:
                b = (pixel.B & ~1) | lsb;
                break;
        }
        return Color.FromArgb(pixel.A, r, g, b);
    }
}
