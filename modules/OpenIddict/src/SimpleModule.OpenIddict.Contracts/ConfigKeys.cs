namespace SimpleModule.OpenIddict.Contracts;

public static class ConfigKeys
{
    public const string OpenIddictBaseUrl = "OpenIddict:BaseUrl";

    /// <summary>
    /// Enables the ROPC (resource-owner password credentials) grant. Intended for
    /// local load testing only — <c>OpenIddictProductionGuard</c> refuses it in
    /// real deployments.
    /// </summary>
    public const string OpenIddictAllowPasswordGrant = "OpenIddict:AllowPasswordGrant";
    public const string OpenIddictEncryptionCertPath = "OpenIddict:EncryptionCertificatePath";
    public const string OpenIddictSigningCertPath = "OpenIddict:SigningCertificatePath";
    public const string OpenIddictCertPassword = "OpenIddict:CertificatePassword";
    public const string OpenIddictAdditionalRedirectUris = "OpenIddict:AdditionalRedirectUris";
}
