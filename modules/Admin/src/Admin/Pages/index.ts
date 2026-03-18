import Roles from './Admin/Roles';
import RolesCreate from './Admin/RolesCreate';
import RolesEdit from './Admin/RolesEdit';
import Users from './Admin/Users';
import UsersCreate from './Admin/UsersCreate';
import UsersEdit from './Admin/UsersEdit';

export const pages: Record<string, any> = {
  'Admin/Admin/Users': Users,
  'Admin/Admin/UsersCreate': UsersCreate,
  'Admin/Admin/UsersEdit': UsersEdit,
  'Admin/Admin/Roles': Roles,
  'Admin/Admin/RolesCreate': RolesCreate,
  'Admin/Admin/RolesEdit': RolesEdit,
};
