﻿using System.Security.Cryptography;
using System.Text;

namespace CryptoLibrary {

public class CryptoLibraryClass {

  private const int KeySizeInBytes = 16;

  private const int BlockSizeInBytes = 16;

  private readonly byte[] key;

  public CryptoLibraryClass(byte[] key) {
    if (key.Length != KeySizeInBytes) {
      throw new ArgumentException($"Key size must be {KeySizeInBytes} bytes.");
    }

    this.key = new byte[key.Length];
    Array.Copy(key, this.key, key.Length);
  }

  public int TransformFile(string sourceFilePath, string destFilePath, int operation) {
    CryptoLibraryClass crypto = new CryptoLibraryClass(key);
    string newsourceFilePath = "Hello.bin";
    crypto.ConvertToBinary(sourceFilePath, newsourceFilePath);

    if (operation == 1) {
      byte[] fileData = File.ReadAllBytes(newsourceFilePath);
      byte[] sha1Digest = crypto.ComputeSHA1(fileData);
      byte[] sha256Digest = crypto.ComputeSHA256(fileData);
      byte[] buffer = crypto.CreateBuffer(sha1Digest, fileData, sha256Digest);
      byte[] encryptedBuffer = crypto.EncryptData(buffer);
      File.WriteAllBytes(destFilePath, encryptedBuffer);
      return crypto.GetPasswordFromDigest(sha256Digest);
    } else if (operation == 0) {
      byte[] encryptedBuffer = File.ReadAllBytes(sourceFilePath);
      byte[] decryptedData = crypto.DecryptData(encryptedBuffer);
      int length = BitConverter.ToInt32(decryptedData, 0);
      int sha1DigestOffset = 4;
      int fileDataOffset = 24;
      int sha256DigestOffset = 24 + length;
      byte[] sha1Digest = new byte[20];
      Buffer.BlockCopy(decryptedData, sha1DigestOffset, sha1Digest, 0, 20);
      byte[] fileData = new byte[length];
      Buffer.BlockCopy(decryptedData, fileDataOffset, fileData, 0, length);
      byte[] sha256Digest = new byte[32];
      Buffer.BlockCopy(decryptedData, sha256DigestOffset, sha256Digest, 0, 32);
      byte[] calculatedSha1Digest = crypto.ComputeSHA1(fileData);
      byte[] calculatedSha256Digest = crypto.ComputeSHA256(fileData);
      bool sha1Validation = crypto.CompareHashes(sha1Digest, calculatedSha1Digest);
      bool sha256Validation = crypto.CompareHashes(sha256Digest, calculatedSha256Digest);

      if (sha1Validation && sha256Validation) {
        File.WriteAllBytes(destFilePath, fileData);
      }

      return 0;
    } else {
      throw new ArgumentException("Invalid operation specified. Operation must be 0 or 1.");
    }
  }
  public int HOTP(byte[] key, int counter) {
    CryptoLibraryClass crypto = new CryptoLibraryClass(key);
    byte[] hmacBytes = crypto.ComputeHMACSHA1(key, BitConverter.GetBytes(counter));
    int sbits = CalculateDynamicTruncation(hmacBytes);
    int hotpValue = (int)(sbits % 1000000);
    return hotpValue;
  }

  private int CalculateDynamicTruncation(byte[] hmacBytes) {
    int offset = hmacBytes[19] & 0xf;
    int bin_code = ((hmacBytes[offset] & 0x7f) << 24)
                   | ((hmacBytes[offset + 1] & 0xff) << 16)
                   | ((hmacBytes[offset + 2] & 0xff) << 8)
                   | (hmacBytes[offset + 3] & 0xff);
    return bin_code;
  }
  public byte[] ComputeSHA1(byte[] data) {
    using (SHA1 sha1 = SHA1.Create()) {
      return sha1.ComputeHash(data);
    }
  }
  public byte[] ComputeHMACSHA1(byte[] data, byte[] key) {
    using (HMACSHA1 hmacSha1 = new HMACSHA1(key)) {
      return hmacSha1.ComputeHash(data);
    }
  }
  public byte[] ComputeSHA256(byte[] data) {
    using (SHA256 sha256 = SHA256.Create()) {
      return sha256.ComputeHash(data);
    }
  }

  public bool CompareHashes(byte[] hash1, byte[] hash2) {
    if (hash1.Length != hash2.Length) {
      return false;
    }

    for (int i = 0; i < hash1.Length; i++) {
      if (hash1[i] != hash2[i]) {
        return false;
      }
    }

    return true;
  }

  private void ConvertToBinary(string inputFile, string outputFile) {
    byte[] buffer;

    using (FileStream fileStream = File.OpenRead(inputFile)) {
      buffer = new byte[fileStream.Length];
      fileStream.Read(buffer, 0, buffer.Length);
    }

    using (FileStream fileStream = File.OpenWrite(outputFile)) {
      fileStream.Write(buffer, 0, buffer.Length);
    }
  }

  private byte[] CreateBuffer(byte[] sha1Digest, byte[] fileData, byte[] sha256Digest) {
    int bufferLength = 4 + sha1Digest.Length + fileData.Length + sha256Digest.Length + BlockSizeInBytes;
    byte[] buffer = new byte[bufferLength];
    Buffer.BlockCopy(BitConverter.GetBytes(fileData.Length), 0, buffer, 0, 4);
    Buffer.BlockCopy(sha1Digest, 0, buffer, 4, sha1Digest.Length);
    Buffer.BlockCopy(fileData, 0, buffer, 4 + sha1Digest.Length, fileData.Length);
    Buffer.BlockCopy(sha256Digest, 0, buffer, 4 + sha1Digest.Length + fileData.Length, sha256Digest.Length);
    return buffer;
  }

  public byte[] EncryptData(byte[] data) {
    using (Aes aes = Aes.Create()) {
      aes.Key = key;
      aes.IV = new byte[16];
      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;

      using (ICryptoTransform encryptor = aes.CreateEncryptor()) {
        return encryptor.TransformFinalBlock(data, 0, data.Length);
      }
    }
  }

  public byte[] DecryptData(byte[] data) {
    using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create()) {
      aes.Key = key;
      aes.IV = new byte[16];
      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;

      using (ICryptoTransform decryptor = aes.CreateDecryptor()) {
        return decryptor.TransformFinalBlock(data, 0, data.Length);
      }
    }
  }

  private int GetPasswordFromDigest(byte[] digest) {
    return BitConverter.ToInt32(digest, 0);
  }



}
}
