import Disable2fa from './Account/Disable2fa';
import EnableAuthenticator from './Account/EnableAuthenticator';
import GenerateRecoveryCodes from './Account/GenerateRecoveryCodes';
import ResetAuthenticator from './Account/ResetAuthenticator';
import ShowRecoveryCodes from './Account/ShowRecoveryCodes';
import TwoFactorAuthentication from './Account/TwoFactorAuthentication';

export const pages: Record<string, any> = {
  'Users/Account/TwoFactorAuthentication': TwoFactorAuthentication,
  'Users/Account/EnableAuthenticator': EnableAuthenticator,
  'Users/Account/Disable2fa': Disable2fa,
  'Users/Account/ResetAuthenticator': ResetAuthenticator,
  'Users/Account/GenerateRecoveryCodes': GenerateRecoveryCodes,
  'Users/Account/ShowRecoveryCodes': ShowRecoveryCodes,
};
