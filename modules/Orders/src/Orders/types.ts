// Auto-generated from [Dto] types — do not edit
export interface CreateOrderRequest {
  userId: string;
  items: OrderItem[];
}

export interface Order {
  id: number;
  userId: string;
  items: OrderItem[];
  total: number;
  createdAt: string;
}

export interface OrderItem {
  productId: number;
  quantity: number;
}

export interface UpdateOrderRequest {
  userId: string;
  items: OrderItem[];
}
