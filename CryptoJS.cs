using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

public class CryptoJS
{
    private static string PUBLIC_KEY = "JH23IH12IJH3J1K2H3J12G43HJG2REHJG234JHG34JHG43JHG342JHG243HJG234JHG324HJ3G42HJ3G42JH432GJH234G23J4HG234JHG432JHG342HJG432JH324GJH234GJ243HG243JHG234JHG234JHG234JHERGWJHERGWJHWGRJHGWEJHRGWE";
    private static string PUBLIC_IV = "JREKEWRKJHK3J24HKJ3H4KJH324KJH234KJ234HKJ234HKJ324HKJ243H23KJ4HK24J3HJKK4J67HKJ756HKJH4TYIUGDFHIUGDFHIUFDGSSGDHFJHG45JHG56JYGUWRETYSGFDHGSRDUYTWERUH4B23NJBJH4EGJWEHRBJWERGJHWEGRJH45KHJ34KJ5";
    private static string CONCATENATE_PUBLIC_KEY = "EKWJRJREHJHWERKJWEHKJERWHKJWREHKJWERHWKEJRHKWJERHWEJKRHWKEJRHWERJK";
    private static string CONCATENATE_PUBLIC_IV = "EKRJREKJHHWJEHRKJWEHRKJWHERKJHWEKRJHWEKJHRKJWEHRJKHWERKJHWEKJRHKWJEHR";
    private static string HEADER_KEY = "EKRJEKRWRELKHWKEJRHWERKJHWERKJHERKJHWERKJHWEKRJHWEKJRHKWJERHKJWEHRKJWEHRKJWEHRJKWHERKJHWERGRGRGRGRGRGRGEHJREJR";

    public static string DecryptStringAES(string encryptedValue, string xChk)
    {
        try
        {
            var keybytes = Encoding.UTF8.GetBytes(PUBLIC_KEY + xChk + CONCATENATE_PUBLIC_KEY);
            var iv = Encoding.UTF8.GetBytes(PUBLIC_IV + xChk + CONCATENATE_PUBLIC_IV);
            var encrypted = Convert.FromBase64String(encryptedValue);
            var decryptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
            return decryptedFromJavascript;
        }
        catch
        {
            return null;
        }
    }

    public static string DecryptHeader(string header)
    {
        try
        {
            var keybytes = Encoding.UTF8.GetBytes(HEADER_KEY);
            var iv = Encoding.UTF8.GetBytes(HEADER_KEY);
            var encrypted = Convert.FromBase64String(header);
            var decryptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
            return decryptedFromJavascript;
        }
        catch
        {
            return null;
        }
    }

    private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
    {
        if (cipherText == null || cipherText.Length <= 0)
        {
            return null;
        }

        if (key == null || key.Length <= 0)
        {
            return null;
        }

        if (iv == null || iv.Length <= 0)
        {
            return null;
        }

        string plaintext = null;

        using (var rijAlg = new RijndaelManaged())
        {
            rijAlg.Mode = CipherMode.CBC;
            rijAlg.Padding = PaddingMode.PKCS7;
            rijAlg.FeedbackSize = 128;
            rijAlg.Key = key;
            rijAlg.IV = iv;

            var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

            using (var msDecrypt = new MemoryStream(cipherText))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        return plaintext;
    }
}