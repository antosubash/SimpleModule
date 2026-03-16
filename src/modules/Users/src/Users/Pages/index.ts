import './index.css';
import Disable2fa from './Account/Disable2fa';
import EnableAuthenticator from './Account/EnableAuthenticator';
import GenerateRecoveryCodes from './Account/GenerateRecoveryCodes';
import ResetAuthenticator from './Account/ResetAuthenticator';
import ShowRecoveryCodes from './Account/ShowRecoveryCodes';
import TwoFactorAuthentication from './Account/TwoFactorAuthentication';
import Roles from './Admin/Roles';
import RolesCreate from './Admin/RolesCreate';
import RolesEdit from './Admin/RolesEdit';
import Users from './Admin/Users';
import UsersEdit from './Admin/UsersEdit';

export const pages: Record<string, any> = {
  'Users/Account/TwoFactorAuthentication': TwoFactorAuthentication,
  'Users/Account/EnableAuthenticator': EnableAuthenticator,
  'Users/Account/Disable2fa': Disable2fa,
  'Users/Account/ResetAuthenticator': ResetAuthenticator,
  'Users/Account/GenerateRecoveryCodes': GenerateRecoveryCodes,
  'Users/Account/ShowRecoveryCodes': ShowRecoveryCodes,
  'Users/Admin/Users': Users,
  'Users/Admin/UsersEdit': UsersEdit,
  'Users/Admin/Roles': Roles,
  'Users/Admin/RolesCreate': RolesCreate,
  'Users/Admin/RolesEdit': RolesEdit,
};
