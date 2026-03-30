// Auto-generated from [Dto] types — do not edit
export interface Tenant {
  id: number;
  name: string;
  slug: string;
  status: any;
  adminEmail: string;
  editionName: string;
  connectionString: string;
  createdAt: string;
  updatedAt: string;
  validUpTo: string | null;
  hosts: TenantHost[];
}

export interface TenantHost {
  id: number;
  tenantId: number;
  hostName: string;
  isActive: boolean;
}

export interface AddTenantHostRequest {
  hostName: string;
}

export interface CreateTenantRequest {
  name: string;
  slug: string;
  adminEmail: string;
  editionName: string;
  connectionString: string;
  validUpTo: string | null;
  hosts: string[];
}

export interface TenantsPermissions {
}

export interface UpdateTenantRequest {
  name: string;
  adminEmail: string;
  editionName: string;
  connectionString: string;
  validUpTo: string | null;
}

