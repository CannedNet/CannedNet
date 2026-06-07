using System.Security.Cryptography;
using System.Text;

namespace CannedNet.Services;

public static class Signatures
{
    private static RSA? _rsa;
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            var pemPath = Path.Combine("Data", "PrivateKey.pem");
            if (!File.Exists(pemPath))
            {
                Console.WriteLine("sigs - creating new sig key");
                Directory.CreateDirectory("Data");
                using var newKey = RSA.Create(2048);
                var pem = newKey.ExportPkcs8PrivateKeyPem();
                File.WriteAllText(pemPath, pem);
                Console.WriteLine("sigs - key saved to " + pemPath);
            }

            var pemContent = File.ReadAllText(pemPath);
            _rsa = RSA.Create();
            _rsa.ImportFromPem(pemContent.ToCharArray());
            Console.WriteLine("sigs - key loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"sigs - failed to load key: {ex.Message}");
        }
    }
    
    public static RSA? GetRsaInstance()
    {
        if (!_initialized) Init();
        return _rsa;
    }

    public static string? Sign(byte[] data)
    {
        if (_rsa == null) return null;
        var signatureBytes = _rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }
}