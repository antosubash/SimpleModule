// Auto-generated from [Dto] types — do not edit
export interface CreateProductRequest {
  name: string;
  price: number;
}

export interface Product {
  name: string;
  price: number;
  id: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
}

