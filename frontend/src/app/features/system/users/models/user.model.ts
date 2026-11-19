/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string;
  avatarUrl?: string;
  isActive: boolean;
  roles: string[];
  createdDate: string;
}

export interface UserDetailDto extends UserDto {
  updatedDate?: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string;
  roles: string[];
}

export interface UpdateUserRequest {
  id: string;
  fullName: string;
  phoneNumber?: string;
  isActive: boolean;
  roles: string[];
}

export interface ChangePasswordRequest {
  userId: string;
  currentPassword: string;
  newPassword: string;
}
