using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SecurePrefs
{
    private const string SecurePrefix = "cdr.secure.";
    private const string SecretPepper = "CyberDriftRunner.SecurePrefs.v2";

    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(BuildSecureKey(key)) || PlayerPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(BuildSecureKey(key));
        PlayerPrefs.DeleteKey(key);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(BuildSecureKey(key), Protect(value ?? string.Empty));
    }

    public static string GetString(string key, string defaultValue = "")
    {
        string secureKey = BuildSecureKey(key);
        if (PlayerPrefs.HasKey(secureKey))
        {
            string protectedValue = PlayerPrefs.GetString(secureKey, string.Empty);
            if (TryUnprotect(protectedValue, out string decrypted))
            {
                return decrypted;
            }

            PlayerPrefs.DeleteKey(secureKey);
            PlayerPrefs.Save();
            return defaultValue;
        }

        if (PlayerPrefs.HasKey(key))
        {
            string legacyValue = PlayerPrefs.GetString(key, defaultValue);
            SetString(key, legacyValue);
            PlayerPrefs.Save();
            return legacyValue;
        }

        return defaultValue;
    }

    public static void SetInt(string key, int value)
    {
        SetString(key, value.ToString(CultureInfo.InvariantCulture));
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (!HasKey(key))
        {
            return defaultValue;
        }

        string value = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : defaultValue;
    }

    public static void SetFloat(string key, float value)
    {
        SetString(key, value.ToString(CultureInfo.InvariantCulture));
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (!HasKey(key))
        {
            return defaultValue;
        }

        string value = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture));
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : defaultValue;
    }

    public static void SetBool(string key, bool value)
    {
        SetInt(key, value ? 1 : 0);
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        return GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    private static string BuildSecureKey(string key)
    {
        return SecurePrefix + key;
    }

    private static string Protect(string plainText)
    {
        byte[] iv = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        byte[] encryptedBytes;
        using (Aes aes = Aes.Create())
        {
            aes.Key = DeriveKey("enc");
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] input = Encoding.UTF8.GetBytes(plainText);
            encryptedBytes = encryptor.TransformFinalBlock(input, 0, input.Length);
        }

        byte[] signedPayload = Combine(iv, encryptedBytes);
        byte[] mac;
        using (HMACSHA256 hmac = new HMACSHA256(DeriveKey("mac")))
        {
            mac = hmac.ComputeHash(signedPayload);
        }

        return $"{Convert.ToBase64String(iv)}.{Convert.ToBase64String(encryptedBytes)}.{Convert.ToBase64String(mac)}";
    }

    private static bool TryUnprotect(string protectedText, out string plainText)
    {
        plainText = string.Empty;
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return false;
        }

        string[] parts = protectedText.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] encryptedBytes = Convert.FromBase64String(parts[1]);
            byte[] mac = Convert.FromBase64String(parts[2]);
            byte[] payload = Combine(iv, encryptedBytes);
            byte[] computedMac;
            using (HMACSHA256 hmac = new HMACSHA256(DeriveKey("mac")))
            {
                computedMac = hmac.ComputeHash(payload);
            }

            if (!FixedTimeEquals(mac, computedMac))
            {
                return false;
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey("enc");
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                plainText = Encoding.UTF8.GetString(decrypted);
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static byte[] DeriveKey(string purpose)
    {
        using SHA256 sha = SHA256.Create();
        string seed = $"{Application.companyName}|{Application.productName}|{Application.identifier}|{purpose}|{SecretPepper}";
        return sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, combined, 0, first.Length);
        Buffer.BlockCopy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int diff = 0;
        for (int i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }
}
