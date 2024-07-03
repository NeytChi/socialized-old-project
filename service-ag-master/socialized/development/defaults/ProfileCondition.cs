﻿using System;
using Serilog;
using System.IO;
using System.Text;
using Serilog.Core;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace Common
{
    public class ProfileCondition
    {
        public ProfileCondition(Logger log)
        {
            this.log = log;
        }
		private const sbyte minLength = 6;
        private const sbyte maxLength = 20;
        
        private EmailAddressAttribute emailChecker = new EmailAddressAttribute();
        public Logger log;
		public Regex onlyEnglish = new Regex("^[a-zA-Z0-9]*$", RegexOptions.Compiled);
		public Random random = new Random();
        private string Alphavite = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private string sum_names = "abc123";

        public bool EmailIsTrue(string email, ref string message)
        {
            if (!string.IsNullOrEmpty(email)) {
                if (emailChecker.IsValid(email))
                    return true;
                message = "invalid_email";
            }
            else
                message = "empty_email";
            log.Warning("Check email -> " + email ?? "");
            return false;
        } 
        public bool UserNameIsTrue(string userName, ref string answer) 
		{
            if (!string.IsNullOrEmpty(userName)) {
                if (Regex.IsMatch(userName, @"^[A-Za-z-А-Яа-я_']+\s?\s?[A-Za-z-А-Яа-я_']+"))
                    return true;
                else
                    answer = "invalid_name";
            }
            else
                answer = "empty_name";
            return false;
        }
		public bool PasswordIsTrue(string password, ref string answer) 
		{
			if (!string.IsNullOrEmpty(password)) {
                if (RequiredLength(password, ref answer)) {
                    if (HasLetter(password, ref answer)) {
                        if (HasDigit(password, ref answer))
                            return true;
                    }
                }
            }
            else
                answer = "empty_password";
            log.Warning(answer);
            return false;
        }
        public bool RequiredLength(string password, ref string answer)
        {
            if (password.Length >= minLength) {
                if (password.Length <= maxLength)
                    return true;
            } 
            answer = "length_password";
            return false;
        }
        public bool HasLetter(string password, ref string answer)
        {
            foreach (char c in password) {
                if (char.IsLetter(c))
                    return true;
            }
            answer = "letter_password";
            return false;
        }
        public bool HasDigit(string password, ref string answer)
        {
            foreach (char c in password) {
                if (char.IsDigit(c))
                    return true;
            }
            answer = "digit_password";
            return false;
        }
        public bool LoginIsTrue(string login, ref string answer)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(login) && login.Length >= 3) {
                result = onlyEnglish.Match(login).Success;
                if (!result)
                    answer = "english_login";
            }
            else
                answer = "length_login";
            log.Information("Check login, result -> " + result);
			return result;
        }
        public string CreateHash(int lengthHash)
        {
            string hash = "";
            for (int i = 0; i < lengthHash; i++)
                hash += Alphavite[random.Next(Alphavite.Length)];
            return hash;
        }
        public int CreateCode(int lengthCode)
        {
            int minValue = 0, maxValue = 0;
            
            for (int i = 0; i < lengthCode; i++)
                maxValue += (int)(9 * Math.Pow(10, i));
            minValue += (int)(Math.Pow(10, lengthCode - 1));
            return random.Next(minValue, maxValue);
        }
        public string HashPassword(string password)
        {
            byte[] salt;
            byte[] buffer2;
            if (password == null) {
                log.Error("Input value is null, function HashPassword()");
                return "";
            }
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8)) {
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
                return false;
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
                return false;
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            hashedBuffer = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, hashedBuffer, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8))
                buffer = bytes.GetBytes(0x20);
            return ByteArraysEqual(ref hashedBuffer, ref buffer);
        }
        private bool ByteArraysEqual(ref byte[] b1,ref byte[] b2)
        {
            if (b1 == b2)
                return true;
            if (b1 == null || b2 == null)
                return false; 
            if (b1.Length != b2.Length)
                return false;
            for (int i = 0; i < b1.Length; i++) {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
        public string Encrypt(string clearText)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create()) {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(sum_names,  new byte[] 
                { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)) {
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
            using (Aes encryptor = Aes.Create()) {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(sum_names, new byte[] 
                { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), 
                        CryptoStreamMode.Write)) {
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
