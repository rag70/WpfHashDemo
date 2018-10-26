// Authors:
//  Roberto Alonso Gómez  <bob@lynza.com>
//
// Copyright (C) 2008 Lynza (http://lynza.com)
using System;
using System.Security.Cryptography;
using System.Text;

namespace WpfHashDemo.Code
{
    static public class Hash
    {
        static public uint Djb32(string str)
        {
            uint hash = 0;    
            foreach (Char c in str)
            {
                hash = ((hash << 5) + hash) ^ c;
            }
            return hash;
        }

        static public uint Sdbm32(string str)
        {
            uint hash = 0;
            foreach (Char c in str)
            {
                hash = c + (hash << 6) + (hash << 16) - hash;
            }
            return hash;
        }

        static public uint StringHashCode(string str)
        {
            return (uint)str.GetHashCode();
        }


        static public uint DjbSdbm16X16(string str)
        {
            return Djb32(str)<<16 | (Sdbm32(str) & 0xffff);
        }

        // https://cp-algorithms.com/string/string-hashing.html
        static public uint PolynomialRolling32(string str)
        {
            int p = 31;
            uint m = (uint)(1e9 + 9);
            uint hash_value = 0;
            uint p_pow = 1;
            foreach (Char c in str)
            {
                hash_value = (uint)(hash_value + (c - 'a' + 1) * p_pow) % m;
                p_pow = (uint)((p_pow* p) % m);
            }
            return hash_value;
        }


        static public ulong Djb64(string str)
        {
            ulong hash = 5381;
            foreach (Char c in str)
            {
                hash = ((hash << 5) + hash) ^ c;
            }
            return hash;
        }

        static public ulong Sdbm64(string str)
        {
            ulong hash = 0;
            foreach (Char c in str)
            {
                hash = c + (hash << 6) + (hash << 16) - hash;
            }
            return hash;
        }

        static public ulong DjbSdbm32x32(string str)
        {
            return (((ulong)Djb32(str))<<32) | Sdbm32(str);
        }

        static public ulong StringHashCode64(string input)
        {
            string s1 = input.Substring(0, input.Length / 2);
            string s2 = input.Substring(input.Length / 2);
            ulong l1 = (ulong)s1.GetHashCode() << 32;
            ulong l2 = (ulong)s2.GetHashCode();
            return l1 | l2;
        }

        static private MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
        static public ulong MD5Trunk64(string str)
        {
            byte[] byteContents = Encoding.Unicode.GetBytes(str);
            var h = MD5.ComputeHash(byteContents);
            ulong hashCodeStart = (ulong)BitConverter.ToInt64(h, 0);
            return hashCodeStart;
        }

        
        static private SHA256CryptoServiceProvider SHA256 = new SHA256CryptoServiceProvider();
        static public ulong SHA256Trunk64(string str)
        {
            byte[] byteContents = Encoding.Unicode.GetBytes(str);
            var h = SHA256.ComputeHash(byteContents);
            ulong hashCodeStart = (ulong)BitConverter.ToInt64(h, 0);
            return hashCodeStart;
        }

    }
}
