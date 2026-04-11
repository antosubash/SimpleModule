// Auto-generated from [Dto] types — do not edit
export interface ChatMessage {
  conversationId: string;
  role: any;
  content: string;
  id: string;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface Conversation {
  userId: string;
  title: string;
  agentName: string;
  pinned: boolean;
  messages: ChatMessage[];
  id: string;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface CreateConversationRequest {
  agentName: string;
  title: string;
}

export interface RenameConversationRequest {
  title: string;
}

