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

                var privatePem = newKey.ExportPkcs8PrivateKeyPem();
                File.WriteAllText(pemPath, privatePem);

                var publicPem = newKey.ExportSubjectPublicKeyInfoPem();
                File.WriteAllText(
                    Path.Combine("Data", "PublicKey.pem"),
                    publicPem
                );

                Console.WriteLine("sigs - keys saved");
            }

            var pemContent = File.ReadAllText(pemPath);
            _rsa = RSA.Create();
            _rsa.ImportFromPem(pemContent.ToCharArray());
            Console.WriteLine("sigs - key loaded");

            RSAParameters p = _rsa.ExportParameters(false);

            string modulusBase64 = Convert.ToBase64String(p.Modulus!);
            string exponentBase64 = Convert.ToBase64String(p.Exponent!);

            Console.WriteLine($"Modulus:");
            Console.WriteLine(modulusBase64);
            Console.WriteLine("Make sure to replace the modulus in your clients global-metadata to this one or else the game will crash.");
            Console.WriteLine("Rec Room's modulus in metadata starts with ''")
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
