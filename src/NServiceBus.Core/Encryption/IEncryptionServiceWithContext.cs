namespace NServiceBus.Encryption
{
    /// <summary>
    /// Abstraction for encryption capabilities.
    /// </summary>
    interface IEncryptionServiceWithContext
    {
        /// <summary>
        /// Encrypts the given value returning an EncryptedValue.
        /// </summary>
        EncryptedValue Encrypt(string value, object context);

        /// <summary>
        /// Decrypts the given EncryptedValue object returning the source string.
        /// </summary>
        string Decrypt(EncryptedValue encryptedValue, object context);
    }
}