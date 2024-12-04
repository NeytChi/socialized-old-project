using System.Text;
using System.Security.Cryptography;

namespace Core
{
    public class ProfileCondition
    {
        private Random random = new Random();
        private readonly string Alphavite = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private readonly string sum_names = "abc123";
        
        public string CreateHash(int lengthHash)
        {
            string hash = "";
            for (int i = 0; i < lengthHash; i++)
            {
                hash += Alphavite[random.Next(Alphavite.Length)];
            }
            return hash;
        }
        public int CreateCode(int lengthCode)
        {
            int minValue = 0, maxValue = 0;

            for (int i = 0; i < lengthCode; i++)
            {
                maxValue += (int)(9 * Math.Pow(10, i));
            }
            minValue += (int)(Math.Pow(10, lengthCode - 1));
            return random.Next(minValue, maxValue);
        }
        public string HashPassword(string password)
        {
            byte[] salt;
            byte[] buffer2;

            using (var bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8, HashAlgorithmName.SHA256))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);
        }
        public bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] hashedBuffer, buffer;
            
            if (hashedPassword == null || password == null)
            {
                return false;
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            var dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            
            hashedBuffer = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, hashedBuffer, 0, 0x20);

            using (var bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8, HashAlgorithmName.SHA256))
            {
                buffer = bytes.GetBytes(0x20);
            }
            return ByteArraysEqual(ref hashedBuffer, ref buffer);
        }
        private bool ByteArraysEqual(ref byte[] b1, ref byte[] b2)
        {
            if (b1 == b2)
            {
                return true;
            }
            if (b1 == null || b2 == null)
            {
                return false;
            }
            if (b1.Length != b2.Length)
            {
                return false;
            }
            for (int i = 0; i < b1.Length; i++) 
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }
            return true;
        }
        public string Encrypt(string clearText)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(
                    sum_names,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 },
                    100_000, 
                    HashAlgorithmName.SHA256 
                );

                encryptor.Key = pdb.GetBytes(32); 
                encryptor.IV = pdb.GetBytes(16);  

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public string Decrypt(string cipherText)
        {
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(
                    sum_names,
                    new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 },
                    100_000,
                    HashAlgorithmName.SHA256 
                );

                encryptor.Key = pdb.GetBytes(32); 
                encryptor.IV = pdb.GetBytes(16); 

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

    }
}
