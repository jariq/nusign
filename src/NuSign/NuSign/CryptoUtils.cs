﻿using System;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace NuSign
{
    public static class CryptoUtils
    {
        public static byte[] ComputeHash(Stream data)
        {
            System.Security.Cryptography.SHA256Managed sha256 = new System.Security.Cryptography.SHA256Managed();
            sha256.ComputeHash(data);
            return sha256.Hash;
        }

        public static X509Certificate2 CreateDetachedCmsSignature(byte[] data, string certThumbPrint, out byte[] signature)
        {
            X509Certificate2 signingCertificate = GetSigningCertificate(certThumbPrint);
            CmsSigner cmsSigner = new CmsSigner(signingCertificate);

            ContentInfo contentInfo = new ContentInfo(data);
            SignedCms signedCms = new SignedCms(contentInfo, true);
            signedCms.ComputeSignature(cmsSigner, false);

            signature = signedCms.Encode();
            return signingCertificate;
        }

        public static SignerInfo VerifyDetachedCmsSignature(byte[] data, byte[] signature, bool skipCertValidation)
        {
            ContentInfo contentInfo = new ContentInfo(data);
            SignedCms signedCms = new SignedCms(contentInfo, true);
            signedCms.Decode(signature);

            try
            {
                signedCms.CheckSignature(skipCertValidation);
            }
            catch (Exception ex)
            {
                throw new InvalidSignatureException(ex);
            }

            return signedCms.SignerInfos[0];
        }

        private static X509Certificate2 GetSigningCertificate(string certThumbPrint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certs = string.IsNullOrEmpty(certThumbPrint)
                    ? X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection)
                    : store.Certificates.Find(X509FindType.FindByThumbprint, certThumbPrint, true);

                if (certs != null && certs.Count > 0)
                    return certs[0];
            }
            finally
            {
                store.Close();
            }

            return null;
        }
    }
}
