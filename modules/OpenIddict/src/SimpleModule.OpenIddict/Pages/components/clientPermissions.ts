export const clientTabIds = [
  { id: 'details', key: 'TabDetails' as const },
  { id: 'uris', key: 'TabUris' as const },
  { id: 'permissions', key: 'TabPermissions' as const },
];

export const permissionGroupDefs = [
  {
    key: 'Endpoints' as const,
    permissions: [
      { value: 'ept:authorization', key: 'Authorization' as const },
      { value: 'ept:token', key: 'Token' as const },
      { value: 'ept:end_session', key: 'EndSession' as const },
      { value: 'ept:revocation', key: 'Revocation' as const },
      { value: 'ept:introspection', key: 'Introspection' as const },
    ],
  },
  {
    key: 'GrantTypes' as const,
    permissions: [
      { value: 'gt:authorization_code', key: 'AuthorizationCode' as const },
      { value: 'gt:refresh_token', key: 'RefreshToken' as const },
      { value: 'gt:client_credentials', key: 'ClientCredentials' as const },
      { value: 'gt:implicit', key: 'Implicit' as const },
    ],
  },
  {
    key: 'ResponseTypes' as const,
    permissions: [
      { value: 'rst:code', key: 'Code' as const },
      { value: 'rst:token', key: 'TokenResponse' as const },
    ],
  },
  {
    key: 'Scopes' as const,
    permissions: [
      { value: 'scp:openid', key: 'OpenID' as const },
      { value: 'scp:profile', key: 'Profile' as const },
      { value: 'scp:email', key: 'Email' as const },
      { value: 'scp:roles', key: 'Roles' as const },
    ],
  },
];
