// Auto-generated from [Dto] types — do not edit
export interface CreateOrderRequest {
  userId: string;
  items: OrderItem[];
}

export interface Order {
  userId: string;
  items: OrderItem[];
  total: number;
  createdBy: string;
  updatedBy: string;
  id: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface OrderItem {
  productId: number;
  quantity: number;
}

export interface UpdateOrderRequest {
  userId: string;
  items: OrderItem[];
}

