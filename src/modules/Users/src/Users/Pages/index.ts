import Roles from './Admin/Roles';
import RolesCreate from './Admin/RolesCreate';
import RolesEdit from './Admin/RolesEdit';
import Users from './Admin/Users';
import UsersEdit from './Admin/UsersEdit';

export const pages: Record<string, any> = {
  'Users/Admin/Users': Users,
  'Users/Admin/UsersEdit': UsersEdit,
  'Users/Admin/Roles': Roles,
  'Users/Admin/RolesCreate': RolesCreate,
  'Users/Admin/RolesEdit': RolesEdit,
};
