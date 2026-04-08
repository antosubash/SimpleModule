export const pages: Record<string, unknown> = {
  'Admin/Admin/Hub': () => import('./Admin/Hub'),
  'Admin/Admin/Users': () => import('./Admin/Users'),
  'Admin/Admin/UsersCreate': () => import('./Admin/UsersCreate'),
  'Admin/Admin/UsersEdit': () => import('./Admin/UsersEdit'),
  'Admin/Admin/Roles': () => import('./Admin/Roles'),
  'Admin/Admin/RolesCreate': () => import('./Admin/RolesCreate'),
  'Admin/Admin/RolesEdit': () => import('./Admin/RolesEdit'),
};
