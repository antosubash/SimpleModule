// Auto-generated from [Dto] types — do not edit
export interface AdminUserDto {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  roles: string[];
  isLockedOut: boolean;
  isDeactivated: boolean;
  createdAt: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string;
  userCount: number;
  createdAt: string;
}

export interface ApplicationRole {
  description: string;
  createdAt: string;
}

export interface ApplicationUser {
  displayName: string;
  createdAt: string;
  lastLoginAt: string | null;
  deactivatedAt: string | null;
}

export interface CreateAdminUserRequest {
  email: string;
  displayName: string;
  password: string;
  emailConfirmed: boolean;
  roles: string[];
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
}

export interface UpdateAdminUserRequest {
  displayName: string;
  email: string;
  emailConfirmed: boolean;
}

export interface UpdateUserRequest {
  email: string;
  displayName: string;
}

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
}

