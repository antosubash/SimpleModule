// Auto-generated from [Dto] types — do not edit
export interface AgentMessageDto {
  id: string;
  sessionId: string;
  role: string;
  content: string;
  timestamp: string;
  tokenCount: number | null;
}

export interface AgentSessionDto {
  id: string;
  agentName: string;
  userId: string;
  createdAt: string;
  lastMessageAt: string;
}

