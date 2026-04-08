// Auto-generated from [Dto] types — do not edit
export interface ChatMessage {
  id: string;
  conversationId: string;
  role: any;
  content: string;
  createdAt: string;
}

export interface Conversation {
  id: string;
  userId: string;
  title: string;
  agentName: string;
  pinned: boolean;
  createdAt: string;
  updatedAt: string;
  messages: ChatMessage[];
}

export interface CreateConversationRequest {
  agentName: string;
  title: string;
}

export interface RenameConversationRequest {
  title: string;
}

