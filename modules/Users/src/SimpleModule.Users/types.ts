// Auto-generated from [Dto] types — do not edit
export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
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

