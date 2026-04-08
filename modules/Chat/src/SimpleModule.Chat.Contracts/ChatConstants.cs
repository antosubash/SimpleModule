namespace SimpleModule.Chat.Contracts;

public static class ChatConstants
{
    public const string ModuleName = "Chat";
    public const string RoutePrefix = "/api/chat";
    public const string ViewPrefix = "/chat";

    public static class Routes
    {
        public const string ListConversations = "/conversations";
        public const string CreateConversation = "/conversations";
        public const string GetConversation = "/conversations/{id:guid}";
        public const string RenameConversation = "/conversations/{id:guid}";
        public const string DeleteConversation = "/conversations/{id:guid}";
        public const string GetMessages = "/conversations/{id:guid}/messages";
        public const string SendMessageStream = "/conversations/{id:guid}/stream";

        public const string Browse = "/";
        public const string Conversation = "/{id:guid}";
    }
}
