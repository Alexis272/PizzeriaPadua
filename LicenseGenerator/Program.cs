using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string Key = "PizzeriaPaduaLicenseKey2026!Secure";

DateTime issuedAt = DateTime.UtcNow;
DateTime? expiryFromArg = null;
DateTime? expiryFromDays = null;
string outputPath = "license.lic";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--days":
        case "-d":
            if (i + 1 < args.Length && int.TryParse(args[++i], out var days))
                expiryFromDays = DateTime.UtcNow.AddDays(days);
            break;
        case "--expiry":
        case "-e":
            if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var exp))
                expiryFromArg = exp.Kind == DateTimeKind.Utc ? exp : exp.ToUniversalTime();
            break;
        case "--output":
        case "-o":
            if (i + 1 < args.Length)
                outputPath = args[++i];
            break;
        case "--issued":
        case "-i":
            if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var iss))
                issuedAt = iss.Kind == DateTimeKind.Utc ? iss : iss.ToUniversalTime();
            break;
    }
}

DateTime expiryDate = expiryFromArg ?? expiryFromDays ?? DateTime.UtcNow.AddDays(365);

if (expiryDate <= DateTime.UtcNow)
{
    Console.Error.WriteLine("Error: Expiry date must be in the future.");
    return 1;
}

string expiryStr = expiryDate.ToString("O");
string issuedStr = issuedAt.ToString("O");
string data = $"{expiryStr}|{issuedStr}";

byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
byte[] dataBytes = Encoding.UTF8.GetBytes(data);
byte[] hash = HMACSHA256.HashData(keyBytes, dataBytes);
string signature = Convert.ToBase64String(hash);

var license = new
{
    ExpiryDate = expiryStr,
    IssuedAt = issuedStr,
    Signature = signature
};

string fullPath = Path.GetFullPath(outputPath);
string? dir = Path.GetDirectoryName(fullPath);
if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    Directory.CreateDirectory(dir);

string json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(fullPath, json);

Console.WriteLine($"License generated: {fullPath}");
return 0;
